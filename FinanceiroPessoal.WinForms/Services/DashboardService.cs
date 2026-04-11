using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Services
{
    public class DashboardService
    {
        public DashboardResumo ObterResumo(DateTime referencia)
        {
            using var context = new FinanceiroDbContext();

            var inicioMes = new DateTime(referencia.Year, referencia.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);
            var hoje = DateTime.Today;
            var fimSemana = hoje.AddDays(7);

            var lancamentosMes = context.Lancamentos
                .Where(x => x.DataVencimento.HasValue &&
                            x.DataVencimento.Value.Date >= inicioMes.Date &&
                            x.DataVencimento.Value.Date <= fimMes.Date)
                .ToList();

            var entradasMes = lancamentosMes
                .Where(x => x.Tipo == TipoLancamento.Entrada)
                .ToList();

            var saidasMes = lancamentosMes
                .Where(x => x.Tipo == TipoLancamento.Saida)
                .ToList();

            var pendentes = saidasMes
                .Where(x => x.Status == "Pendente")
                .ToList();

            var pagos = saidasMes
                .Where(x => x.Status == "Pago")
                .ToList();

            var vencemSemana = context.Lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.DataVencimento.HasValue &&
                            x.DataVencimento.Value.Date >= hoje &&
                            x.DataVencimento.Value.Date <= fimSemana &&
                            x.Status != "Pago")
                .ToList();

            var atrasados = context.Lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.Status == "Atrasado")
                .ToList();

            var vencemHoje = context.Lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.DataVencimento.HasValue &&
                            x.DataVencimento.Value.Date == hoje &&
                            x.Status != "Pago")
                .ToList();

            var totalEntradas = entradasMes.Sum(x => x.Valor);
            var totalSaidas = saidasMes.Sum(x => x.Valor);

            return new DashboardResumo
            {
                TotalEntradas = totalEntradas,
                QuantidadeEntradas = entradasMes.Count,

                TotalSaidas = totalSaidas,
                QuantidadeSaidas = saidasMes.Count,

                SaldoMes = totalEntradas - totalSaidas,

                TotalPendente = pendentes.Sum(x => x.Valor),
                QuantidadePendentes = pendentes.Count,

                TotalPago = pagos.Sum(x => x.Valor),
                QuantidadePagos = pagos.Count,

                TotalSemana = vencemSemana.Sum(x => x.Valor),
                QuantidadeSemana = vencemSemana.Count,

                TotalLancamentosMes = lancamentosMes.Count,
                TotalAtrasados = atrasados.Sum(x => x.Valor),
                TotalVencemHoje = vencemHoje.Sum(x => x.Valor)
            };
        }

        public List<ProximoVencimentoDto> ObterProximosVencimentos(int quantidade = 8)
        {
            using var context = new FinanceiroDbContext();

            return context.Lancamentos
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .Where(x => x.DataVencimento.HasValue &&
                            x.Tipo == TipoLancamento.Saida &&
                            x.Status != "Pago")
                .OrderBy(x => x.DataVencimento)
                .Take(quantidade)
                .Select(x => new ProximoVencimentoDto
                {
                    Id = x.Id,
                    Vencimento = x.DataVencimento,
                    Descricao = x.Descricao,
                    Valor = x.Valor,
                    Conta = x.Conta != null ? x.Conta.Nome : "",
                    Pessoa = x.Pessoa != null ? x.Pessoa.Nome : "",
                    Status = x.Status,
                    Tipo = x.Tipo == TipoLancamento.Entrada ? "Entrada" : "Saída"
                })
                .ToList();
        }

        public List<GastoCategoriaDto> ObterGastosPorCategoria(DateTime referencia)
        {
            using var context = new FinanceiroDbContext();

            var inicioMes = new DateTime(referencia.Year, referencia.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            return context.Lancamentos
                .Include(x => x.Categoria)
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.DataVencimento.HasValue &&
                            x.DataVencimento.Value.Date >= inicioMes.Date &&
                            x.DataVencimento.Value.Date <= fimMes.Date)
                .AsEnumerable()
                .GroupBy(x => x.Categoria != null ? x.Categoria.Nome : "Sem Categoria")
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
