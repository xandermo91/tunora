using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Services;

public interface IInstanceService
{
    Task<List<Instance>> GetAllAsync(int companyId, CancellationToken ct = default);
    Task<Instance?> GetByIdAsync(int id, int companyId, CancellationToken ct = default);
    Task<Instance> CreateAsync(string name, string location, int companyId, CancellationToken ct = default);
    Task<Instance> UpdateAsync(int id, string name, string location, int companyId, CancellationToken ct = default);
    Task DeleteAsync(int id, int companyId, CancellationToken ct = default);
    Task<List<InstanceChannel>> GetChannelsAsync(int instanceId, int companyId, CancellationToken ct = default);
    Task AssignChannelAsync(int instanceId, int channelId, int companyId, CancellationToken ct = default);
    Task RemoveChannelAsync(int instanceId, int channelId, int companyId, CancellationToken ct = default);
    Task<Instance?> GetByConnectionKeyAsync(string connectionKey, CancellationToken ct = default);
}
