using FinanceiroPessoal.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.Core.Repositories
{
    public interface ILancamentoRepository
    {
        Task<List<Lancamento>> ObterTodos();
        Task<Lancamento?> ObterPorId(int id);
        Task Adicionar(Lancamento lancamento);
        Task Atualizar(Lancamento lancamento);
        Task Excluir(int id);
        Task MarcarComoPago(int id, DateTime? dataPagamento = null);
        Task<List<Lancamento>> Filtrar(string pessoa, string status, string tipo, DateTime dataIni, DateTime dataFim);
        Task<decimal> CalcularSaldoConta(string pessoa, string status, string tipo, DateTime? dataIni, DateTime? dataFim);
        Task<decimal> ObterTotalPendenteSaidas();
        Task<decimal> ObterTotalPagoSaidas();

        Task<List<Lancamento>> ObterLancamentosPorPeriodoAsync(DateTime dataIni, DateTime dataFim);
        Task<List<Lancamento>> ObterVencimentosSemanaAsync(DateTime dataIni, DateTime dataFim);
        Task<List<Lancamento>> ObterAtrasadosAsync();
        Task<List<Lancamento>> ObterVencemHojeAsync(DateTime dataReferencia);
        Task<List<ProximoVencimentoDto>> ObterProximosVencimentosAsync(int quantidade);
        Task<List<Lancamento>> ObterPagosPorPeriodoAsync(DateTime inicio, DateTime fim);
    }
}
