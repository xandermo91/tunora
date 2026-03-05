using System.ComponentModel.DataAnnotations;

namespace Tunora.Infrastructure.Options;

public class JwtOptions
{
    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Must be at least 32 characters (256 bits) for HMAC-SHA256.</summary>
    [Required, MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
