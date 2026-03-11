using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService, ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var stats = await analyticsService.GetOverviewAsync(CompanyId, ct);
        return Ok(stats);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var logs = await db.PlaybackLogs
            .AsNoTracking()
            .Where(l => l.Instance.CompanyId == CompanyId)
            .OrderByDescending(l => l.OccurredAt)
            .Take(50_000)
            .Select(l => new
            {
                l.OccurredAt,
                InstanceName = l.Instance.Name,
                ChannelName = l.Channel.Name,
                l.TrackTitle,
                l.ArtistName,
                EventType = l.EventType.ToString(),
            })
            .ToListAsync(ct);

        // UTF-8 with BOM so Excel on Windows reads non-ASCII characters correctly
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var sb = new StringBuilder();
        sb.AppendLine("OccurredAt,Instance,Channel,Track,Artist,Event");
        foreach (var log in logs)
        {
            sb.AppendLine(
                $"{log.OccurredAt:o}," +
                $"\"{log.InstanceName.Replace("\"", "\"\"")}\"," +
                $"\"{log.ChannelName.Replace("\"", "\"\"")}\"," +
                $"\"{log.TrackTitle.Replace("\"", "\"\"")}\"," +
                $"\"{log.ArtistName.Replace("\"", "\"\"")}\"," +
                $"{log.EventType}");
        }

        var bytes = utf8Bom.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "tunora-analytics.csv");
    }
}
