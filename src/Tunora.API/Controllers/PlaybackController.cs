using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Tunora.API.DTOs.Playback;
using Tunora.API.Hubs;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Services;
using PlaybackEventType = Tunora.Core.Domain.Enums.PlaybackEventType;

namespace Tunora.API.Controllers;

[Route("api/v1/playback")]
public class PlaybackController(
    IInstanceService instanceService,
    IPlaybackService playbackService,
    IHubContext<PlaybackHub> hub) : ApiControllerBase
{
    [HttpPost("{id:int}/play")]
    public async Task<IActionResult> Play(int id, [FromBody] PlayCommandDto dto, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });

        if (!instance.InstanceChannels.Any(ic => ic.ChannelId == dto.ChannelId))
            return BadRequest(new { error = "Channel is not assigned to this instance." });

        await playbackService.UpdateInstanceStateAsync(id, InstanceStatus.Online, dto.ChannelId, null, null, ct);

        await hub.Clients.Group($"instance-{id}")
            .SendAsync("ReceiveCommand", new PlaybackCommand("Play", dto.ChannelId), ct);

        return NoContent();
    }

    [HttpPost("{id:int}/stop")]
    public async Task<IActionResult> Stop(int id, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });

        await playbackService.UpdateInstanceStateAsync(id, InstanceStatus.Stopped, null, null, null, ct);

        if (instance.ActiveChannelId.HasValue)
            await playbackService.WriteLogAsync(id, instance.ActiveChannelId.Value, PlaybackEventType.Stopped, ct: ct);

        await hub.Clients.Group($"instance-{id}")
            .SendAsync("ReceiveCommand", new PlaybackCommand("Stop"), ct);

        return NoContent();
    }

    [HttpPost("{id:int}/next")]
    public async Task<IActionResult> Next(int id, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });

        if (instance.ActiveChannelId.HasValue)
            await playbackService.WriteLogAsync(id, instance.ActiveChannelId.Value, PlaybackEventType.Skipped, ct: ct);

        await hub.Clients.Group($"instance-{id}")
            .SendAsync("ReceiveCommand", new PlaybackCommand("Next"), ct);

        return NoContent();
    }

    [HttpPut("{id:int}/channel")]
    public async Task<IActionResult> ChangeChannel(int id, [FromBody] ChangeChannelDto dto, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });

        if (!instance.InstanceChannels.Any(ic => ic.ChannelId == dto.ChannelId))
            return BadRequest(new { error = "Channel is not assigned to this instance." });

        await playbackService.UpdateInstanceStateAsync(id, InstanceStatus.Online, dto.ChannelId, null, null, ct);

        await hub.Clients.Group($"instance-{id}")
            .SendAsync("ReceiveCommand", new PlaybackCommand("ChangeChannel", dto.ChannelId), ct);

        return NoContent();
    }
}
