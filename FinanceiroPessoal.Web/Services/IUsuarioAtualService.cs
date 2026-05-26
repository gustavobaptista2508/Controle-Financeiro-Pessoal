using FinanceiroPessoal.Core.Models;

namespace FinanceiroPessoal.Web.Services;

public interface IUsuarioAtualService
{
    Task<int?> ObterUsuarioIdAsync();
    Task<Usuario?> ObterUsuarioAsync();
}
