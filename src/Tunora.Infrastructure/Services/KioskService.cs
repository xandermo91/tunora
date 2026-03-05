using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tunora.Core.Domain.Entities;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Options;

namespace Tunora.Infrastructure.Services;

public class KioskService(ApplicationDbContext db, IOptions<JwtOptions> jwtOptions) : IKioskService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<KioskAuthResult?> AuthenticateAsync(string connectionKey, CancellationToken ct = default)
    {
        var instance = await db.Instances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ConnectionKey == connectionKey && i.IsActive, ct);

        if (instance is null) return null;

        var token = GenerateKioskToken(instance);
        return new KioskAuthResult(token, instance.Id, instance.Name);
    }

    private string GenerateKioskToken(Instance instance)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("instanceId", instance.Id.ToString()),
            new Claim("companyId",  instance.CompanyId.ToString()),
            new Claim("role",       "Kiosk"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
