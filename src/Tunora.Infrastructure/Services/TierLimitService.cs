using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Enums;
using Tunora.Core.Exceptions;
using Tunora.Infrastructure.Data;

namespace Tunora.Infrastructure.Services;

public class TierLimitService(ApplicationDbContext db) : ITierLimitService
{
    private record TierLimits(int MaxInstances, int MaxChannels, bool CanSchedule);

    private static readonly Dictionary<SubscriptionTier, TierLimits> Limits = new()
    {
        [SubscriptionTier.Starter]      = new(1,          3, false),
        [SubscriptionTier.Professional] = new(5,          5, true),
        [SubscriptionTier.Business]     = new(20,         5, true),
        [SubscriptionTier.Enterprise]   = new(int.MaxValue, 5, true),
    };

    public async Task EnforceInstanceLimitAsync(int companyId, CancellationToken ct = default)
    {
        var company = await db.Companies
            .Include(c => c.Instances.Where(i => i.IsActive))
            .FirstOrDefaultAsync(c => c.Id == companyId, ct)
            ?? throw new NotFoundException("Company", companyId);

        var limits = Limits[company.SubscriptionTier];
        if (company.Instances.Count >= limits.MaxInstances)
            throw new TierLimitExceededException(
                $"Your {company.SubscriptionTier} plan allows up to {limits.MaxInstances} location(s). Please upgrade to add more.");
    }

    public async Task EnforceChannelLimitAsync(int companyId, int instanceId, CancellationToken ct = default)
    {
        var company = await db.Companies.FindAsync([companyId], ct)
            ?? throw new NotFoundException("Company", companyId);

        var limits = Limits[company.SubscriptionTier];
        var channelCount = await db.InstanceChannels.CountAsync(ic => ic.InstanceId == instanceId, ct);

        if (channelCount >= limits.MaxChannels)
            throw new TierLimitExceededException(
                $"Your {company.SubscriptionTier} plan allows up to {limits.MaxChannels} channel(s) per location.");
    }

    public async Task EnforceSchedulingAccessAsync(int companyId, CancellationToken ct = default)
    {
        var company = await db.Companies.FindAsync([companyId], ct)
            ?? throw new NotFoundException("Company", companyId);

        if (!Limits[company.SubscriptionTier].CanSchedule)
            throw new TierLimitExceededException(
                $"Scheduling is not available on the {company.SubscriptionTier} plan. Please upgrade to Professional or higher.");
    }
}
