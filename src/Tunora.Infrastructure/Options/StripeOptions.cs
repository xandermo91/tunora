namespace Tunora.Infrastructure.Options;

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string StarterPriceId { get; set; } = string.Empty;
    public string ProfessionalPriceId { get; set; } = string.Empty;
    public string BusinessPriceId { get; set; } = string.Empty;
}
