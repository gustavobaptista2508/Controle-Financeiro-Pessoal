using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryCadastroAuxiliarRepository(WebAuthSessionService session) : ICadastroAuxiliarRepository
{
    private TenantData Current => MultiTenantDataStore.GetOrCreate(session.CurrentUserEmail ?? "guest@local");

    public Task<List<Pessoa>> ObterPessoas() => Task.FromResult(Current.Pessoas.ToList());
    public Task<List<Categoria>> ObterCategorias() => Task.FromResult(Current.Categorias.ToList());
    public Task<List<Conta>> ObterContas() => Task.FromResult(Current.Contas.ToList());

    public Task<Categoria> AdicionarCategoriaAsync(Categoria categoria)
    {
        categoria.Id = Current.Categorias.Count == 0 ? 1 : Current.Categorias.Max(x => x.Id) + 1;
        Current.Categorias.Add(categoria);
        return Task.FromResult(categoria);
    }

    public Task<Pessoa> AdicionarPessoaAsync(Pessoa pessoa)
    {
        pessoa.Id = Current.Pessoas.Count == 0 ? 1 : Current.Pessoas.Max(x => x.Id) + 1;
        Current.Pessoas.Add(pessoa);
        return Task.FromResult(pessoa);
    }

    public Task<Conta> AdicionarContaAsync(Conta conta)
    {
        conta.Id = Current.Contas.Count == 0 ? 1 : Current.Contas.Max(x => x.Id) + 1;
        Current.Contas.Add(conta);
        return Task.FromResult(conta);
    }
}
