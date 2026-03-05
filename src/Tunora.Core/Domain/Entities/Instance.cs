using Tunora.Core.Domain.Enums;
using Tunora.Core.Domain.Interfaces;

namespace Tunora.Core.Domain.Entities;

public class Instance : IAuditableEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ConnectionKey { get; set; } = string.Empty;
    public InstanceStatus Status { get; set; } = InstanceStatus.Offline;
    public int? ActiveChannelId { get; set; }
    public string? CurrentTrackTitle { get; set; }
    public string? CurrentTrackArtist { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public Channel? ActiveChannel { get; set; }
    public ICollection<InstanceChannel> InstanceChannels { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
    public ICollection<PlaybackLog> PlaybackLogs { get; set; } = [];
}
