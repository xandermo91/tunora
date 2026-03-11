using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tunora.Core.Domain.Entities;
using Tunora.Core.Domain.Enums;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Models;
using Tunora.Infrastructure.Options;

namespace Tunora.Infrastructure.Services;

public class AuthService(ApplicationDbContext db, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower(), ct))
            return AuthResult.Fail("Email is already registered.");

        var slug = GenerateSlug(request.CompanyName);
        if (await db.Companies.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";

        var company = new Company
        {
            Name = request.CompanyName,
            Slug = slug,
            ContactEmail = request.Email.ToLower(),
        };

        var user = new User
        {
            Company = company,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Admin,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower() && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthResult.Fail("Invalid email or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == tokenHash && u.IsActive, ct);

        if (user is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return AuthResult.Fail("Refresh token is invalid or expired.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task LogoutAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var rawToken = GenerateRefreshToken();

        user.RefreshToken = HashToken(rawToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);
        await db.SaveChangesAsync(ct);

        return AuthResult.Ok(GenerateAccessToken(user), rawToken);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,      user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName,  user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim("companyId",                         user.CompanyId.ToString()),
            new Claim("role",                              user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string GenerateSlug(string companyName)
        => new string(companyName.ToLower()
            .Replace(" ", "-")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToArray())
            .Trim('-');
}
