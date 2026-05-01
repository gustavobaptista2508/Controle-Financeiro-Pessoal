using FinanceiroPessoal.Core.Services;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    private readonly AuthService _authService;

    public WebAuthSessionService(AuthService authService)
    {
        _authService = authService;
        _isFirstAccess = !_authService.ChaveExiste();
    }

    public bool IsAuthenticated { get; private set; }
    public string? SetupSecret { get; private set; }
    public string? SetupQrUri { get; private set; }
    public string? PreferredPushApp { get; private set; }
    public bool PushChallengePending { get; private set; }

    private bool _isFirstAccess;

    public bool IsFirstAccess => _isFirstAccess;

    public void EnsureSecret()
    {
        if (_authService.ChaveExiste())
        {
            return;
        }

        SetupSecret = _authService.GerarNovaChave();
        SetupQrUri = _authService.GerarUriQrCode(SetupSecret);
    }

    public bool Login(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return false;
        }

        var ok = _authService.VerificarCodigo(codigo.Trim());
        IsAuthenticated = ok;

        if (ok)
        {
            PushChallengePending = false;
            _isFirstAccess = false;
        }

        return ok;
    }

    public void SetPreferredPushApp(string app)
    {
        if (!string.IsNullOrWhiteSpace(app))
        {
            PreferredPushApp = app.Trim();
        }
    }

    public bool StartPushChallenge()
    {
        if (string.IsNullOrWhiteSpace(PreferredPushApp))
        {
            return false;
        }

        PushChallengePending = true;
        return true;
    }

    public bool ApprovePushChallenge()
    {
        if (_isFirstAccess || !PushChallengePending)
        {
            return false;
        }

        IsAuthenticated = true;
        PushChallengePending = false;
        return true;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        PushChallengePending = false;
    }
}
