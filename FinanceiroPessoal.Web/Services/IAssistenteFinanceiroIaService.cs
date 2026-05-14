namespace FinanceiroPessoal.Web.Services;

public interface IAssistenteFinanceiroIaService
{
    Task<string> GerarResumoMensalAsync(int usuarioId, int mes, int ano);
    Task<string> PerguntarAsync(int usuarioId, string pergunta);
    Task<string> AnalisarCategoriasAsync(int usuarioId, int mes, int ano);
    Task<bool> PodeUsarIaHojeAsync(int usuarioId);
    Task<IReadOnlyList<FinanceiroPessoal.Core.Models.IaConversa>> ListarUltimasConversasAsync(int usuarioId, int limite = 10);
}
