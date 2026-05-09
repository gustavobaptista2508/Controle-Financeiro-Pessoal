using FinanceiroPessoal.Core.Services;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    private readonly Dictionary<string, (string Senha, string Nome)> _usuarios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@financeiro.local"] = ("123456", "Administrador")
    };

    private readonly AuthService _authService;

    public WebAuthSessionService(AuthService authService)
    {
        _authService = authService;
    }

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUserEmail { get; private set; }
    public string? CurrentUserName { get; private set; }
    public string? SetupSecret { get; private set; }
    public string? SetupQrUri { get; private set; }
    public string? PreferredPushApp { get; private set; }
    public bool PushChallengePending { get; private set; }
    public bool IsFirstAccess => !_authService.ChaveExiste();

    public bool LoginWithPassword(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return false;
        if (!_usuarios.TryGetValue(email.Trim(), out var dados)) return false;

        IsAuthenticated = dados.Senha == senha;
        if (IsAuthenticated)
        {
            CurrentUserEmail = email.Trim().ToLowerInvariant();
            CurrentUserName = dados.Nome;
        }

        return IsAuthenticated;
    }

    public string RegisterUser(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return "Preencha nome, e-mail e senha.";
        var key = email.Trim().ToLowerInvariant();
        if (_usuarios.ContainsKey(key)) return "E-mail já cadastrado.";

        _usuarios[key] = (senha, nome.Trim());
        MultiTenantDataStore.InitializeTenant(key);
        return "Usuário cadastrado com base individual criada com sucesso.";
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUserEmail = null;
        CurrentUserName = null;
        PushChallengePending = false;
    }
}
