using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Data;


namespace Tunora.Infrastructure.Services;

public class AnalyticsService(ApplicationDbContext db) : IAnalyticsService
{
    public async Task<OverviewStats> GetOverviewAsync(int companyId, CancellationToken ct = default)
    {
        var instances = await db.Instances
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.IsActive)
            .Select(i => i.Status)
            .ToListAsync(ct);

        // DaysOfWeek is stored as JSON — filter in memory after load
        var activeSchedules = await db.Schedules
            .AsNoTracking()
            .Where(s => s.Instance.CompanyId == companyId && s.IsActive)
            .ToListAsync(ct);

        // Count schedules that run at least one day (any day falls within a 7-day week)
        var schedulesThisWeek = activeSchedules.Count(s => s.DaysOfWeek.Length > 0);

        return new OverviewStats(
            TotalLocations:    instances.Count,
            ActiveLocations:   instances.Count(s => s != InstanceStatus.Offline),
            PlayingNow:        instances.Count(s => s == InstanceStatus.Playing),
            SchedulesThisWeek: schedulesThisWeek
        );
    }
}
