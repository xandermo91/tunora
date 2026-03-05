namespace Tunora.Core.Domain.Entities;

public class InstanceChannel
{
    public int InstanceId { get; set; }
    public int ChannelId { get; set; }
    public int SortOrder { get; set; }
    public DateTime AssignedAt { get; set; }

    public Instance Instance { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}
