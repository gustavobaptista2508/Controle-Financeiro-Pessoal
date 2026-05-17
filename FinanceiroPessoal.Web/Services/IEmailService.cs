using FinanceiroPessoal.Core.Models;

namespace FinanceiroPessoal.Web.Services;

public interface IEmailService
{
    Task<bool> EnviarBoasVindasAsync(Usuario usuario);
    Task<bool> EnviarLembreteTrialAsync(Usuario usuario, int diasRestantes);
    Task<bool> EnviarTrialEncerradoAsync(Usuario usuario);
    Task<bool> EnviarRecuperacaoSenhaAsync(Usuario usuario, string linkRedefinicao);
}
