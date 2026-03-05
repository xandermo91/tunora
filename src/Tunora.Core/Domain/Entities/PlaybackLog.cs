using Tunora.Core.Domain.Enums;

namespace Tunora.Core.Domain.Entities;

public class PlaybackLog
{
    public long Id { get; set; }
    public int InstanceId { get; set; }
    public int ChannelId { get; set; }
    public string TrackId { get; set; } = string.Empty;
    public string TrackTitle { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public PlaybackEventType EventType { get; set; }
    public DateTime OccurredAt { get; set; }

    public Instance Instance { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}
