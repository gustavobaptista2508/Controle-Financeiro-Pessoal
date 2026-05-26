namespace FinanceiroPessoal.Web.Services;

public interface IAssinaturaService
{
    Task<bool> UsuarioTemAcessoAsync(int usuarioId);
    Task<bool> UsuarioPossuiPlusAsync(int usuarioId);
}
