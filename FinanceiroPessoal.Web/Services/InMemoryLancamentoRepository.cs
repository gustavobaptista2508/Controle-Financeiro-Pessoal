using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;

namespace FinanceiroPessoal.Web.Services;

public class InMemoryLancamentoRepository : ILancamentoRepository
{
    private readonly List<Lancamento> _dados = new();
    private int _nextId = 1;

    public Task<List<Lancamento>> ObterTodos() => Task.FromResult(_dados.OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());

    public Task<Lancamento?> ObterPorId(int id) => Task.FromResult(_dados.FirstOrDefault(x => x.Id == id));

    public Task Adicionar(Lancamento lancamento)
    {
        if (lancamento.Id <= 0) lancamento.Id = _nextId++;
        _dados.Add(lancamento);
        return Task.CompletedTask;
    }

    public Task Atualizar(Lancamento lancamento)
    {
        var idx = _dados.FindIndex(x => x.Id == lancamento.Id);
        if (idx >= 0) _dados[idx] = lancamento;
        return Task.CompletedTask;
    }

    public Task Excluir(int id)
    {
        _dados.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task MarcarComoPago(int id, DateTime? dataPagamento = null)
    {
        var lanc = _dados.FirstOrDefault(x => x.Id == id);
        if (lanc is not null)
        {
            lanc.Status = "Pago";
            lanc.DataPagamento = dataPagamento ?? DateTime.Now;
        }

        return Task.CompletedTask;
    }

    public Task<List<Lancamento>> Filtrar(string pessoa, string status, string tipo, DateTime dataIni, DateTime dataFim)
    {
        IEnumerable<Lancamento> q = _dados;

        if (!string.IsNullOrWhiteSpace(pessoa) && pessoa != "Todos")
            q = q.Where(x => x.Pessoa is not null && x.Pessoa.Nome == pessoa);

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
            q = q.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
        {
            if (tipo == "Entrada") q = q.Where(x => x.Tipo == TipoLancamento.Entrada);
            else if (tipo == "Saída") q = q.Where(x => x.Tipo == TipoLancamento.Saida);
        }

        q = q.Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value >= dataIni && x.DataVencimento.Value <= dataFim.Date.AddDays(1).AddTicks(-1));

        return Task.FromResult(q.OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());
    }

    public Task<decimal> CalcularSaldoConta(string pessoa, string status, string tipo, DateTime? dataIni, DateTime? dataFim)
    {
        IEnumerable<Lancamento> entradas = _dados.Where(x => x.Tipo == TipoLancamento.Entrada);
        IEnumerable<Lancamento> saidasPagas = _dados.Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pago");

        if (!string.IsNullOrWhiteSpace(pessoa) && pessoa != "Todos")
        {
            entradas = entradas.Where(x => x.Pessoa is not null && x.Pessoa.Nome == pessoa);
            saidasPagas = saidasPagas.Where(x => x.Pessoa is not null && x.Pessoa.Nome == pessoa);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
        {
            entradas = entradas.Where(x => x.Status == status);
            saidasPagas = saidasPagas.Where(x => x.Status == status);
        }

        if (dataIni.HasValue)
        {
            entradas = entradas.Where(x => x.DataVencimento >= dataIni.Value);
            saidasPagas = saidasPagas.Where(x => x.DataVencimento >= dataIni.Value);
        }

        if (dataFim.HasValue)
        {
            var end = dataFim.Value.Date.AddDays(1).AddTicks(-1);
            entradas = entradas.Where(x => x.DataVencimento <= end);
            saidasPagas = saidasPagas.Where(x => x.DataVencimento <= end);
        }

        return Task.FromResult(entradas.Sum(x => x.Valor) - saidasPagas.Sum(x => x.Valor));
    }

    public Task<decimal> ObterTotalPendenteSaidas() => Task.FromResult(_dados.Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pendente").Sum(x => x.Valor));

    public Task<decimal> ObterTotalPagoSaidas() => Task.FromResult(_dados.Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pago").Sum(x => x.Valor));

    public Task<List<Lancamento>> ObterLancamentosPorPeriodoAsync(DateTime dataIni, DateTime dataFim)
        => Task.FromResult(_dados.Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value >= dataIni && x.DataVencimento.Value <= dataFim.Date.AddDays(1).AddTicks(-1)).OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());

    public Task<List<Lancamento>> ObterVencimentosSemanaAsync(DateTime dataIni, DateTime dataFim)
        => ObterLancamentosPorPeriodoAsync(dataIni, dataFim);

    public Task<List<Lancamento>> ObterAtrasadosAsync()
    {
        var hoje = DateTime.Today;
        return Task.FromResult(_dados.Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value.Date < hoje && x.Status != "Pago").OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());
    }

    public Task<List<Lancamento>> ObterVencemHojeAsync(DateTime dataReferencia)
        => Task.FromResult(_dados.Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value.Date == dataReferencia.Date).OrderBy(x => x.DataVencimento).ThenBy(x => x.Descricao).ToList());

    public Task<List<ProximoVencimentoDto>> ObterProximosVencimentosAsync(int quantidade)
    {
        var result = _dados
            .Where(x => x.DataVencimento.HasValue && x.Tipo == TipoLancamento.Saida && x.Status != "Pago")
            .OrderBy(x => x.DataVencimento)
            .Take(quantidade)
            .Select(x => new ProximoVencimentoDto
            {
                Id = x.Id,
                Vencimento = x.DataVencimento,
                Descricao = x.Descricao,
                Valor = x.Valor,
                Conta = x.Conta?.Nome ?? string.Empty,
                Pessoa = x.Pessoa?.Nome ?? string.Empty,
                Status = x.Status,
                Tipo = x.Tipo == TipoLancamento.Entrada ? "Entrada" : "Saída"
            })
            .ToList();

        return Task.FromResult(result);
    }

    public Task<List<Lancamento>> ObterPagosPorPeriodoAsync(DateTime inicio, DateTime fim)
        => Task.FromResult(_dados.Where(x => x.Status == "Pago" && x.DataPagamento.HasValue && x.DataPagamento.Value.Date >= inicio.Date && x.DataPagamento.Value.Date <= fim.Date).ToList());
}
