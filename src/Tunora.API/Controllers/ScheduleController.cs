using Microsoft.AspNetCore.Mvc;
using Tunora.API.DTOs.Schedules;
using Tunora.Infrastructure.Models;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/instances/{instanceId:int}/schedules")]
public class ScheduleController(IScheduleService scheduleService, ITierLimitService tierLimitService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int instanceId, CancellationToken ct)
    {
        var schedules = await scheduleService.GetByInstanceAsync(instanceId, CompanyId, ct);
        return Ok(schedules.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(int instanceId, [FromBody] CreateScheduleDto dto, CancellationToken ct)
    {
        await tierLimitService.EnforceSchedulingAccessAsync(CompanyId, ct);

        var req = new CreateScheduleRequest(
            dto.ChannelId,
            dto.DaysOfWeek.Select(d => (DayOfWeek)d).ToArray(),
            TimeOnly.Parse(dto.StartTime),
            TimeOnly.Parse(dto.EndTime));

        var schedule = await scheduleService.CreateAsync(instanceId, CompanyId, req, ct);
        return CreatedAtAction(nameof(List), new { instanceId }, ToDto(schedule));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int instanceId, int id, [FromBody] UpdateScheduleDto dto, CancellationToken ct)
    {
        var req = new UpdateScheduleRequest(
            dto.ChannelId,
            dto.DaysOfWeek.Select(d => (DayOfWeek)d).ToArray(),
            TimeOnly.Parse(dto.StartTime),
            TimeOnly.Parse(dto.EndTime),
            dto.IsActive);

        var schedule = await scheduleService.UpdateAsync(id, CompanyId, req, ct);
        return Ok(ToDto(schedule));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int instanceId, int id, CancellationToken ct)
    {
        await scheduleService.DeleteAsync(id, CompanyId, ct);
        return NoContent();
    }

    private static ScheduleDto ToDto(Core.Domain.Entities.Schedule s) => new(
        s.Id,
        s.Name,
        s.InstanceId,
        s.ChannelId,
        s.Channel.Name,
        s.Channel.AccentColor,
        s.DaysOfWeek.Select(d => (int)d).ToArray(),
        s.StartTime.ToString("HH:mm"),
        s.EndTime.ToString("HH:mm"),
        s.IsActive,
        s.CreatedAt);
}
