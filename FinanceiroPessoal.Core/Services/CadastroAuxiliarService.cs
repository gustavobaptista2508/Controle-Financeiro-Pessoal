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
}