namespace Tunora.Infrastructure.Services;

public interface ITierLimitService
{
    Task EnforceInstanceLimitAsync(int companyId, CancellationToken ct = default);
    Task EnforceChannelLimitAsync(int companyId, int instanceId, CancellationToken ct = default);
    Task EnforceSchedulingAccessAsync(int companyId, CancellationToken ct = default);
}
