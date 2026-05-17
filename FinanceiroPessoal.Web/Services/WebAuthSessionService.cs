using System.Linq;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Services;
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

    private readonly FinanceiroDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly UsuarioPadraoService _usuarioPadraoService;

    public WebAuthSessionService(FinanceiroDbContext db, IPasswordHasherService passwordHasher, UsuarioPadraoService usuarioPadraoService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _usuarioPadraoService = usuarioPadraoService;
    }

    public async Task<(bool ok, string? erro)> LoginWithPasswordAsync(string email, string senha, bool lembrarMe, HttpContext context)
    {
        Console.WriteLine("LOGIN: buscando usuário");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);
        Console.WriteLine($"LOGIN: usuário encontrado {usuario is not null}");

        if (usuario is null)
            return (false, "E-mail ou senha inválidos.");

        var senhaValida = _passwordHasher.VerifyPassword(senha, usuario.SenhaHash);

        if (!senhaValida)
        {
            var senhaHashPareceTextoPuro =
                !string.IsNullOrWhiteSpace(usuario.SenhaHash)
                && usuario.SenhaHash == senha;

            if (senhaHashPareceTextoPuro)
            {
                senhaValida = true;
                usuario.SenhaHash = _passwordHasher.HashPassword(senha);
                Console.WriteLine("LOGIN: senha legada convertida para hash.");
            }
        }

        if (!senhaValida)
            return (false, "E-mail ou senha inválidos.");

        if (!usuario.Ativo)
            return (false, "Usuário inativo.");

        if (string.Equals(usuario.Email, "admin@granaok.com", StringComparison.OrdinalIgnoreCase))
        {
            usuario.AssinaturaStatus = "ATIVA";
            usuario.AssinaturaExpiraEm = DateTime.Now.AddYears(10);

            if (!usuario.PlanoId.HasValue)
            {
                var planoAtivo = await _db.Planos.AsNoTracking().OrderBy(p => p.Preco).FirstOrDefaultAsync(p => p.Ativo);
                if (planoAtivo is not null)
                    usuario.PlanoId = planoAtivo.Id;
            }
        }

        usuario.UltimoLogin = DateTime.Now;
        usuario.DataAtualizacao = DateTime.Now;
        await _db.SaveChangesAsync();

        await SignInAsync(usuario, lembrarMe, context);
        Console.WriteLine("LOGIN: sessão criada");
        Login(usuario.Nome, usuario.Email);
        Console.WriteLine("LOGIN: redirecionando");
        return (true, null);
    }

    public async Task LoginWithGoogleAsync(string email, string nome, HttpContext context)
    {
        Console.WriteLine("DEBUG GOOGLE LOGIN: callback iniciado");
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == normalizedEmail);
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

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            await _usuarioPadraoService.CriarEstruturaPadraoAsync(usuario.Id);
            Console.WriteLine($"DEBUG GOOGLE LOGIN: usuário criado e dados padrão inicializados. UsuarioId: {usuario.Id}");
        }
        else
        {
            usuario.UltimoLogin = DateTime.Now;
            usuario.DataAtualizacao = DateTime.Now;
            await _db.SaveChangesAsync();
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
