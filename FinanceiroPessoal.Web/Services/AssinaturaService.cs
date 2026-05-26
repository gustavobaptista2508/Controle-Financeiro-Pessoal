using FinanceiroPessoal.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class AssinaturaService(FinanceiroDbContext db) : IAssinaturaService
{
    public async Task<bool> UsuarioTemAcessoAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.Include(x => x.Plano).IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId);
        if (usuario is null || !usuario.Ativo)
            return false;

        var status = (usuario.AssinaturaStatus ?? string.Empty).Trim().ToUpperInvariant();
        if (status is not ("TRIAL" or "ATIVA" or "ACTIVE"))
            return false;

        var now = DateTime.UtcNow;

        if (usuario.TrialExpiraEm.HasValue && usuario.TrialExpiraEm.Value.ToUniversalTime() < now)
            return false;

        if (usuario.AssinaturaExpiraEm.HasValue && usuario.AssinaturaExpiraEm.Value.ToUniversalTime() < now)
            return false;

        return true;
    }

    public async Task<bool> UsuarioPossuiPlusAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.Include(x => x.Plano).IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId);
        if (usuario is null || !usuario.Ativo)
            return false;

        if (!await UsuarioTemAcessoAsync(usuarioId))
            return false;

        return (usuario.Plano?.Nome?.Contains("PLUS", StringComparison.OrdinalIgnoreCase) ?? false)
               || (usuario.PlanoId.HasValue && await db.Planos.AnyAsync(p => p.Id == usuario.PlanoId.Value && p.Nome.Contains("PLUS")));
    }
}
