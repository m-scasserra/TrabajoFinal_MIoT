using Backend.Common.Security;
using Backend.Features.Auth.Dtos;
using Backend.Features.Auth.Models;
using Dapper;
using Npgsql;
using BCrypt.Net;

namespace Backend.Features.Auth;

public sealed class AuthService(NpgsqlConnection db, ITokenService tokens) : IAuthService
{
    public async Task<LoginResult?> LoginAsync(LoginRequest req, string? ip, string? userAgent)
    {
        var user = await db.QuerySingleOrDefaultAsync<UserRow>(
            """
            SELECT id, email, password_hash, role, org_id, active, email_verified_at
            FROM general.users WHERE email = @Email
            """,
            new { req.Email });

        if (user is null
            || user.EmailVerifiedAt is null
            || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)
            || !user.Active)
            return null;

        var accessToken = tokens.GenerateJwtToken(user.Id, user.Role, user.OrgId);
        var rawRefreshToken = tokens.GenerateRefreshToken();

        await InsertSessionAsync(user.Id, tokens.HashToken(rawRefreshToken), ip, userAgent);

        return new LoginResult(
            accessToken,
            rawRefreshToken,
            new UserDto(user.Id, user.Email, user.Role));
    }

    public async Task<RefreshResult?> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent)
    {
        var tokenHash = tokens.HashToken(rawRefreshToken);

        var session = await db.QuerySingleOrDefaultAsync<SessionQueryResult>(
            """
            SELECT s.id AS SessionID, s.user_id AS UserId, u.role, u.org_id AS OrgId, u.active
            FROM general.user_sessions s
            JOIN general.users u ON u.id = s.user_id
            WHERE s.refresh_token_hash = @Hash
              AND s.revoked_at is NULL
              AND s.expires_at > NOW();
            """,
            new { Hash = tokenHash });

        if (session is null || !session.Active)
            return null;

        await db.ExecuteAsync(
            "UPDATE general.user_sessions SET revoked_at = NOW() where id = @SessionId",
            new { session.SessionId });

        var newRawRefreshToken = tokens.GenerateRefreshToken();
        await InsertSessionAsync(session.UserId, tokens.HashToken(newRawRefreshToken), ip, userAgent);

        var accessToken = tokens.GenerateJwtToken(session.UserId, session.Role, session.OrgId);

        return new RefreshResult(accessToken, newRawRefreshToken);
    }

    public async Task LogoutAsync(string rawRefreshToken)
    {
        var tokenHash = tokens.HashToken(rawRefreshToken);

        await db.ExecuteAsync(
            "UPDATE general.user_sessions SET revoked_at = NOW() WHERE refresh_token_hash = @Hash",
            new { Hash = tokenHash });
    }

    private async Task InsertSessionAsync(Guid userId, string hash, string? ip, string? userAgent)
    {
        await db.ExecuteAsync(
            """
            INSERT INTO general.user_sessions
                (user_id, refresh_token_hash, ip_address, user_agent, expires_at)
            VALUES (@UserId, @Hash, @IpAdress::inet, @UserAgent, @ExpiresAt);
            """,
            new
            {
                UserId = userId,
                Hash = hash,
                IPAddress = ip,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
    }

    public async Task<bool> ConfirmAccountAsync(string rawToken, string newPassword)
    {
        var tokenHash = tokens.HashToken(rawToken);

        if (db.State != System.Data.ConnectionState.Open)
            await db.OpenAsync();

        await using var tx = await db.BeginTransactionAsync();

        try
        {
            // Check if the token exists, is of type EMAIL_VERIFICATION, is not used, and is not expired
            var row = await db.QuerySingleOrDefaultAsync<(Guid TokenId, Guid UserId)>(
                """
                SELECT id AS TokenId, user_id AS UserId
                FROM general.user_tokens
                WHERE token_hash = @Hash
                    AND type = 'EMAIL_VERIFICATION'
                    AND used_at IS NULL
                    AND expires_at > NOW();
                """,
                new { Hash = tokenHash }, tx);

            if (row.UserId == Guid.Empty)
            {
                await tx.RollbackAsync();
                return false;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // Update user's password and mark email as verified
            await db.ExecuteAsync(
                """
                UPDATE general.users
                SET password_hash = @Hash,
                    email_verified_at = NOW()
                WHERE id = @UserId;
                """,
                new { Hash = passwordHash, UserId = row.UserId }, tx);

            // Mark the token as used
            await db.ExecuteAsync(
                "UPDATE general.user_tokens SET used_at = NOW() WHERE id = @TokenId",
                new { TokenId = row.TokenId }, tx);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

}