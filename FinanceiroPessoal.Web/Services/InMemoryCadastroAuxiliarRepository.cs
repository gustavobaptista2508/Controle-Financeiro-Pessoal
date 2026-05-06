using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryCadastroAuxiliarRepository : ICadastroAuxiliarRepository
{
    private readonly List<string> _categorias = new() { "Geral" };
    private readonly List<string> _contas = new() { "Principal" };
    private readonly List<string> _pessoas = new() { "Padrão" };

    public Task<List<string>> ObterCategorias() => Task.FromResult(_categorias.ToList());
    public Task<List<string>> ObterContas() => Task.FromResult(_contas.ToList());
    public Task<List<string>> ObterPessoas() => Task.FromResult(_pessoas.ToList());
    public Task AdicionarCategoria(string categoria) { if(!_categorias.Contains(categoria)) _categorias.Add(categoria); return Task.CompletedTask; }
    public Task AdicionarConta(string conta) { if(!_contas.Contains(conta)) _contas.Add(conta); return Task.CompletedTask; }
    public Task AdicionarPessoa(string pessoa) { if(!_pessoas.Contains(pessoa)) _pessoas.Add(pessoa); return Task.CompletedTask; }
}
