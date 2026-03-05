namespace Tunora.Core.Domain.Entities;

public class Channel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string JamendoTag { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string AccentColor { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<InstanceChannel> InstanceChannels { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
}
