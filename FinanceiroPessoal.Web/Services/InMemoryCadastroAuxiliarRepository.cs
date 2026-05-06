using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryCadastroAuxiliarRepository : ICadastroAuxiliarRepository
{
    private readonly List<Categoria> _categorias = new() { new() { Id = 1, Nome = "Geral" } };
    private readonly List<Conta> _contas = new() { new() { Id = 1, Nome = "Principal", Tipo = "Conta corrente" } };
    private readonly List<Pessoa> _pessoas = new() { new() { Id = 1, Nome = "Padrão" } };

    public Task<List<Pessoa>> ObterPessoas() => Task.FromResult(_pessoas.ToList());
    public Task<List<Categoria>> ObterCategorias() => Task.FromResult(_categorias.ToList());
    public Task<List<Conta>> ObterContas() => Task.FromResult(_contas.ToList());
}
