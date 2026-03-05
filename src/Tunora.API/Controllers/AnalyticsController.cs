using Microsoft.AspNetCore.Mvc;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ApiControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var stats = await analyticsService.GetOverviewAsync(CompanyId, ct);
        return Ok(stats);
    }
}
