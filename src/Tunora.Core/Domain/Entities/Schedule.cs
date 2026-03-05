using Tunora.Core.Domain.Interfaces;

namespace Tunora.Core.Domain.Entities;

public class Schedule : IAuditableEntity
{
    public int Id { get; set; }
    public int InstanceId { get; set; }
    public int ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek[] DaysOfWeek { get; set; } = [];
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public string? QuartzJobKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Instance Instance { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}
