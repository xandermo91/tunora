using Microsoft.AspNetCore.Mvc;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/schedules")]
public class CompanySchedulesController(IScheduleService scheduleService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var schedules = await scheduleService.GetByCompanyAsync(CompanyId, ct);
        return Ok(schedules.Select(s => new
        {
            s.Id,
            s.Name,
            s.InstanceId,
            InstanceName = s.Instance.Name,
            s.ChannelId,
            ChannelName = s.Channel.Name,
            ChannelAccentColor = s.Channel.AccentColor,
            DaysOfWeek = s.DaysOfWeek.Select(d => (int)d).ToArray(),
            StartTime = s.StartTime.ToString("HH:mm"),
            EndTime = s.EndTime.ToString("HH:mm"),
            s.IsActive,
            s.CreatedAt,
        }));
    }
}
