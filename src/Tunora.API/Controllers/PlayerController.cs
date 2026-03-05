using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tunora.API.DTOs.Playback;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

/// <summary>Endpoints called by the in-store kiosk player. Requires Kiosk JWT.</summary>
[Route("api/v1/player")]
[ApiController]
[Authorize]
public class PlayerController(IPlaybackService playbackService) : ControllerBase
{
    [HttpGet("tracks/next")]
    public async Task<IActionResult> NextTrack([FromQuery] int channelId, CancellationToken ct)
    {
        var role = User.FindFirstValue("role");
        if (role != "Kiosk") return Forbid();

        var track = await playbackService.GetNextTrackAsync(channelId, ct);
        if (track is null) return NotFound(new { error = "No tracks found for this channel." });

        return Ok(new TrackResponseDto(track.TrackId, track.Title, track.ArtistName, track.AudioUrl, track.AlbumImageUrl));
    }
}
