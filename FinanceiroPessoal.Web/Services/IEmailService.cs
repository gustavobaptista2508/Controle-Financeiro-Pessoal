using FinanceiroPessoal.Core.Models;

namespace FinanceiroPessoal.Web.Services;

public interface IEmailService
{
    Task EnviarBoasVindasAsync(Usuario usuario);
    Task EnviarLembreteTrialAsync(Usuario usuario, int diasRestantes);
    Task EnviarTrialEncerradoAsync(Usuario usuario);
}
