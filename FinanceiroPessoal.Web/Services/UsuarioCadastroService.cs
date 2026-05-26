using System.Linq;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Services;
using Microsoft.EntityFrameworkCore;
using FinanceiroPessoal.Web.Models;

namespace FinanceiroPessoal.Web.Services;

public class UsuarioCadastroService
{
    public class CadastroUsuarioResultado
    {
        public int UsuarioId { get; set; }
        public string? CheckoutUrl { get; set; }
    }

    private readonly FinanceiroDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsuarioCadastroService> _logger;
    private readonly UsuarioPadraoService _usuarioPadraoService;
    private readonly IStripeSubscriptionService _stripeSubscriptionService;

    public UsuarioCadastroService(FinanceiroDbContext db, IPasswordHasherService passwordHasher, IEmailService emailService, ILogger<UsuarioCadastroService> logger, UsuarioPadraoService usuarioPadraoService, IStripeSubscriptionService stripeSubscriptionService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
        _usuarioPadraoService = usuarioPadraoService;
        _stripeSubscriptionService = stripeSubscriptionService;
    }

    public async Task<(bool Success, string Message)> CadastrarAsync(CadastroUsuarioModel cadastro, int? planoId = null)
    {
        if (cadastro == null) return (false, "Dados de cadastro inválidos.");

        var nome = cadastro.Nome?.Trim() ?? string.Empty;
        var email = cadastro.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var senha = cadastro.Senha ?? string.Empty;
        var confirmarSenha = cadastro.ConfirmarSenha ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-mail é obrigatório.");
        if (!email.Contains('@')) return (false, "E-mail inválido.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6) return (false, "A senha deve ter no mínimo 6 caracteres.");
        if (senha != confirmarSenha) return (false, "A senha e a confirmação devem ser iguais.");
        if (!planoId.HasValue || planoId.Value <= 0) return (false, "Plano inválido.");

        var plano = await _db.Planos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planoId.Value && p.Ativo);
        if (plano is null) return (false, "Plano inválido ou inativo.");

        var emailJaExiste = await _db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
        if (emailJaExiste) return (false, "E-mail já cadastrado.");

        var now = DateTime.UtcNow;
        var senhaHash = _passwordHasher.HashPassword(senha);

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = senhaHash,
            Telefone = null,
            Ativo = true,
            EmailConfirmado = true,
            TokenRecuperacao = null,
            TokenExpiracao = null,
            UltimoLogin = null,
            DataCriacao = now,
            DataAtualizacao = now,
            PlanoId = plano.Id,
            AssinaturaStatus = "TRIAL",
            TrialExpiraEm = now.AddDays(14),
            StripeCustomerId = null,
            StripeSubscriptionId = null,
            AssinaturaExpiraEm = null
        };

        _db.Usuarios.Add(usuario);

        var pendentes = _db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => $"{e.Entity.GetType().Name}:{e.State}")
            .ToList();

        _logger.LogInformation(
            "Cadastro: tentando salvar usuário. Nome={Nome}, Email={Email}, PlanoId={PlanoId}, Status={Status}, HashLength={HashLength}",
            usuario.Nome,
            usuario.Email,
            usuario.PlanoId,
            usuario.AssinaturaStatus,
            usuario.SenhaHash?.Length ?? 0);

        _logger.LogInformation("Entidades pendentes antes do SaveChanges: {Pendentes}", string.Join(", ", pendentes));

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            var mensagemCompleta = ObterMensagemCompleta(ex);
            _logger.LogError(ex, "Cadastro: erro completo ao salvar usuário no MySQL: {MensagemCompleta}", mensagemCompleta);
            throw new InvalidOperationException($"Erro ao salvar usuário no banco: {mensagemCompleta}", ex);
        }

        _logger.LogInformation("Cadastro: usuário salvo com sucesso. UsuarioId={UsuarioId}", usuario.Id);

        try
        {
            await _usuarioPadraoService.CriarEstruturaPadraoAsync(usuario.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Usuário criado, mas estrutura padrão falhou. UsuarioId={UsuarioId}", usuario.Id);
        }

        await GarantirAdminComHashAsync();

        try
        {
            await _emailService.EnviarBoasVindasAsync(usuario);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de boas-vindas para usuário {UsuarioId}.", usuario.Id);
        }

        return (true, "Usuário cadastrado com sucesso.");
    }

    public async Task<CadastroUsuarioResultado> CadastrarUsuarioAsync(CadastroUsuarioModel cadastro, int planoId)
    {
        var result = await CadastrarAsync(cadastro, planoId);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        var usuario = await _db.Usuarios
            .OrderByDescending(u => u.Id)
            .FirstOrDefaultAsync(u => u.Email == cadastro.Email.Trim().ToLowerInvariant());

        if (usuario is null)
            throw new InvalidOperationException("Não foi possível concluir o cadastro.");

        string? checkoutUrl = null;
        try
        {
            _logger.LogInformation("Cadastro: iniciando checkout Stripe para usuário {UsuarioId}.", usuario.Id);
            checkoutUrl = await _stripeSubscriptionService.CriarCheckoutSessionAsync(usuario.Id, planoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Usuário criado, mas Stripe falhou. UsuarioId={UsuarioId}", usuario.Id);
        }

        return new CadastroUsuarioResultado { UsuarioId = usuario.Id, CheckoutUrl = checkoutUrl };
    }

    public async Task<(bool Success, string Message, string? CheckoutUrl)> CadastrarEIniciarCheckoutAsync(CadastroUsuarioModel cadastro, int planoId)
    {
        try
        {
            var resultado = await CadastrarUsuarioAsync(cadastro, planoId);
            return (true, "Usuário cadastrado com sucesso.", resultado.CheckoutUrl);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, null);
        }
    }

    private static string ObterMensagemCompleta(Exception ex)
    {
        var mensagens = new List<string>();
        Exception? atual = ex;

        while (atual != null)
        {
            mensagens.Add(atual.Message);
            atual = atual.InnerException;
        }

        return string.Join(" | ", mensagens);
    }

    private async Task GarantirAdminComHashAsync()
    {
        var admin = await _db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "admin@granaok.com");
        if (admin is null)
            return;

        if (admin.SenhaHash == "123456")
        {
            admin.SenhaHash = _passwordHasher.HashPassword("123456");
            admin.DataAtualizacao = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
