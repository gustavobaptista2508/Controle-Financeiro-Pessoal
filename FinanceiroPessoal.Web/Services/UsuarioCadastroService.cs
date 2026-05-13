using System.Linq;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

using FinanceiroPessoal.Web.Models;

namespace FinanceiroPessoal.Web.Services;

public class UsuarioCadastroService
{
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasherService _passwordHasher;

    public UsuarioCadastroService(IConfiguration configuration, IPasswordHasherService passwordHasher)
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
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

        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? _configuration.GetConnectionString("MySqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Conexão com banco de dados não configurada (DefaultConnection/MySqlConnection).");
        }

        Console.WriteLine($"DEBUG MYSQL: ConnectionString carregada para cadastro (host): {ExtractServer(connectionString)}");
        await using var db = new MySqlDbContext(connectionString);

        var emailJaExiste = await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
        if (emailJaExiste)
        {
            return (false, "E-mail já cadastrado.");
        }

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = _passwordHasher.HashPassword(senha),
            Ativo = true,
            EmailConfirmado = true,
            DataCriacao = DateTime.Now,
            DataAtualizacao = DateTime.Now,
            PlanoId = planoId,
            AssinaturaStatus = planoId.HasValue ? "PENDENTE" : "PENDENTE"
        };

        db.Usuarios.Add(usuario);
        var linhas = await db.SaveChangesAsync();
        Console.WriteLine($"DEBUG CADASTRO SERVICE: linhas salvas {linhas}");
        Console.WriteLine($"DEBUG CADASTRO SERVICE: UsuarioId criado: {usuario.Id}");

        db.Contas.Add(new Conta
        {
            Nome = "Conta Principal",
            Tipo = "Corrente",
            UsuarioId = usuario.Id
        });

        db.Categorias.AddRange(
            new Categoria { Nome = "Alimentação", UsuarioId = usuario.Id },
            new Categoria { Nome = "Moradia", UsuarioId = usuario.Id },
            new Categoria { Nome = "Transporte", UsuarioId = usuario.Id },
            new Categoria { Nome = "Saúde", UsuarioId = usuario.Id },
            new Categoria { Nome = "Lazer", UsuarioId = usuario.Id },
            new Categoria { Nome = "Salário", UsuarioId = usuario.Id }
        );

        await db.SaveChangesAsync();

        await GarantirAdminComHashAsync(db);

        return (true, "Usuário cadastrado com sucesso.");
    }
    private static string ExtractServer(string connectionString)
    {
        const string key = "Server=";
        var part = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(x => x.TrimStart().StartsWith(key, StringComparison.OrdinalIgnoreCase));
        return part ?? "server=indefinido";
    }

    private async Task GarantirAdminComHashAsync(MySqlDbContext db)
    {
        var admin = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == "admin@granaok.com");
        if (admin is null)
            return;

        if (admin.SenhaHash == "123456")
        {
            admin.SenhaHash = _passwordHasher.HashPassword("123456");
            admin.DataAtualizacao = DateTime.Now;
            await db.SaveChangesAsync();
        }
    }
}
