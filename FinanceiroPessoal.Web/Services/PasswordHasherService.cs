namespace FinanceiroPessoal.Web.Services;

public class PasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    public bool VerifyPassword(string senha, string hash)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch
        {
            return false;
        }
    }
}
