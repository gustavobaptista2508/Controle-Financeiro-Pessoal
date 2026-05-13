using System.Linq;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    public bool IsAuthenticated { get; private set; }
    public string CurrentUserName { get; private set; } = string.Empty;
    public string CurrentUserEmail { get; private set; } = string.Empty;
    public event Action? AuthenticationStateChanged;

    private readonly IConfiguration _configuration;
    private readonly IPasswordHasherService _passwordHasher;

    public WebAuthSessionService(IConfiguration configuration, IPasswordHasherService passwordHasher)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<(bool ok, string? erro)> LoginWithPasswordAsync(string email, string senha, bool lembrarMe, HttpContext context)
    {
        Console.WriteLine("DEBUG LOGIN SERVICE: chamado");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.");

        await using var db = CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        Console.WriteLine($"DEBUG GOOGLE LOGIN: usuário existente: {usuario is not null}");
        Console.WriteLine($"DEBUG LOGIN SERVICE: usuário encontrado {usuario is not null}");

        if (usuario is null)
            return (false, "E-mail ou senha inválidos.");

        var senhaValida = _passwordHasher.VerifyPassword(senha, usuario.SenhaHash);
        Console.WriteLine($"DEBUG LOGIN SERVICE: senha válida {senhaValida}");


        if (!senhaValida && usuario.SenhaHash == senha)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(senha);
            usuario.DataAtualizacao = DateTime.Now;
            await db.SaveChangesAsync();
            senhaValida = true;
            Console.WriteLine("DEBUG AUTHSERVICE: hash legado migrado para BCrypt");
        }

        if (!senhaValida)
            return (false, "E-mail ou senha inválidos.");

        if (!usuario.Ativo)
            return (false, "Usuário inativo.");

        usuario.UltimoLogin = DateTime.Now;
        usuario.DataAtualizacao = DateTime.Now;
        await db.SaveChangesAsync();

        await SignInAsync(usuario, lembrarMe, context);
        Login(usuario.Nome, usuario.Email);
        return (true, null);
    }

    public async Task LoginWithGoogleAsync(string email, string nome, HttpContext context)
    {
        Console.WriteLine("DEBUG GOOGLE LOGIN: callback iniciado");
        await using var db = CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        Console.WriteLine($"DEBUG GOOGLE LOGIN: usuário existente: {usuario is not null}");

        if (usuario is null)
        {
            usuario = new Usuario
            {
                Nome = nome,
                Email = normalizedEmail,
                SenhaHash = string.Empty,
                Ativo = true,
                EmailConfirmado = true,
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now,
                UltimoLogin = DateTime.Now
            };

            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            db.Contas.Add(new Conta
            {
                Nome = "Conta Principal",
                Tipo = "Corrente",
                UsuarioId = usuario.Id
            });

            db.Categorias.AddRange(
                new Categoria { Nome = "Alimentação", UsuarioId = usuario.Id },
                new Categoria { Nome = "Moradia", UsuarioId = usuario.Id },
                new Categoria { Nome = "Transporte", UsuarioId = usuario.Id },
                new Categoria { Nome = "Saúde", UsuarioId = usuario.Id },
                new Categoria { Nome = "Lazer", UsuarioId = usuario.Id },
                new Categoria { Nome = "Salário", UsuarioId = usuario.Id }
            );

            await db.SaveChangesAsync();
            Console.WriteLine($"DEBUG GOOGLE LOGIN: usuário criado e dados padrão inicializados. UsuarioId: {usuario.Id}");
        }
        else
        {
            usuario.UltimoLogin = DateTime.Now;
            usuario.DataAtualizacao = DateTime.Now;
            await db.SaveChangesAsync();
        }

        await SignInAsync(usuario, true, context);
        Login(usuario.Nome, usuario.Email);
    }

    public async Task LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Logout();
    }

    public void Login(string nome, string email)
    {
        IsAuthenticated = true;
        CurrentUserName = nome;
        CurrentUserEmail = email;

        AuthenticationStateChanged?.Invoke();
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUserName = string.Empty;
        CurrentUserEmail = string.Empty;

        AuthenticationStateChanged?.Invoke();
    }


    private MySqlDbContext CreateDbContext()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration.GetConnectionString("MySqlConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Conexão MySQL não configurada (DefaultConnection/MySqlConnection).");

        Console.WriteLine($"DEBUG MYSQL: ConnectionString carregada para login/google (host): {ExtractServer(connectionString)}");

        return new MySqlDbContext(connectionString);
    }

    private static string ExtractServer(string connectionString)
    {
        const string key = "Server=";
        var part = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.TrimStart().StartsWith(key, StringComparison.OrdinalIgnoreCase));
        return part ?? "server=indefinido";
    }

    private static async Task SignInAsync(Usuario usuario, bool lembrarMe, HttpContext context)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = lembrarMe,
            ExpiresUtc = lembrarMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }
}
