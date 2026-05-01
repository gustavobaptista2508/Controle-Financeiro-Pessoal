using FinanceiroPessoal.Core.Services;
using System.Security.Cryptography;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    private readonly AuthService _authService;

    public WebAuthSessionService(AuthService authService)
    {
        _authService = authService;
    }

    public bool IsAuthenticated { get; private set; }
    public string? SetupSecret { get; private set; }
    public string? SetupError { get; private set; }

    public void EnsureSecret()
    {
        SetupError = null;

        if (_authService.ChaveExiste())
        {
            return;
        }

        try
        {
            SetupSecret = _authService.GerarNovaChave();
        }
        catch (PlatformNotSupportedException)
        {
            SetupError = "Não foi possível inicializar a chave de autenticação nesta plataforma.";
        }
        catch (CryptographicException)
        {
            SetupError = "Não foi possível proteger a chave de autenticação neste ambiente.";
        }
    }

    public bool Login(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        var ok = _authService.VerificarCodigo(codigo.Trim());
        IsAuthenticated = ok;
        return ok;
    }

    public void Logout() => IsAuthenticated = false;
}
