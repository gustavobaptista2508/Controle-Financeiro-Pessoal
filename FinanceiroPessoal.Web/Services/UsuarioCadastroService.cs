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
        string nome = cadastro.Nome.Trim();
        string email = cadastro.Email.Trim().ToLowerInvariant();
        string senha = cadastro.Senha;

        if (string.IsNullOrWhiteSpace(nome)) return (false, "Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email)) return (false, "E-mail é obrigatório.");
        if (!email.Contains('@')) return (false, "E-mail inválido.");
        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 6) return (false, "A senha deve ter no mínimo 6 caracteres.");


        var emailJaExiste = await _db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
        if (emailJaExiste)
        {
            return (false, "E-mail já cadastrado.");
        }

        var now = DateTime.Now;
        const int trialDays = 14;
        var trialEndsAt = now.AddDays(trialDays);

        if (planoId.HasValue)
        {
            var planoAtivo = await _db.Planos.AnyAsync(p => p.Id == planoId.Value && p.Ativo);
            if (!planoAtivo)
            {
                return (false, "Plano inválido ou inativo.");
            }
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = _passwordHasher.HashPassword(senha),
            Ativo = true,
            EmailConfirmado = true,
            DataCriacao = now,
            DataAtualizacao = now,
            PlanoId = planoId,
            AssinaturaStatus = "PENDENTE",
            TrialExpiraEm = trialEndsAt,
            AssinaturaExpiraEm = trialEndsAt
        };

        _db.Usuarios.Add(usuario);
        var linhas = await _db.SaveChangesAsync();
        _logger.LogInformation("Cadastro: usuário salvo com ID {UsuarioId}.", usuario.Id);

        await _usuarioPadraoService.CriarEstruturaPadraoAsync(usuario.Id);

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

        _logger.LogInformation("Cadastro: usuário salvo com ID {UsuarioId}.", usuario.Id);

        try
        {
            _logger.LogInformation("Cadastro: iniciando checkout Stripe para usuário {UsuarioId}.", usuario.Id);
            var checkoutUrl = await _stripeSubscriptionService.CriarCheckoutSessionAsync(usuario.Id, planoId);
            return new CadastroUsuarioResultado { UsuarioId = usuario.Id, CheckoutUrl = checkoutUrl };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cadastro criado, mas checkout Stripe falhou para usuário {UsuarioId}.", usuario.Id);
            return new CadastroUsuarioResultado { UsuarioId = usuario.Id, CheckoutUrl = null };
        }
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

    private async Task GarantirAdminComHashAsync()
    {
        var admin = await _db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "admin@granaok.com");
        if (admin is null)
            return;

        if (admin.SenhaHash == "123456")
        {
            admin.SenhaHash = _passwordHasher.HashPassword("123456");
            admin.DataAtualizacao = DateTime.Now;
            await _db.SaveChangesAsync();
        }
    }
}
