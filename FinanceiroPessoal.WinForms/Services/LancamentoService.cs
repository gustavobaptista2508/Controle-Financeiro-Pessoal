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

    public decimal ObterTotalPendente()
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Where(x => x.Status == "Pendente")
            .Select(x => x.Valor)
            .DefaultIfEmpty(0)
            .Sum();
    }

    public decimal ObterTotalPago()
    {
        using var context = new FinanceiroDbContext();

        return context.Lancamentos
            .Where(x => x.Status == "Pago")
            .Select(x => x.Valor)
            .DefaultIfEmpty(0)
            .Sum();
    }

    public List<Lancamento> Filtrar(string? pessoa, string? status)
    {
        using var context = new FinanceiroDbContext();

        var query = context.Lancamentos
            .Include(x => x.Categoria)
            .Include(x => x.Conta)
            .Include(x => x.Pessoa)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pessoa) && pessoa != "Todos")
            query = query.Where(x => x.Pessoa != null && x.Pessoa.Nome == pessoa);

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
            query = query.Where(x => x.Status == status);

        return query
            .OrderBy(x => x.DataVencimento)
            .ThenBy(x => x.Descricao)
            .ToList();
    }
}