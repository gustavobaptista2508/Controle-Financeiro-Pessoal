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

    public Task<Categoria> AdicionarCategoriaAsync(Categoria categoria)
    {
        categoria.Id = _categorias.Count == 0 ? 1 : _categorias.Max(x => x.Id) + 1;
        _categorias.Add(categoria);
        return Task.FromResult(categoria);
    }

    public Task<Pessoa> AdicionarPessoaAsync(Pessoa pessoa)
    {
        pessoa.Id = _pessoas.Count == 0 ? 1 : _pessoas.Max(x => x.Id) + 1;
        _pessoas.Add(pessoa);
        return Task.FromResult(pessoa);
    }

    public Task<Conta> AdicionarContaAsync(Conta conta)
    {
        conta.Id = _contas.Count == 0 ? 1 : _contas.Max(x => x.Id) + 1;
        _contas.Add(conta);
        return Task.FromResult(conta);
    }
}
