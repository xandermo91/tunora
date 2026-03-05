using Microsoft.AspNetCore.Mvc;
using Tunora.API.DTOs.Instances;
using Tunora.Core.Domain.Entities;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/instances")]
public class InstanceController(IInstanceService instanceService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var instances = await instanceService.GetAllAsync(CompanyId, ct);
        return Ok(instances.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });
        return Ok(ToDto(instance));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstanceDto dto, CancellationToken ct)
    {
        var instance = await instanceService.CreateAsync(dto.Name, dto.Location, CompanyId, ct);
        return CreatedAtAction(nameof(GetById), new { id = instance.Id }, ToCreatedDto(instance));
    }

    [HttpGet("{id:int}/connection-key")]
    public async Task<IActionResult> GetConnectionKey(int id, CancellationToken ct)
    {
        var instance = await instanceService.GetByIdAsync(id, CompanyId, ct);
        if (instance is null) return NotFound(new { error = "Instance not found." });
        return Ok(new ConnectionKeyDto(instance.ConnectionKey));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstanceDto dto, CancellationToken ct)
    {
        var instance = await instanceService.UpdateAsync(id, dto.Name, dto.Location, CompanyId, ct);
        return Ok(ToDto(instance));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await instanceService.DeleteAsync(id, CompanyId, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/channels")]
    public async Task<IActionResult> GetChannels(int id, CancellationToken ct)
    {
        var channels = await instanceService.GetChannelsAsync(id, CompanyId, ct);
        return Ok(channels.Select(ic => new AssignedChannelDto(
            ic.ChannelId, ic.Channel.Name, ic.Channel.IconName, ic.Channel.AccentColor, ic.SortOrder)));
    }

    [HttpPost("{id:int}/channels")]
    public async Task<IActionResult> AssignChannel(int id, [FromBody] AssignChannelDto dto, CancellationToken ct)
    {
        await instanceService.AssignChannelAsync(id, dto.ChannelId, CompanyId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/channels/{channelId:int}")]
    public async Task<IActionResult> RemoveChannel(int id, int channelId, CancellationToken ct)
    {
        await instanceService.RemoveChannelAsync(id, channelId, CompanyId, ct);
        return NoContent();
    }

    private static List<AssignedChannelDto> MapChannels(Instance i) =>
        i.InstanceChannels
            .OrderBy(ic => ic.SortOrder)
            .Select(ic => new AssignedChannelDto(
                ic.ChannelId, ic.Channel.Name, ic.Channel.IconName, ic.Channel.AccentColor, ic.SortOrder))
            .ToList();

    private static InstanceDto ToDto(Instance i) => new(
        i.Id, i.Name, i.Location, i.Status.ToString(),
        i.ActiveChannelId, i.CurrentTrackTitle, i.CurrentTrackArtist,
        i.LastSeenAt, i.CreatedAt, MapChannels(i));

    private static InstanceCreatedDto ToCreatedDto(Instance i) => new(
        i.Id, i.Name, i.Location, i.ConnectionKey, i.Status.ToString(),
        i.CreatedAt, MapChannels(i));
}
