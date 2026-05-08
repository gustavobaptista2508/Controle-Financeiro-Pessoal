using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Core.Services;

public class CadastroAuxiliarService
{
    public readonly ICadastroAuxiliarRepository _repository;

    public CadastroAuxiliarService(ICadastroAuxiliarRepository repository)
    {
        _repository = repository;
    }
    public async Task<List<Categoria>> ObterCategorias()
    {
        return await _repository.ObterCategorias();
    }

    public async Task<List<Conta>> ObterContas()
    {
        return await _repository.ObterContas();
    }

    public async Task<List<Pessoa>> ObterPessoas()
    {
        return await _repository.ObterPessoas();
    }

    public async Task<Categoria> AdicionarCategoriaAsync(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da categoria é obrigatório.", nameof(nome));

        var categoria = new Categoria { Nome = nome.Trim() };
        return await _repository.AdicionarCategoriaAsync(categoria);
    }
    public async Task<Pessoa> AdicionarPessoaAsync(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da pessoa é obrigatório.", nameof(nome));

        var pessoa = new Pessoa { Nome = nome.Trim() };
        return await _repository.AdicionarPessoaAsync(pessoa);
    }

    public async Task<Conta> AdicionarContaAsync(string nome, string tipo)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(tipo))
            throw new ArgumentException("Nome e tipo da conta são obrigatórios.");

        var conta = new Conta { Nome = nome.Trim(), Tipo = tipo.Trim() };
        return await _repository.AdicionarContaAsync(conta);
    }

}