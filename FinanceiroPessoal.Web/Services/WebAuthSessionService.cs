using FinanceiroPessoal.Core.Services;

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

    public void EnsureSecret()
    {
        if (_authService.ChaveExiste())
        {
            return;
        }

        SetupSecret = _authService.GerarNovaChave();
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
