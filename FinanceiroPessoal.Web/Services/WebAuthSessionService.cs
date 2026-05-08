using FinanceiroPessoal.Core.Services;

namespace FinanceiroPessoal.Web.Services;

public class WebAuthSessionService
{
    private readonly Dictionary<string, (string Senha, string Nome)> _usuarios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@financeiro.local"] = ("123456", "Administrador")
    };
    private readonly Dictionary<string, List<string>> _tabelasPorUsuario = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@financeiro.local"] = new List<string> { "categorias", "contas", "pessoas", "lancamentos" }
    };

    private readonly AuthService _authService;

    public WebAuthSessionService(AuthService authService)
    {
        _authService = authService;
    }

    public bool IsAuthenticated { get; private set; }
    public string? SetupSecret { get; private set; }
    public string? SetupQrUri { get; private set; }
    public string? PreferredPushApp { get; private set; }
    public bool PushChallengePending { get; private set; }

    public bool IsFirstAccess => !_authService.ChaveExiste();

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
        PushChallengePending = false;
        return true;
    }

    public bool LoginWithPassword(string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return false;
        if (!_usuarios.TryGetValue(email.Trim(), out var dados)) return false;

        IsAuthenticated = dados.Senha == senha;
        return IsAuthenticated;
    }

    public string RegisterUser(string nome, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return "Preencha nome, e-mail e senha.";
        if (_usuarios.ContainsKey(email)) return "E-mail já cadastrado.";

        _usuarios[email] = (senha, nome);
        var safe = email.Replace("@", "_").Replace(".", "_");
        _tabelasPorUsuario[email] = new List<string>
        {
            $"categorias_{safe}",
            $"contas_{safe}",
            $"pessoas_{safe}",
            $"lancamentos_{safe}"
        };
        return "Usuário cadastrado com tabelas exclusivas criadas.";
    }

    public void Logout()
    {
        IsAuthenticated = false;
        PushChallengePending = false;
    }
}
