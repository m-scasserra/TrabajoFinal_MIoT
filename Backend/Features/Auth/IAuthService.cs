using Backend.Features.Auth.Dtos;

namespace Backend.Features.Auth;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(LoginRequest req, string? ip, string? userAgent);
    Task<RefreshResult?> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent);
    Task LogoutAsync(string rawRefreshToken);

}

public record LoginResult(string AccessToken, string RawRefreshToken, UserDto User);
public record RefreshResult(string AccessToken, string RawRefreshToken);