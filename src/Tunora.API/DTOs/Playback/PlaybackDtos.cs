using System.ComponentModel.DataAnnotations;

namespace Tunora.API.DTOs.Playback;

public record KioskAuthRequestDto([Required] string ConnectionKey);

public record KioskAuthResponseDto(string AccessToken, int InstanceId, string InstanceName);

public record PlayCommandDto([Required] int ChannelId);

public record ChangeChannelDto([Required] int ChannelId);

public record TrackResponseDto(
    string TrackId,
    string Title,
    string ArtistName,
    string AudioUrl,
    string? AlbumImageUrl
);
