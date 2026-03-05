using System.ComponentModel.DataAnnotations;

namespace Tunora.Infrastructure.Models;

public record RegisterRequest(
    [Required, MaxLength(200)] string CompanyName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    string? Error
)
{
    public static AuthResult Ok(string accessToken, string refreshToken) =>
        new(true, accessToken, refreshToken, null);

    public static AuthResult Fail(string error) =>
        new(false, null, null, error);
}
