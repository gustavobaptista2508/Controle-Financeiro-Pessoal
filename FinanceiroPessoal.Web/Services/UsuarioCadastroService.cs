using System.Linq;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

using FinanceiroPessoal.Web.Models;

namespace FinanceiroPessoal.Web.Services;

public class UsuarioCadastroService
{
    private readonly FinanceiroDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<UsuarioCadastroService> _logger;

    public UsuarioCadastroService(FinanceiroDbContext db, IPasswordHasherService passwordHasher, IEmailService emailService, ILogger<UsuarioCadastroService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> CadastrarAsync(CadastroUsuarioModel cadastro, int? planoId = null)
    {
        Console.WriteLine("DEBUG CADASTRO SERVICE: chamado");
        Console.WriteLine($"DEBUG CADASTRO SERVICE: email recebido {cadastro.Email}");
        Console.WriteLine($"DEBUG CADASTRO SERVICE: senha preenchida {!string.IsNullOrEmpty(cadastro.Senha)}");

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
            AssinaturaStatus = "TRIAL",
            TrialExpiraEm = trialEndsAt,
            AssinaturaExpiraEm = trialEndsAt
        };

        _db.Usuarios.Add(usuario);
        var linhas = await _db.SaveChangesAsync();
        Console.WriteLine($"DEBUG CADASTRO SERVICE: linhas salvas {linhas}");
        Console.WriteLine($"DEBUG CADASTRO SERVICE: UsuarioId criado: {usuario.Id}");

        _db.Contas.Add(new Conta
        {
            Nome = "Conta Principal",
            Tipo = "Corrente",
            UsuarioId = usuario.Id
        });

        _db.Categorias.AddRange(
            new Categoria { Nome = "Alimentação", UsuarioId = usuario.Id },
            new Categoria { Nome = "Moradia", UsuarioId = usuario.Id },
            new Categoria { Nome = "Transporte", UsuarioId = usuario.Id },
            new Categoria { Nome = "Saúde", UsuarioId = usuario.Id },
            new Categoria { Nome = "Lazer", UsuarioId = usuario.Id },
            new Categoria { Nome = "Salário", UsuarioId = usuario.Id }
        );

        await _db.SaveChangesAsync();

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
