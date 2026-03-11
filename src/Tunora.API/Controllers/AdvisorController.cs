using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tunora.API.DTOs.Advisor;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/advisor")]
[EnableRateLimiting("advisor")]
public class AdvisorController(IClaudeAdvisorService advisor, IAnalyticsService analytics) : ApiControllerBase
{
    [HttpPost("music")]
    public async Task<IActionResult> GetMusicAdvice([FromBody] MusicAdviceDto dto, CancellationToken ct)
    {
        try
        {
            var result = await advisor.GetMusicAdviceAsync(dto.BusinessType, dto.Description, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            return StatusCode(503, new { error = "AI advisor is currently unavailable." });
        }
    }

    [HttpPost("analytics-insight")]
    public async Task<IActionResult> GetAnalyticsInsight(CancellationToken ct)
    {
        try
        {
            var stats = await analytics.GetOverviewAsync(CompanyId, ct);
            var result = await advisor.GetAnalyticsInsightAsync(stats, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            return StatusCode(503, new { error = "AI advisor is currently unavailable." });
        }
    }
}
