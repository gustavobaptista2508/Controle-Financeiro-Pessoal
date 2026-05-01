using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using FinanceiroPessoal.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Services;

public class LancamentoService
{
    private readonly ILancamentoRepository _repository;

    public LancamentoService(ILancamentoRepository repository)
    {
        _repository = repository;
    }
    public async Task<List<Lancamento>> ObterTodos()
    {
        return await _repository.ObterTodos();
    }

    public async Task<Lancamento?> ObterPorId(int id)
    {
        return await _repository.ObterPorId(id);
    }

    public async Task Adicionar(Lancamento lancamento)
    {
        await _repository.Adicionar(lancamento);
    }

    public async Task Atualizar(Lancamento lancamento)
    {
        await _repository.Atualizar(lancamento);
    }

    public async Task Excluir(int id)
    {
        await _repository.Excluir(id);
    }

    public async Task MarcarComoPago(int id, DateTime? dataPagamento = null)
    {
        await _repository.MarcarComoPago(id, dataPagamento);
    }

    public async Task<decimal> ObterTotalPendenteSaidas()
    {
        return await _repository.ObterTotalPendenteSaidas();
    }

    public async Task<decimal> ObterTotalPagoSaidas()
    {
        return await _repository.ObterTotalPagoSaidas();
    }

    public async Task<decimal> CalcularSaldoConta(string? pessoa = null, string? status = null, string? tipo = null,
                                 DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        return await _repository.CalcularSaldoConta(pessoa, status, tipo, dataInicio, dataFim);
    }

    public async Task<List<Lancamento>> Filtrar(string? pessoa, string? status, string? tipo,
                               DateTime? dataInicio, DateTime? dataFim)
    {
        var dataStart = dataInicio ?? DateTime.Now.AddMonths(-1);
        var dateEnd = dataFim ?? DateTime.Now;
        return await _repository.Filtrar(pessoa ?? "", status ?? "", tipo ?? "", dataStart, dateEnd);
    }


}