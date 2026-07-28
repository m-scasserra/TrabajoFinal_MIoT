namespace Backend.Common.Security;

public interface ITokenService
{
    string GenerateJwtToken(Guid userId, string role, Guid? orgId);
    string GenerateRefreshToken();
    string HashToken(string token);
}