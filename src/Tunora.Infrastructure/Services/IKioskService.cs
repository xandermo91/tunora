namespace Tunora.Infrastructure.Services;

public record KioskAuthResult(string AccessToken, int InstanceId, string InstanceName);

public interface IKioskService
{
    Task<KioskAuthResult?> AuthenticateAsync(string connectionKey, CancellationToken ct = default);
}
