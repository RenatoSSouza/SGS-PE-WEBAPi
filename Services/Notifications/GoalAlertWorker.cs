using Microsoft.EntityFrameworkCore;
using sgspe.Data;

namespace sgspe.Services.Notifications;

/// <summary>
/// Verifica periodicamente as metas de todas as empresas ativas e gera as notificações
/// automáticas (intervalo em Notifications:IntervalMinutes, padrão 360).
/// </summary>
public class GoalAlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<GoalAlertWorker> _logger;
    private readonly TimeSpan _interval;

    public GoalAlertWorker(IServiceScopeFactory scopes, ILogger<GoalAlertWorker> logger, IConfiguration config)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(Math.Max(1, config.GetValue("Notifications:IntervalMinutes", 360)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequena espera para a aplicação terminar de subir (migrations/seed).
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao gerar notificações de metas");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var now = DateTime.UtcNow;

        var companyIds = await db.Companies.AsNoTracking().Where(c => c.IsActive).Select(c => c.Id).ToListAsync(ct);
        var total = 0;
        foreach (var id in companyIds)
            total += await service.GenerateGoalNotificationsAsync(id, now.Year, now.Month);
        if (total > 0) _logger.LogInformation("{Count} notificações de metas geradas", total);
    }
}
