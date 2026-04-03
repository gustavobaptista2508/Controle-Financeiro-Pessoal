using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;

namespace FinanceiroPessoal.WinForms.Services;

public class CadastroAuxiliarService
{
    public List<Categoria> ObterCategorias()
    {
        using var context = new FinanceiroDbContext();
        return context.Categorias.OrderBy(x => x.Nome).ToList();
    }

    public List<Conta> ObterContas()
    {
        using var context = new FinanceiroDbContext();
        return context.Contas.OrderBy(x => x.Nome).ToList();
    }

    public List<Pessoa> ObterPessoas()
    {
        using var context = new FinanceiroDbContext();
        return context.Pessoas.OrderBy(x => x.Nome).ToList();
    }
}