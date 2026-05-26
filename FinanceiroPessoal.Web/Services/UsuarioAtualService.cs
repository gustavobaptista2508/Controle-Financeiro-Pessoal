using System.Security.Claims;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class UsuarioAtualService(AuthenticationStateProvider authStateProvider, FinanceiroDbContext db) : IUsuarioAtualService
{
    public async Task<int?> ObterUsuarioIdAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
            return null;

        var claimId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("nameidentifier")?.Value
                      ?? user.FindFirst("sub")?.Value
                      ?? user.FindFirst("id")?.Value
                      ?? user.FindFirst("usuario_id")?.Value;

        return int.TryParse(claimId, out var usuarioId) ? usuarioId : null;
    }

    public async Task<Usuario?> ObterUsuarioAsync()
    {
        var usuarioId = await ObterUsuarioIdAsync();
        if (!usuarioId.HasValue)
            return null;

        return await db.Usuarios
            .Include(x => x.Plano)
            .FirstOrDefaultAsync(x => x.Id == usuarioId.Value);
    }
}
