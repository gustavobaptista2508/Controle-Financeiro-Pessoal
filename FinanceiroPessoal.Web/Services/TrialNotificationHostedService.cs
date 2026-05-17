namespace FinanceiroPessoal.Web.Services;

public class TrialNotificationHostedService(IServiceProvider serviceProvider, IWebHostEnvironment env, ILogger<TrialNotificationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = env.IsDevelopment() ? TimeSpan.FromMinutes(15) : TimeSpan.FromDays(1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var trialService = scope.ServiceProvider.GetRequiredService<TrialNotificationService>();
                await trialService.ExecutarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro na execução do TrialNotificationHostedService.");
            }

            await Task.Delay(intervalo, stoppingToken);
        }
    }
}
