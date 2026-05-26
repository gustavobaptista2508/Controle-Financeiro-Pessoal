using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Services;

public class CadastroAuxiliarService
{
    public readonly ICadastroAuxiliarRepository _repository;
    private readonly FinanceiroDbContext _db;

    public CadastroAuxiliarService(ICadastroAuxiliarRepository repository, FinanceiroDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public Task<List<Categoria>> ObterCategorias() => _repository.ObterCategorias();

    public Task<List<Conta>> ObterContas() => _repository.ObterContas();

    public Task<List<Pessoa>> ObterPessoas() => _repository.ObterPessoas();

    public Task<List<Categoria>> ObterCategorias(int usuarioId)
        => _db.Categorias.Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.Nome).ToListAsync();

    public Task<List<Conta>> ObterContas(int usuarioId)
        => _db.Contas.Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.Nome).ToListAsync();

    public Task<List<Pessoa>> ObterPessoas(int usuarioId)
        => _db.Pessoas.Where(x => x.UsuarioId == usuarioId).OrderBy(x => x.Nome).ToListAsync();

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
