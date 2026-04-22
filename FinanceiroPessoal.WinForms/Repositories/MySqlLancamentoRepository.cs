using FinanceiroPessoal.WinForms.Data;
using FinanceiroPessoal.WinForms.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinanceiroPessoal.WinForms.Repositories
{
    public class MySqlLancamentoRepository : ILancamentoRepository
    {
        private readonly MySqlDbContext _context;

        // ✅ CONSTRUTOR SEM PARÂMETRO - AUTO-CRIA
        public MySqlLancamentoRepository()
        {
            _context = new MySqlDbContext();
        }

        public async Task<List<Lancamento>> ObterTodos()
        {
            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        public async Task<Lancamento?> ObterPorId(int id)
        {
            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task Adicionar(Lancamento lancamento)
        {
            _context.Lancamentos.Add(lancamento);
            await _context.SaveChangesAsync();
        }

        public async Task Atualizar(Lancamento lancamento)
        {
            _context.Lancamentos.Update(lancamento);
            await _context.SaveChangesAsync();
        }

        public async Task Excluir(int id)
        {
            var lancamento = await _context.Lancamentos.FirstOrDefaultAsync(x => x.Id == id);
            if (lancamento != null)
            {
                _context.Lancamentos.Remove(lancamento);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarcarComoPago(int id, DateTime? dataPagamento = null)
        {
            var lancamento = await _context.Lancamentos.FirstOrDefaultAsync(x => x.Id == id);
            if (lancamento != null)
            {
                lancamento.Status = "Pago";
                lancamento.DataPagamento = dataPagamento ?? DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Lancamento>> Filtrar(string pessoa, string status, string tipo, DateTime dataIni, DateTime dataFim)
        {
            var query = _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .AsQueryable();

            // Filtros
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

            // ✅ FILTROS DE DATA CORRETOS
            query = query.Where(x => x.DataVencimento >= dataIni);
            query = query.Where(x => x.DataVencimento <= dataFim.Date.AddDays(1).AddTicks(-1));

            return await query
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        async Task<decimal> ILancamentoRepository.CalcularSaldoConta(string pessoa, string status, string tipo, DateTime? dataIni, DateTime? dataFim)
        {
            var queryEntradas = _context.Lancamentos
        .Where(x => x.Tipo == TipoLancamento.Entrada &&
                    x.Status == "Pago" &&
                    x.DataPagamento.HasValue);

            var querySaidasPagas = _context.Lancamentos
        .Where(x => x.Tipo == TipoLancamento.Saida &&
                    x.Status == "Pago" &&
                    x.DataPagamento.HasValue);

            // Aplica filtros
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

            if (dataIni.HasValue)
            {
                queryEntradas = queryEntradas.Where(x => x.DataPagamento!.Value >= dataIni.Value);
                querySaidasPagas = querySaidasPagas.Where(x => x.DataPagamento!.Value >= dataIni.Value);
            }

            if (dataFim.HasValue)
            {
                var fim = dataFim.Value.Date.AddDays(1).AddTicks(-1);
                queryEntradas = queryEntradas.Where(x => x.DataPagamento!.Value <= fim);
                querySaidasPagas = querySaidasPagas.Where(x => x.DataPagamento!.Value <= fim);
            }

            var totalEntradas = await queryEntradas.SumAsync(x => x.Valor);
            var totalSaidasPagas = await querySaidasPagas.SumAsync(x => x.Valor);

            return totalEntradas - totalSaidasPagas;
        }

        public async Task<decimal> ObterTotalPendenteSaidas()
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            return await _context.Lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.Status == "Pendente" &&
                            x.DataVencimento.HasValue &&
                            x.DataVencimento.Value.Date >= inicioMes &&
                            x.DataVencimento.Value.Date <= fimMes)
                .SumAsync(x => x.Valor);
        }

        public async Task<decimal> ObterTotalPagoSaidas()
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var fimMes = inicioMes.AddMonths(1).AddDays(-1);

            return await _context.Lancamentos
                .Where(x => x.Tipo == TipoLancamento.Saida &&
                            x.Status == "Pago" &&
                            x.DataPagamento.HasValue &&
                            x.DataPagamento.Value.Date >= inicioMes &&
                            x.DataPagamento.Value.Date <= fimMes)
                .SumAsync(x => x.Valor);
        }

        public async Task<List<Lancamento>> ObterLancamentosPorPeriodoAsync(DateTime dataIni, DateTime dataFim)
        {
            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .Where(x => x.DataVencimento >= dataIni && x.DataVencimento <= dataFim.Date.AddDays(1).AddTicks(-1))
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        public async Task<List<Lancamento>> ObterVencimentosSemanaAsync(DateTime dataIni, DateTime dataFim)
        {
            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .Where(x => x.DataVencimento >= dataIni && x.DataVencimento <= dataFim.Date.AddDays(1).AddTicks(-1))
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        public async Task<List<Lancamento>> ObterAtrasadosAsync()
        {
            var hoje = DateTime.Today;

            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .Where(x => x.DataVencimento < hoje && x.Status != "Pago")
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        public async Task<List<Lancamento>> ObterVencemHojeAsync(DateTime dataReferencia)
        {
            return await _context.Lancamentos
                .Include(x => x.Categoria)
                .Include(x => x.Conta)
                .Include(x => x.Pessoa)
                .Where(x => x.DataVencimento.HasValue && x.DataVencimento.Value.Date == dataReferencia.Date)
                .OrderBy(x => x.DataVencimento)
                .ThenBy(x => x.Descricao)
                .ToListAsync();
        }

        public async Task<List<ProximoVencimentoDto>> ObterProximosVencimentosAsync(int quantidade)
        {
            return await _context.Lancamentos
       .Include(x => x.Conta).Include(x => x.Pessoa)
       .Where(x => x.DataVencimento.HasValue && x.Tipo == TipoLancamento.Saida && x.Status != "Pago")
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
       }).ToListAsync();
        }

        public async Task<List<Lancamento>> ObterPagosPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            return await _context.Lancamentos
        .Include(x => x.Conta)
        .Include(x => x.Categoria)
        .Include(x => x.Pessoa)
        .Where(x => x.Status == "Pago" &&
                    x.DataPagamento.HasValue &&
                    x.DataPagamento.Value.Date >= inicio.Date &&
                    x.DataPagamento.Value.Date <= fim.Date)
        .AsNoTracking()
        .ToListAsync();
        }
    }
}
