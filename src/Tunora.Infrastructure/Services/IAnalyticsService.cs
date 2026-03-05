namespace Tunora.Infrastructure.Services;

public record OverviewStats(
    int TotalLocations,
    int ActiveLocations,
    int PlayingNow,
    int SchedulesThisWeek
);

public interface IAnalyticsService
{
    Task<OverviewStats> GetOverviewAsync(int companyId, CancellationToken ct = default);
}
