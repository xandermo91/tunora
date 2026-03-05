using System.ComponentModel.DataAnnotations;

namespace Tunora.API.DTOs.Auth;

public record RegisterDto(
    [Required, MaxLength(200)] string CompanyName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record RefreshTokenDto(
    [Required] string RefreshToken
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken
);
