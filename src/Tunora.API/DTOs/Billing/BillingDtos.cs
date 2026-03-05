namespace Tunora.API.DTOs.Billing;

public record SubscriptionStatusDto(
    string Tier,
    string Status,
    bool IsTrialing,
    DateTime? PeriodEnd,
    int MaxInstances,
    int MaxChannels,
    bool CanSchedule);

public record CreateCheckoutDto(
    string Tier,       // "Starter" | "Professional" | "Business"
    string SuccessUrl,
    string CancelUrl);

public record CreatePortalDto(string ReturnUrl);

public record CheckoutSessionDto(string Url);
public record BillingPortalDto(string Url);
