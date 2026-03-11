using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tunora.API.DTOs.Channels;
using Tunora.Infrastructure.Data;

namespace Tunora.API.Controllers;

// Direct DbContext injection is intentional here — channels are read-only catalog data
// with no business logic. A service wrapper would add indirection for no benefit.
[Route("api/v1/channels")]
public class ChannelsController(ApplicationDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var channels = await db.Channels
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.IconName, c.AccentColor))
            .ToListAsync(ct);

        return Ok(channels);
    }
}
