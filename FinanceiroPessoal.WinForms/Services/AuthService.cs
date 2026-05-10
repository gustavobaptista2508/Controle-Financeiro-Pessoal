using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using System.Security.Cryptography;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public class AuthService
    {
        private readonly string _chaveArquivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FinanceiroPessoal", "auth.key");

        public Usuario? UsuarioAtual { get; private set; }

        public bool ChaveExiste() => File.Exists(_chaveArquivo);

        public string GerarNovaChave()
        {
            var chave = KeyGeneration.GenerateRandomKey(20);
            var base32 = Base32Encoding.ToString(chave);
            Directory.CreateDirectory(Path.GetDirectoryName(_chaveArquivo)!);
            var bytes = Encoding.UTF8.GetBytes(base32);
            var protegido = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_chaveArquivo, protegido);
            return base32;
        }

        public bool VerificarCodigo(string codigo)
        {
            try
            {
                var protegido = File.ReadAllBytes(_chaveArquivo);
                var bytes = ProtectedData.Unprotect(protegido, null, DataProtectionScope.CurrentUser);
                var base32 = Encoding.UTF8.GetString(bytes);
                var chave = Base32Encoding.ToBytes(base32);
                var totp = new Totp(chave);
                return totp.VerifyTotp(codigo, out _, new VerificationWindow(previous: 1, future: 1));
            }
            catch { return false; }
        }

        public string GerarUriQrCode(string base32, string usuario = "Financeiro Pessoal")
            => $"otpauth://totp/{Uri.EscapeDataString(usuario)}?secret={base32}&issuer={Uri.EscapeDataString("FinanceiroPessoal")}";

        public Usuario? LoginUsuario(string email, string senha)
        {
            using var db = new FinanceiroDbContext();
            var hash = GerarSha256(senha);
            var usuario = db.Usuarios.FirstOrDefault(x => x.Email == email && x.SenhaHash == hash && x.Ativo);
            if (usuario is null) return null;
            usuario.UltimoLogin = DateTime.Now;
            usuario.DataAtualizacao = DateTime.Now;
            db.SaveChanges();
            UsuarioAtual = usuario;
            SessaoUsuario.UsuarioId = usuario.Id;
            SessaoUsuario.Nome = usuario.Nome;
            return usuario;
        }

        public (bool ok, string mensagem) CadastrarUsuario(string nome, string email, string senha)
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
            CriarDadosPadraoDoUsuario(db, usuario.Id);
            return (true, "Usuário cadastrado com sucesso.");
        }

        private static void CriarDadosPadraoDoUsuario(FinanceiroDbContext db, int usuarioId)
        {
            db.Categorias.Add(new Categoria { Nome = "Geral", UsuarioId = usuarioId });
            db.Contas.Add(new Conta { Nome = "Conta Principal", Tipo = "Corrente", UsuarioId = usuarioId });
            db.Pessoas.Add(new Pessoa { Nome = "Sem pessoa", UsuarioId = usuarioId });
            db.SaveChanges();
        }

        private static string GerarSha256(string senha)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(senha));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
