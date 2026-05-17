using System.Data;
using FinanceiroPessoal.Core.Data;
using FinanceiroPessoal.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceiroPessoal.Web.Services;

public class TrialNotificationService(FinanceiroDbContext db, IEmailService emailService, ILogger<TrialNotificationService> logger)
{
    public async Task ExecutarAsync(CancellationToken cancellationToken = default)
    {
        await GarantirColunasControleAsync(cancellationToken);

        var usuariosTrial = await db.Usuarios.IgnoreQueryFilters()
            .Where(x => x.AssinaturaStatus == "TRIAL" && x.Ativo)
            .ToListAsync(cancellationToken);

        foreach (var usuario in usuariosTrial)
        {
            if (!usuario.TrialExpiraEm.HasValue) continue;

            var diasRestantes = (usuario.TrialExpiraEm.Value.Date - DateTime.Now.Date).Days;

            if (diasRestantes == 3)
            {
                var lembreteEnviado = await ObterDataControleAsync(usuario.Id, "trial_lembrete_enviado_em", cancellationToken);
                if (!lembreteEnviado.HasValue)
                {
                    await TentarEnviarEmailAsync(() => emailService.EnviarLembreteTrialAsync(usuario, diasRestantes), usuario.Id, "trial_lembrete_enviado_em", cancellationToken);
                }
            }

            if (usuario.TrialExpiraEm.Value < DateTime.Now)
            {
                var encerradoEnviado = await ObterDataControleAsync(usuario.Id, "trial_encerrado_email_enviado_em", cancellationToken);
                if (!encerradoEnviado.HasValue)
                {
                    await TentarEnviarEmailAsync(() => emailService.EnviarTrialEncerradoAsync(usuario), usuario.Id, "trial_encerrado_email_enviado_em", cancellationToken);
                }
            }
        }
    }

    private async Task TentarEnviarEmailAsync(Func<Task<bool>> envio, int usuarioId, string colunaControle, CancellationToken cancellationToken)
    {
        try
        {
            var enviado = await envio();
            if (enviado)
            {
                await AtualizarDataControleAsync(usuarioId, colunaControle, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar e-mail transacional de trial para usuário {UsuarioId}.", usuarioId);
        }
    }

    private async Task GarantirColunasControleAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS trial_lembrete_enviado_em datetime NULL;", cancellationToken);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS trial_encerrado_email_enviado_em datetime NULL;", cancellationToken);
    }

    private async Task<DateTime?> ObterDataControleAsync(int usuarioId, string coluna, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT {coluna} FROM usuarios WHERE id = @id LIMIT 1";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = usuarioId;
        cmd.Parameters.Add(param);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is DBNull or null ? null : Convert.ToDateTime(result);
    }

    private async Task AtualizarDataControleAsync(int usuarioId, string coluna, CancellationToken cancellationToken)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"UPDATE usuarios SET {coluna} = @agora WHERE id = @id";

        var pAgora = cmd.CreateParameter();
        pAgora.ParameterName = "@agora";
        pAgora.Value = DateTime.Now;
        cmd.Parameters.Add(pAgora);

        var pId = cmd.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = usuarioId;
        cmd.Parameters.Add(pId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
