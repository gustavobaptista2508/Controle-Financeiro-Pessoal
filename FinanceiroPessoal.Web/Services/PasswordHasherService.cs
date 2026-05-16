using Microsoft.AspNetCore.Identity;

namespace FinanceiroPessoal.Web.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string HashPassword(string senha)
    {
        return _hasher.HashPassword(new object(), senha);
    }

    public bool VerifyPassword(string senha, string hash)
    {
        if (string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(hash))
            return false;

        var result = _hasher.VerifyHashedPassword(new object(), hash, senha);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
