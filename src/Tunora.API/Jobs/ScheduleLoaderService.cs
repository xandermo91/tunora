using Quartz;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Jobs;

/// <summary>Reloads all active DB schedules into Quartz when the application starts.</summary>
public class ScheduleLoaderService(
    IServiceScopeFactory scopeFactory,
    ISchedulerFactory schedulerFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        // ISchedulerFactory.GetScheduler() is safe to call after QuartzHostedService.StartAsync
        _ = await schedulerFactory.GetScheduler(ct); // ensure scheduler is available

        using var scope   = scopeFactory.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var active = await scheduleService.GetAllActiveAsync(ct);
        foreach (var schedule in active)
            await scheduleService.RegisterWithQuartzAsync(schedule, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
