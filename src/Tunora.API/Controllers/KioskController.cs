using Microsoft.AspNetCore.Mvc;
using Tunora.API.DTOs.Playback;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[Route("api/v1/kiosk")]
[ApiController]
public class KioskController(IKioskService kioskService) : ControllerBase
{
    [HttpPost("auth")]
    public async Task<IActionResult> Authenticate([FromBody] KioskAuthRequestDto dto, CancellationToken ct)
    {
        var result = await kioskService.AuthenticateAsync(dto.ConnectionKey, ct);
        if (result is null) return Unauthorized(new { error = "Invalid connection key." });

        return Ok(new KioskAuthResponseDto(result.AccessToken, result.InstanceId, result.InstanceName));
    }
}
