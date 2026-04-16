using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.WinForms.Services;

public class LancamentoService
{
    public List<Lancamento> ObterTodos()
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Include(x => x.Categoria)
            .Include(x => x.Conta)
            .Include(x => x.Pessoa)
            .OrderBy(x => x.DataVencimento)
            .ThenBy(x => x.Descricao)
            .ToList();
    }

    public Lancamento? ObterPorId(int id)
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Include(x => x.Categoria)
            .Include(x => x.Conta)
            .Include(x => x.Pessoa)
            .FirstOrDefault(x => x.Id == id);
    }

    public void Adicionar(Lancamento lancamento)
    {
        using var context = new FinanceiroDbContext();
        context.Lancamentos.Add(lancamento);
        context.SaveChanges();
    }

    public void Atualizar(Lancamento lancamento)
    {
        using var context = new FinanceiroDbContext();

        context.Lancamentos.Update(lancamento);
        context.SaveChanges();
    }

    public void Excluir(int id)
    {
        using var context = new FinanceiroDbContext();

        var lancamento = context.Lancamentos.FirstOrDefault(x => x.Id == id);
        if (lancamento == null)
            return;

        context.Lancamentos.Remove(lancamento);
        context.SaveChanges();
    }

    public void MarcarComoPago(int id, DateTime? dataPagamento = null)
    {
        using var context = new FinanceiroDbContext();

        var lancamento = context.Lancamentos.FirstOrDefault(x => x.Id == id);
        if (lancamento == null)
            return;

        lancamento.Status = "Pago";
        lancamento.DataPagamento = dataPagamento ?? DateTime.Now;

        context.SaveChanges();
    }

    public decimal ObterTotalPendenteSaidas()
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pendente")
            .Select(x => x.Valor)
            .DefaultIfEmpty(0)
            .Sum();
    }

    public decimal ObterTotalPagoSaidas()
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pago")
            .Select(x => x.Valor)
            .DefaultIfEmpty(0)
            .Sum();
    }

    public decimal CalcularSaldoConta(string? pessoa = null, string? status = null, string? tipo = null,
                                 DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        using var context = new FinanceiroDbContext();

        var queryEntradas = context.Lancamentos
            .Where(x => x.Tipo == TipoLancamento.Entrada);

        var querySaidasPagas = context.Lancamentos
            .Where(x => x.Tipo == TipoLancamento.Saida && x.Status == "Pago");

        // Aplica filtros iguais ao Filtrar()
        if (!string.IsNullOrWhiteSpace(pessoa) && pessoa != "Todos")
        {
            queryEntradas = queryEntradas.Where(x => x.Pessoa != null && x.Pessoa.Nome == pessoa);
            querySaidasPagas = querySaidasPagas.Where(x => x.Pessoa != null && x.Pessoa.Nome == pessoa);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
        {
            queryEntradas = queryEntradas.Where(x => x.Status == status);
            querySaidasPagas = querySaidasPagas.Where(x => x.Status == status);
        }

        if (dataInicio.HasValue)
        {
            queryEntradas = queryEntradas.Where(x => x.DataVencimento.HasValue && x.DataVencimento >= dataInicio);
            querySaidasPagas = querySaidasPagas.Where(x => x.DataVencimento.HasValue && x.DataVencimento >= dataInicio);
        }

        if (dataFim.HasValue)
        {
            queryEntradas = queryEntradas.Where(x => x.DataVencimento.HasValue && x.DataVencimento <= dataFim.Value.Date.AddDays(1).AddTicks(-1));
            querySaidasPagas = querySaidasPagas.Where(x => x.DataVencimento.HasValue && x.DataVencimento <= dataFim.Value.Date.AddDays(1).AddTicks(-1));
        }

        var totalEntradas = queryEntradas.Sum(x => x.Valor);
        var totalSaidasPagas = querySaidasPagas.Sum(x => x.Valor);

        return totalEntradas - totalSaidasPagas;
    }

    public List<Lancamento> Filtrar(string? pessoa, string? status, string? tipo,
                               DateTime? dataInicio, DateTime? dataFim)
    {
        using var context = new FinanceiroDbContext();

        var query = context.Lancamentos
            .Include(x => x.Categoria)
            .Include(x => x.Conta)
            .Include(x => x.Pessoa)
            .AsQueryable();

        // Filtros existentes
        if (!string.IsNullOrWhiteSpace(pessoa) && pessoa != "Todos")
            query = query.Where(x => x.Pessoa != null && x.Pessoa.Nome == pessoa);

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
            query = query.Where(x => x.Status == status);

        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
        {
            if (tipo == "Entrada")
                query = query.Where(x => x.Tipo == TipoLancamento.Entrada);
            else if (tipo == "Saída")
                query = query.Where(x => x.Tipo == TipoLancamento.Saida);
        }

        // ✅ NOVOS FILTROS DE DATA
        if (dataInicio.HasValue)
            query = query.Where(x => x.DataVencimento >= dataInicio.Value);

        if (dataFim.HasValue)
            query = query.Where(x => x.DataVencimento <= dataFim.Value.Date.AddDays(1).AddTicks(-1)); // Até 23:59:59

        return query
            .OrderBy(x => x.DataVencimento)
            .ThenBy(x => x.Descricao)
            .ToList();
    }


}