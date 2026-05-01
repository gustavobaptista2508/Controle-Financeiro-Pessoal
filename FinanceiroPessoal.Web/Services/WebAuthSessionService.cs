using FinanceiroPessoal.Core.Services;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    private readonly AuthService _authService;
    private readonly bool _initialKeyExists;

    public WebAuthSessionService(AuthService authService)
    {
        _authService = authService;
        _initialKeyExists = _authService.ChaveExiste();
        IsFirstAccess = !_initialKeyExists;
    }

    public bool IsAuthenticated { get; private set; }
    public string? SetupSecret { get; private set; }
    public string? SetupQrUri { get; private set; }
    public string? PreferredPushApp { get; private set; }
    public bool PushChallengePending { get; private set; }

    public bool IsFirstAccess { get; private set; }

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
            IsFirstAccess = false;
            PushChallengePending = false;
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
        if (!PushChallengePending)
        {
            return false;
        }

        IsAuthenticated = true;
        IsFirstAccess = false;
        PushChallengePending = false;
        return true;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        PushChallengePending = false;
    }
}
