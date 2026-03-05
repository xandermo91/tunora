using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Entities;
using Tunora.Core.Domain.Enums;
using Tunora.Core.Exceptions;
using Tunora.Infrastructure.Data;

namespace Tunora.Infrastructure.Services;

public class InstanceService(ApplicationDbContext db, ITierLimitService tierLimitService) : IInstanceService
{
    public async Task<List<Instance>> GetAllAsync(int companyId, CancellationToken ct = default) =>
        await db.Instances
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && i.IsActive)
            .Include(i => i.InstanceChannels)
                .ThenInclude(ic => ic.Channel)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<Instance?> GetByIdAsync(int id, int companyId, CancellationToken ct = default) =>
        await db.Instances
            .AsNoTracking()
            .Include(i => i.InstanceChannels)
                .ThenInclude(ic => ic.Channel)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId && i.IsActive, ct);

    public async Task<Instance> CreateAsync(string name, string location, int companyId, CancellationToken ct = default)
    {
        await tierLimitService.EnforceInstanceLimitAsync(companyId, ct);

        var instance = new Instance
        {
            CompanyId = companyId,
            Name = name,
            Location = location,
            ConnectionKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            Status = InstanceStatus.Offline,
        };
        db.Instances.Add(instance);
        await db.SaveChangesAsync(ct);
        return instance;
    }

    public async Task<Instance> UpdateAsync(int id, string name, string location, int companyId, CancellationToken ct = default)
    {
        var instance = await db.Instances
            .Include(i => i.InstanceChannels).ThenInclude(ic => ic.Channel)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId && i.IsActive, ct)
            ?? throw new NotFoundException($"Instance {id} not found.");

        instance.Name = name;
        instance.Location = location;
        await db.SaveChangesAsync(ct);
        return instance;
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken ct = default)
    {
        var instance = await db.Instances
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == companyId && i.IsActive, ct)
            ?? throw new NotFoundException($"Instance {id} not found.");

        instance.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<InstanceChannel>> GetChannelsAsync(int instanceId, int companyId, CancellationToken ct = default)
    {
        var exists = await db.Instances
            .AnyAsync(i => i.Id == instanceId && i.CompanyId == companyId && i.IsActive, ct);

        if (!exists) throw new NotFoundException($"Instance {instanceId} not found.");

        return await db.InstanceChannels
            .Include(ic => ic.Channel)
            .Where(ic => ic.InstanceId == instanceId)
            .OrderBy(ic => ic.SortOrder)
            .ToListAsync(ct);
    }

    public async Task AssignChannelAsync(int instanceId, int channelId, int companyId, CancellationToken ct = default)
    {
        var exists = await db.Instances
            .AnyAsync(i => i.Id == instanceId && i.CompanyId == companyId && i.IsActive, ct);

        if (!exists) throw new NotFoundException($"Instance {instanceId} not found.");

        // Idempotent — no error if already assigned
        var alreadyAssigned = await db.InstanceChannels
            .AnyAsync(ic => ic.InstanceId == instanceId && ic.ChannelId == channelId, ct);
        if (alreadyAssigned) return;

        await tierLimitService.EnforceChannelLimitAsync(companyId, instanceId, ct);

        var channelExists = await db.Channels
            .AnyAsync(c => c.Id == channelId && c.IsActive, ct);
        if (!channelExists) throw new NotFoundException($"Channel {channelId} not found.");

        var nextSort = await db.InstanceChannels
            .Where(ic => ic.InstanceId == instanceId)
            .MaxAsync(ic => (int?)ic.SortOrder, ct) ?? 0;

        db.InstanceChannels.Add(new InstanceChannel
        {
            InstanceId = instanceId,
            ChannelId = channelId,
            SortOrder = nextSort + 1,
            AssignedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<Instance?> GetByConnectionKeyAsync(string connectionKey, CancellationToken ct = default) =>
        await db.Instances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ConnectionKey == connectionKey && i.IsActive, ct);

    public async Task RemoveChannelAsync(int instanceId, int channelId, int companyId, CancellationToken ct = default)
    {
        var assignment = await db.InstanceChannels
            .Include(ic => ic.Instance)
            .FirstOrDefaultAsync(ic =>
                ic.InstanceId == instanceId &&
                ic.ChannelId == channelId &&
                ic.Instance.CompanyId == companyId &&
                ic.Instance.IsActive, ct)
            ?? throw new NotFoundException("Channel assignment not found.");

        db.InstanceChannels.Remove(assignment);
        await db.SaveChangesAsync(ct);
    }
}
