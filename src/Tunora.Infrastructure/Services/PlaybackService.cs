using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Entities;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Data;

namespace Tunora.Infrastructure.Services;

public class PlaybackService(ApplicationDbContext db, JamendoClient jamendo) : IPlaybackService
{
    public async Task<TrackDto?> GetNextTrackAsync(int channelId, CancellationToken ct = default)
    {
        var tag = await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == channelId && c.IsActive)
            .Select(c => c.JamendoTag)
            .FirstOrDefaultAsync(ct);

        if (tag is null) return null;

        var track = await jamendo.GetTrackByTagAsync(tag, ct);
        if (track is null) return null;

        return new TrackDto(track.Id, track.Title, track.ArtistName, track.AudioUrl, track.AlbumImageUrl);
    }

    public async Task UpdateInstanceStateAsync(int instanceId, InstanceStatus status, int? channelId,
        string? trackTitle, string? trackArtist, CancellationToken ct = default)
    {
        var instance = await db.Instances.FindAsync([instanceId], ct);
        if (instance is null) return;

        instance.Status = status;
        if (channelId.HasValue) instance.ActiveChannelId = channelId;
        instance.CurrentTrackTitle = trackTitle;
        instance.CurrentTrackArtist = trackArtist;
        instance.LastSeenAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task WriteLogAsync(int instanceId, int channelId, PlaybackEventType eventType,
        string trackId = "", string trackTitle = "", string artistName = "", CancellationToken ct = default)
    {
        db.PlaybackLogs.Add(new PlaybackLog
        {
            InstanceId = instanceId,
            ChannelId  = channelId,
            TrackId    = trackId,
            TrackTitle = trackTitle,
            ArtistName = artistName,
            EventType  = eventType,
            OccurredAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
