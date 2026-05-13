using FinanceiroPessoal.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class AssinaturaService(FinanceiroDbContext db) : IAssinaturaService
{
    public async Task<bool> UsuarioTemAcessoAsync(int usuarioId)
    {
        var u = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId);
        if (u is null) return false;
        var now = DateTime.Now;
        return (u.AssinaturaStatus == "ATIVA" && (!u.AssinaturaExpiraEm.HasValue || u.AssinaturaExpiraEm.Value >= now))
               || (u.AssinaturaStatus == "TRIAL" && u.TrialExpiraEm.HasValue && u.TrialExpiraEm.Value >= now);
    }
}
