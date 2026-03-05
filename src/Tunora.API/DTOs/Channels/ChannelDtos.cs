namespace Tunora.API.DTOs.Channels;

public record ChannelDto(
    int Id,
    string Name,
    string Description,
    string IconName,
    string AccentColor
);
