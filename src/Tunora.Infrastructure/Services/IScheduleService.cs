using Tunora.Core.Domain.Entities;
using Tunora.Infrastructure.Models;

namespace Tunora.Infrastructure.Services;

public interface IScheduleService
{
    Task<List<Schedule>> GetByInstanceAsync(int instanceId, int companyId, CancellationToken ct = default);
    Task<List<Schedule>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Schedule> CreateAsync(int instanceId, int companyId, CreateScheduleRequest req, CancellationToken ct = default);
    Task<Schedule> UpdateAsync(int id, int companyId, UpdateScheduleRequest req, CancellationToken ct = default);
    Task DeleteAsync(int id, int companyId, CancellationToken ct = default);
    Task RegisterWithQuartzAsync(Schedule schedule, CancellationToken ct = default);
}
