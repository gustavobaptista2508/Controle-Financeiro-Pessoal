using FinanceiroPessoal.WinForms.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Repositories
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
    }
}
