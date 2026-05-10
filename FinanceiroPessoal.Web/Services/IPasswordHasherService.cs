namespace FinanceiroPessoal.Web.Services;

public interface IPasswordHasherService
{
    string HashPassword(string senha);
    bool VerifyPassword(string senha, string hash);
}
