namespace Tunora.Core.Domain.Entities;

public class StripeEvent
{
    public int Id { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
