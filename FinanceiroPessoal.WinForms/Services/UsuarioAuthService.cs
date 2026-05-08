using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services;

public class UsuarioAuthService
{
    public Usuario? UsuarioAtual { get; private set; }

    public Usuario? Login(string email, string senha)
    {
        using var db = new FinanceiroDbContext();
        var hash = GerarSha256(senha);
        var usuario = db.Usuarios.FirstOrDefault(x => x.Email == email && x.SenhaHash == hash && x.Ativo);
        if (usuario is null) return null;

        usuario.UltimoLogin = DateTime.Now;
        usuario.DataAtualizacao = DateTime.Now;
        db.SaveChanges();
        UsuarioAtual = usuario;
        return usuario;
    }

    public (bool ok, string mensagem) Cadastrar(string nome, string email, string senha)
    {
        using var db = new FinanceiroDbContext();
        if (db.Usuarios.Any(x => x.Email == email)) return (false, "E-mail já cadastrado.");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = GerarSha256(senha),
            Ativo = true,
            EmailConfirmado = true,
            DataCriacao = DateTime.Now,
            DataAtualizacao = DateTime.Now
        };

        db.Usuarios.Add(usuario);
        db.SaveChanges();

        CriarTabelasDoUsuario(db, usuario.Id);
        return (true, "Usuário cadastrado com sucesso.");
    }

    private static void CriarTabelasDoUsuario(FinanceiroDbContext db, int usuarioId)
    {
        db.Database.ExecuteSqlRaw($"CREATE TABLE IF NOT EXISTS categorias_u{usuarioId} (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT NOT NULL);");
        db.Database.ExecuteSqlRaw($"CREATE TABLE IF NOT EXISTS contas_u{usuarioId} (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT NOT NULL, tipo TEXT NOT NULL);");
        db.Database.ExecuteSqlRaw($"CREATE TABLE IF NOT EXISTS pessoas_u{usuarioId} (id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT NOT NULL);");
        db.Database.ExecuteSqlRaw($"CREATE TABLE IF NOT EXISTS lancamentos_u{usuarioId} (id INTEGER PRIMARY KEY AUTOINCREMENT, descricao TEXT NOT NULL, valor REAL NOT NULL, status TEXT, tipo TEXT, dataVencimento TEXT, competencia TEXT);");
    }

    private static string GerarSha256(string senha)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(senha));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
