using Tunora.Core.Domain.Enums;

namespace Tunora.Infrastructure.Services;

public record TrackDto(string TrackId, string Title, string ArtistName, string AudioUrl, string? AlbumImageUrl);

public interface IPlaybackService
{
    Task<TrackDto?> GetNextTrackAsync(int channelId, CancellationToken ct = default);
    Task UpdateInstanceStateAsync(int instanceId, InstanceStatus status, int? channelId,
        string? trackTitle, string? trackArtist, CancellationToken ct = default);
    Task WriteLogAsync(int instanceId, int channelId, PlaybackEventType eventType,
        string trackId = "", string trackTitle = "", string artistName = "", CancellationToken ct = default);
}
