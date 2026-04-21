using OtpNet;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public class AuthService
    {
        // Chave salva em arquivo local criptografado
        private readonly string _chaveArquivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FinanceiroPessoal", "auth.key");

        public bool ChaveExiste() => File.Exists(_chaveArquivo);

        public string GerarNovaChave()
        {
            var chave = KeyGeneration.GenerateRandomKey(20);
            var base32 = Base32Encoding.ToString(chave);

            Directory.CreateDirectory(Path.GetDirectoryName(_chaveArquivo)!);

            // Salva criptografado com DPAPI (proteção do Windows por usuário)
            var bytes = System.Text.Encoding.UTF8.GetBytes(base32);
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
                var base32 = System.Text.Encoding.UTF8.GetString(bytes);

                var chave = Base32Encoding.ToBytes(base32);
                var totp = new Totp(chave);

                // Verifica com janela de ±1 intervalo (30s) para tolerância de clock
                return totp.VerifyTotp(
                    codigo,
                    out _,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch
            {
                return false;
            }
        }

        public string GerarUriQrCode(string base32, string usuario = "Financeiro Pessoal")
        {
            return $"otpauth://totp/{Uri.EscapeDataString(usuario)}" +
                   $"?secret={base32}&issuer={Uri.EscapeDataString("FinanceiroPessoal")}";
        }
    }
}
