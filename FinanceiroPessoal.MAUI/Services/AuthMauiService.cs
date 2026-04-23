using OtpNet;
using System.Security.Cryptography;

namespace FinanceiroPessoal.MAUI.Services
{
    public class AuthMauiService
    {
        private const string ChavePreferencia = "totp_secret_key";

        public bool ChaveExiste()
            => Preferences.ContainsKey(ChavePreferencia);

        public string GerarNovaChave()
        {
            var chave = KeyGeneration.GenerateRandomKey(20);
            var base32 = Base32Encoding.ToString(chave);
            Preferences.Set(ChavePreferencia, base32);
            return base32;
        }

        public bool VerificarCodigo(string codigo)
        {
            try
            {
                var base32 = Preferences.Get(ChavePreferencia, string.Empty);
                if (string.IsNullOrEmpty(base32)) return false;

                var chave = Base32Encoding.ToBytes(base32);
                var totp = new Totp(chave);

                return totp.VerifyTotp(codigo, out _,
                    new VerificationWindow(previous: 1, future: 1));
            }
            catch { return false; }
        }

        public string GerarUriQrCode(string base32)
            => $"otpauth://totp/FinanceiroPessoal?secret={base32}&issuer=FinanceiroPessoal";
    }
}