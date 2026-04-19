using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using FinanceiroPessoal.WinForms.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public class DashboardService
    {
        private readonly ILancamentoRepository _repository;

        public DashboardService(ILancamentoRepository repository)
        {
            _repository = repository;
        }
        public async Task<DashboardResumo> ObterResumo(DateTime referencia)
        {
            var inicioMes = new DateTime(referencia.Year, referencia.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);
            var hoje = DateTime.Today;
            var fimSemana = hoje.AddDays(7);

            // ✅ Usa repository - Funciona com qualquer banco
            var lancamentosMes = await _repository.ObterLancamentosPorPeriodoAsync(inicioMes, fimMes);

            var entradasMes = lancamentosMes.Where(x => x.Tipo == TipoLancamento.Entrada).ToList();
            var saidasMes = lancamentosMes.Where(x => x.Tipo == TipoLancamento.Saida).ToList();
            var saidasPagasMes = saidasMes.Where(x => x.Status == "Pago").ToList();
            var pendentesMes = saidasMes.Where(x => x.Status == "Pendente").ToList();
            var pagosMes = saidasMes.Where(x => x.Status == "Pago").ToList();

            var vencemSemana = await _repository.ObterVencimentosSemanaAsync(hoje, fimSemana);
            var atrasados = await _repository.ObterAtrasadosAsync();
            var vencemHoje = await _repository.ObterVencemHojeAsync(hoje);

            var totalEntradas = entradasMes.Sum(x => x.Valor);
            var totalSaidasPagas = saidasPagasMes.Sum(x => x.Valor);

            return new DashboardResumo
            {
                TotalEntradas = totalEntradas,
                QuantidadeEntradas = entradasMes.Count,
                TotalSaidas = totalSaidasPagas,
                QuantidadeSaidas = saidasPagasMes.Count,
                SaldoMes = totalEntradas - totalSaidasPagas,
                TotalPendente = pendentesMes.Sum(x => x.Valor),
                QuantidadePendentes = pendentesMes.Count,
                TotalPago = pagosMes.Sum(x => x.Valor),
                QuantidadePagos = pagosMes.Count,
                TotalSemana = vencemSemana.Sum(x => x.Valor),
                QuantidadeSemana = vencemSemana.Count,
                TotalLancamentosMes = lancamentosMes.Count,
                TotalAtrasados = atrasados.Sum(x => x.Valor),
                TotalVencemHoje = vencemHoje.Sum(x => x.Valor)
            };
        }

        public async Task<List<ProximoVencimentoDto>> ObterProximosVencimentos(int quantidade = 8)
        {
            return await _repository.ObterProximosVencimentosAsync(quantidade);
        }

        public async Task<List<GastoCategoriaDto>> ObterGastosPorCategoria(DateTime referencia)
        {
            var inicioMes = new DateTime(referencia.Year, referencia.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            var lancamentos = await _repository.ObterLancamentosPorPeriodoAsync(inicioMes, fimMes);

            return lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida)
                .GroupBy(x => x.Categoria?.Nome ?? "Sem Categoria")
                .Select(g => new GastoCategoriaDto
                {
                    Categoria = g.Key,
                    Valor = g.Sum(x => x.Valor)
                })
                .OrderByDescending(x => x.Valor)
                .ToList();
        }
    }
}
