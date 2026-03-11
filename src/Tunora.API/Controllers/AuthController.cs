using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tunora.API.DTOs.Auth;
using Tunora.Infrastructure.Models;
using Tunora.Infrastructure.Services;

namespace Tunora.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(new RegisterRequest(
            dto.CompanyName, dto.Email, dto.Password, dto.FirstName, dto.LastName), ct);

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new AuthResponseDto(result.AccessToken!, result.RefreshToken!));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var result = await authService.LoginAsync(new LoginRequest(dto.Email, dto.Password), ct);

        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new AuthResponseDto(result.AccessToken!, result.RefreshToken!));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(dto.RefreshToken, ct);

        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(new AuthResponseDto(result.AccessToken!, result.RefreshToken!));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        await authService.LogoutAsync(userId, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId))
            return Unauthorized();

        var changed = await authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword, ct);
        if (!changed)
            return BadRequest(new { error = "Current password is incorrect." });

        return NoContent();
    }
}
