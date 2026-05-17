using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Core.Services;

public class UsuarioPadraoService
{
    private readonly FinanceiroDbContext _db;

    public UsuarioPadraoService(FinanceiroDbContext db)
    {
        _db = db;
    }

    public async Task CriarEstruturaPadraoAsync(int usuarioId)
    {
        await CriarContaPrincipalAsync(usuarioId);
        await CriarCategoriasPadraoAsync(usuarioId);
        await _db.SaveChangesAsync();
    }

    private async Task CriarContaPrincipalAsync(int usuarioId)
    {
        var existeConta = await _db.Contas
            .IgnoreQueryFilters()
            .AnyAsync(c => c.UsuarioId == usuarioId && c.Nome == "Conta Principal");

        if (!existeConta)
        {
            _db.Contas.Add(new Conta
            {
                UsuarioId = usuarioId,
                Nome = "Conta Principal",
                Tipo = "Conta Corrente"
            });
        }
    }

    private async Task CriarCategoriasPadraoAsync(int usuarioId)
    {
        var existeCategoria = await _db.Categorias
            .IgnoreQueryFilters()
            .AnyAsync(c => c.UsuarioId == usuarioId);

        if (existeCategoria)
            return;

        var categorias = new List<Categoria>
        {
            new() { UsuarioId = usuarioId, Nome = "Salário" },
            new() { UsuarioId = usuarioId, Nome = "Receitas Extras" },
            new() { UsuarioId = usuarioId, Nome = "Investimentos" },
            new() { UsuarioId = usuarioId, Nome = "Alimentação" },
            new() { UsuarioId = usuarioId, Nome = "Transporte" },
            new() { UsuarioId = usuarioId, Nome = "Moradia" },
            new() { UsuarioId = usuarioId, Nome = "Saúde" },
            new() { UsuarioId = usuarioId, Nome = "Educação" },
            new() { UsuarioId = usuarioId, Nome = "Lazer" },
            new() { UsuarioId = usuarioId, Nome = "Compras" },
            new() { UsuarioId = usuarioId, Nome = "Serviços" },
            new() { UsuarioId = usuarioId, Nome = "Impostos e Taxas" },
            new() { UsuarioId = usuarioId, Nome = "Outros" }
        };

        _db.Categorias.AddRange(categorias);
    }
}
