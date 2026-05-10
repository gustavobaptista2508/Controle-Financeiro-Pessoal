using FinanceiroPessoal.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    public bool IsAuthenticated { get; private set; }
    public string CurrentUserName { get; private set; } = string.Empty;
    public string CurrentUserEmail { get; private set; } = string.Empty;
    public event Action? AuthenticationStateChanged;

    private static readonly Dictionary<string, Usuario> _usuarios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@financeiro.local"] = new Usuario
        {
            Id = 1,
            Nome = "Administrador",
            Email = "admin@financeiro.local",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Ativo = true
        }
    };

    public async Task<(bool ok, string? erro)> LoginWithPasswordAsync(string email, string senha, bool lembrarMe, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return (false, "Preencha e-mail e senha.");

        if (!_usuarios.TryGetValue(email.Trim(), out var usuario) || !usuario.Ativo)
            return (false, "E-mail ou senha inválidos.");

        if (!BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
            return (false, "E-mail ou senha inválidos.");

        await SignInAsync(usuario, lembrarMe, context);
        Login(usuario.Nome, usuario.Email);
        return (true, null);
    }

    public async Task LoginWithGoogleAsync(string email, string nome, HttpContext context)
    {
        if (!_usuarios.TryGetValue(email, out var usuario))
        {
            usuario = new Usuario
            {
                Id = _usuarios.Count + 1,
                Nome = nome,
                Email = email,
                SenhaHash = string.Empty,
                Ativo = true
            };
            _usuarios[email] = usuario;
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

    public string RegisterUser(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return "Preencha nome, e-mail e senha.";

        var key = email.Trim().ToLowerInvariant();
        if (_usuarios.ContainsKey(key)) return "E-mail já cadastrado.";

        _usuarios[key] = new Usuario
        {
            Id = _usuarios.Count + 1,
            Nome = nome.Trim(),
            Email = key,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
            Ativo = true
        };

        return "Usuário cadastrado com sucesso.";
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
