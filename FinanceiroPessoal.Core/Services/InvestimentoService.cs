using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Services;

public class InvestimentoService
{
    private readonly FinanceiroDbContext _context;

    public InvestimentoService(FinanceiroDbContext context)
    {
        _context = context;
    }

    public Task<List<Investimento>> ListarAsync(int usuarioId) =>
        _context.Investimentos
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.DataCompra)
            .ThenBy(x => x.Nome)
            .ToListAsync();

    public Task<Investimento?> ObterPorIdAsync(int id, int usuarioId) =>
        _context.Investimentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);

    public async Task CriarAsync(Investimento investimento)
    {
        Validar(investimento);
        investimento.DataCriacao = DateTime.Now;
        investimento.DataAtualizacao = null;

        _context.Investimentos.Add(investimento);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Investimento investimento, int usuarioId)
    {
        Validar(investimento);

        var existente = await _context.Investimentos
            .FirstOrDefaultAsync(x => x.Id == investimento.Id && x.UsuarioId == usuarioId);

        if (existente is null)
            throw new InvalidOperationException("Investimento não encontrado para o usuário informado.");

        existente.Nome = investimento.Nome;
        existente.Ticker = investimento.Ticker;
        existente.Tipo = investimento.Tipo;
        existente.ValorInvestido = investimento.ValorInvestido;
        existente.Quantidade = investimento.Quantidade;
        existente.ValorAtualUnitario = investimento.ValorAtualUnitario;
        existente.DataCompra = investimento.DataCompra;
        existente.Observacao = investimento.Observacao;
        existente.DataAtualizacao = DateTime.Now;

        await _context.SaveChangesAsync();
    }

    public async Task ExcluirAsync(int id, int usuarioId)
    {
        var existente = await _context.Investimentos
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);

        if (existente is null)
            return;

        _context.Investimentos.Remove(existente);
        await _context.SaveChangesAsync();
    }

    private static void Validar(Investimento investimento)
    {
        if (investimento.Quantidade <= 0)
            throw new ArgumentException("A quantidade deve ser maior que zero.");

        if (investimento.ValorInvestido < 0)
            throw new ArgumentException("O valor investido não pode ser negativo.");

        if (investimento.ValorAtualUnitario < 0)
            throw new ArgumentException("O valor atual unitário não pode ser negativo.");
    }
}
