using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryLancamentoRepository : ILancamentoRepository
{
    private readonly List<Lancamento> _dados = new();

    public Task Adicionar(Lancamento lancamento)
    {
        if (lancamento.Id == Guid.Empty) lancamento.Id = Guid.NewGuid();
        _dados.Add(lancamento);
        return Task.CompletedTask;
    }

    public Task Atualizar(Lancamento lancamento)
    {
        var idx = _dados.FindIndex(x => x.Id == lancamento.Id);
        if (idx >= 0) _dados[idx] = lancamento;
        return Task.CompletedTask;
    }

    public Task Excluir(Guid id)
    {
        _dados.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task<List<Lancamento>> Filtrar(string? descricao, string? categoria, string? tipo, DateTime? inicio = null, DateTime? fim = null)
    {
        IEnumerable<Lancamento> q = _dados;
        if (!string.IsNullOrWhiteSpace(descricao)) q = q.Where(x => x.Descricao.Contains(descricao, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todos") q = q.Where(x => x.Categoria == categoria);
        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos") q = q.Where(x => x.Tipo == tipo);
        if (inicio.HasValue) q = q.Where(x => x.Data >= inicio.Value);
        if (fim.HasValue) q = q.Where(x => x.Data <= fim.Value);
        return Task.FromResult(q.ToList());
    }

    public Task<Lancamento?> ObterPorId(Guid id) => Task.FromResult(_dados.FirstOrDefault(x => x.Id == id));
    public Task<List<Lancamento>> ObterTodos() => Task.FromResult(_dados.ToList());
}
