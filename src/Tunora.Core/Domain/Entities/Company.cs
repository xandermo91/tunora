using Tunora.Core.Domain.Enums;
using Tunora.Core.Domain.Interfaces;

namespace Tunora.Core.Domain.Entities;

public class Company : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Starter;
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trialing;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Instance> Instances { get; set; } = [];
}
