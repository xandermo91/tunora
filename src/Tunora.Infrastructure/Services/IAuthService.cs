using Tunora.Infrastructure.Models;

namespace Tunora.Infrastructure.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(int userId, CancellationToken ct = default);
}
