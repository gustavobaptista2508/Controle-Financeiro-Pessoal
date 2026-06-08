using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Services;

public class ObjetivoFinanceiroService
{
    private const int LimitePlanoGratuito = 1;
    private readonly FinanceiroDbContext _context;

    public ObjetivoFinanceiroService(FinanceiroDbContext context)
    {
        _context = context;
    }

    public Task<List<ObjetivoFinanceiro>> ListarAsync(int usuarioId) =>
        _context.ObjetivosFinanceiros
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Ativo)
            .OrderBy(x => x.DataMeta ?? DateTime.MaxValue)
            .ThenBy(x => x.Nome)
            .ToListAsync();

    public async Task CriarAsync(int usuarioId, ObjetivoFinanceiro model)
    {
        await ValidarLimitePlanoGratuitoAsync(usuarioId);
        Validar(model);

        model.Id = 0;
        model.UsuarioId = usuarioId;
        model.Ativo = true;
        model.DataCriacao = DateTime.Now;
        model.DataAtualizacao = null;
        Normalizar(model);

        _context.ObjetivosFinanceiros.Add(model);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(int usuarioId, ObjetivoFinanceiro model)
    {
        Validar(model);

        var existente = await _context.ObjetivosFinanceiros
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == model.Id && x.UsuarioId == usuarioId && x.Ativo);

        if (existente is null)
            throw new InvalidOperationException("Objetivo financeiro não encontrado para o usuário informado.");

        existente.Nome = model.Nome;
        existente.ValorAlvo = model.ValorAlvo;
        existente.ValorAtual = model.ValorAtual;
        existente.DataMeta = model.DataMeta;
        existente.Cor = model.Cor;
        existente.Icone = model.Icone;
        existente.DataAtualizacao = DateTime.Now;
        Normalizar(existente);

        await _context.SaveChangesAsync();
    }

    public async Task InativarAsync(int usuarioId, int id)
    {
        var existente = await _context.ObjetivosFinanceiros
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId && x.Ativo);

        if (existente is null)
            return;

        existente.Ativo = false;
        existente.DataAtualizacao = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private async Task ValidarLimitePlanoGratuitoAsync(int usuarioId)
    {
        if (!await UsuarioEstaNoPlanoGratuitoAsync(usuarioId))
            return;

        var totalAtivos = await _context.ObjetivosFinanceiros
            .IgnoreQueryFilters()
            .CountAsync(x => x.UsuarioId == usuarioId && x.Ativo);

        if (totalAtivos >= LimitePlanoGratuito)
            throw new InvalidOperationException("O plano gratuito permite cadastrar apenas 1 objetivo financeiro. Faça upgrade para o Essencial ou Plus para objetivos ilimitados.");
    }

    private async Task<bool> UsuarioEstaNoPlanoGratuitoAsync(int usuarioId)
    {
        var usuario = await _context.Usuarios
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == usuarioId);

        if (usuario?.PlanoId is null)
            return true;

        var nomePlano = await _context.Planos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == usuario.PlanoId.Value)
            .Select(x => x.Nome)
            .FirstOrDefaultAsync() ?? string.Empty;
        return nomePlano.Contains("gratuito", StringComparison.OrdinalIgnoreCase)
            || nomePlano.Contains("free", StringComparison.OrdinalIgnoreCase);
    }

    private static void Validar(ObjetivoFinanceiro objetivo)
    {
        if (string.IsNullOrWhiteSpace(objetivo.Nome))
            throw new ArgumentException("Informe o nome do objetivo financeiro.");

        if (objetivo.ValorAlvo <= 0)
            throw new ArgumentException("O valor alvo deve ser maior que zero.");

        if (objetivo.ValorAtual < 0)
            throw new ArgumentException("O valor atual não pode ser negativo.");
    }

    private static void Normalizar(ObjetivoFinanceiro objetivo)
    {
        objetivo.Nome = objetivo.Nome.Trim();
        objetivo.Cor = string.IsNullOrWhiteSpace(objetivo.Cor) ? "#2563eb" : objetivo.Cor.Trim();
        objetivo.Icone = string.IsNullOrWhiteSpace(objetivo.Icone) ? "savings" : objetivo.Icone.Trim();
    }
}
