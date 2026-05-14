using FinanceiroPessoal.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class AssinaturaService(FinanceiroDbContext db) : IAssinaturaService
{
    public async Task<bool> UsuarioTemAcessoAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == usuarioId);
        if (usuario is null)
        {
            return false;
        }

        Console.WriteLine($"ASSINATURA: usuario {usuarioId}, status {usuario.AssinaturaStatus}");

        var now = DateTime.Now;
        if (usuario.AssinaturaStatus == "ATIVA")
        {
            return !usuario.AssinaturaExpiraEm.HasValue || usuario.AssinaturaExpiraEm.Value >= now;
        }

        if (usuario.AssinaturaStatus == "TRIAL")
        {
            if (usuario.TrialExpiraEm.HasValue && usuario.TrialExpiraEm.Value >= now)
            {
                return true;
            }

            usuario.AssinaturaStatus = "EXPIRADA";
            usuario.DataAtualizacao = now;
            await db.SaveChangesAsync();
            return false;
        }

        return false;
    }
}
