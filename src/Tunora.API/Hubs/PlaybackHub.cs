using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Services;
using PlaybackEventType = Tunora.Core.Domain.Enums.PlaybackEventType;

namespace Tunora.API.Hubs;

/// <summary>Commands sent from server to player.</summary>
public record PlaybackCommand(string Type, int? ChannelId = null);

/// <summary>State reported by player, relayed to dashboard.</summary>
public record PlaybackState(
    int InstanceId,
    string Status,
    int? ChannelId,
    string? TrackId,
    string? TrackTitle,
    string? TrackArtist,
    string? AlbumImageUrl
);

[Authorize]
public class PlaybackHub(IPlaybackService playbackService, ApplicationDbContext db) : Hub
{
    // ── Player calls this to receive commands ────────────────────────────────
    public async Task JoinInstance(int instanceId)
    {
        if (!await CanAccessInstanceAsync(instanceId)) throw new HubException("Access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"instance-{instanceId}");
    }

    // ── Dashboard calls this to watch a specific instance ────────────────────
    public async Task WatchInstance(int instanceId)
    {
        if (!await CanAccessInstanceAsync(instanceId)) throw new HubException("Access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard-{instanceId}");
    }

    // ── Player calls this to report current state → relayed to dashboard ─────
    public async Task ReportState(PlaybackState state)
    {
        if (!await CanAccessInstanceAsync(state.InstanceId)) throw new HubException("Access denied.");

        var status = Enum.TryParse<InstanceStatus>(state.Status, out var s) ? s : InstanceStatus.Offline;
        await playbackService.UpdateInstanceStateAsync(
            state.InstanceId, status, state.ChannelId,
            state.TrackTitle, state.TrackArtist);

        // Log track starts (only when playing with track info)
        if (status == InstanceStatus.Playing && state.ChannelId.HasValue && !string.IsNullOrEmpty(state.TrackTitle))
        {
            await playbackService.WriteLogAsync(
                state.InstanceId, state.ChannelId.Value, PlaybackEventType.Started,
                state.TrackId ?? string.Empty, state.TrackTitle, state.TrackArtist ?? string.Empty);
        }

        await Clients.Group($"dashboard-{state.InstanceId}")
            .SendAsync("ReceiveState", state);
    }

    // ── Internal: validate the calling client belongs to this instance's company ──
    private async Task<bool> CanAccessInstanceAsync(int instanceId)
    {
        var role = Context.User?.FindFirstValue("role");

        // Kiosk: JWT carries the exact instanceId it is bound to
        if (role == "Kiosk")
        {
            var claimedInstance = Context.User?.FindFirstValue("instanceId");
            return int.TryParse(claimedInstance, out var id) && id == instanceId;
        }

        // Dashboard users: verify the instance belongs to the caller's company
        var companyIdClaim = Context.User?.FindFirstValue("companyId");
        if (!int.TryParse(companyIdClaim, out var companyId)) return false;

        return await db.Instances
            .AsNoTracking()
            .AnyAsync(i => i.Id == instanceId && i.CompanyId == companyId && i.IsActive);
    }
}
