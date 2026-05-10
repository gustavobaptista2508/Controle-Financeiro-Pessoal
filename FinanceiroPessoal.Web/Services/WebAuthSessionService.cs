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
        Console.WriteLine("AUTH SERVICE LOGIN EXECUTADO");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.");

        await using var db = CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        Console.WriteLine($"USUARIO ENCONTRADO: {(usuario is null ? "NAO" : "SIM")}");

        if (usuario is null)
            return (false, "E-mail ou senha inválidos.");

        var senhaValida = _passwordHasher.VerifyPassword(senha, usuario.SenhaHash);
        Console.WriteLine($"SENHA VALIDA: {(senhaValida ? "SIM" : "NAO")}");

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
        await using var db = CreateDbContext();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);

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
        var connectionString = _configuration.GetConnectionString("MySqlConnection")
            ?? throw new InvalidOperationException("Conexão MySQL não configurada.");

        return new MySqlDbContext(connectionString);
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
