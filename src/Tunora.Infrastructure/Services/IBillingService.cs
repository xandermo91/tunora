using Tunora.Core.Domain.Enums;

namespace Tunora.Infrastructure.Services;

public record BillingStatus(
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime? PeriodEnd);

public interface IBillingService
{
    Task<BillingStatus> GetStatusAsync(int companyId, CancellationToken ct = default);
    Task<string> CreateCheckoutSessionAsync(int companyId, string tier, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string> CreatePortalSessionAsync(int companyId, string returnUrl, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
