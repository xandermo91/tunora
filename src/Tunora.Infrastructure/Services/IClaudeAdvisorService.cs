namespace Tunora.Infrastructure.Services;

public record AdvisorResponse(string Insight, string[] Suggestions);

public interface IClaudeAdvisorService
{
    Task<AdvisorResponse> GetMusicAdviceAsync(string businessType, string? description, CancellationToken ct = default);
    Task<AdvisorResponse> GetAnalyticsInsightAsync(OverviewStats stats, CancellationToken ct = default);
}
