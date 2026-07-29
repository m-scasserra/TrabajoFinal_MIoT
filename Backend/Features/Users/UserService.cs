using Backend.Common;
using Backend.Common.Email;
using Backend.Common.Security;
using Backend.Features.Users.Dtos;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Features.Users;

public sealed class UserService(
    NpgsqlConnection db,
    ITokenService tokens,
    IEmailSender email,
    IOptions<AppSettings> appOptions) : IUserService
{
    private readonly AppSettings _app = appOptions.Value;
    private const string UnusablePasswordHash = "!";

    private const string SelectBase = """
        SELECT id AS Id, org_id AS OrgId, fullname AS Fullname, email AS Email,
            role AS Role, active AS Active,
            (email_verified_at IS NOT NULL) AS EmailVerified,
            created_at AS CreatedAt
        FROM general.users
        """;

    public async Task<IEnumerable<UserListItemDto>> ListAsync(CurrentUser me)
    {
        if (me.IsSuperAdmin)
            return await db.QueryAsync<UserListItemDto>(
                $"{SelectBase} ORDER BY created_at DESC");

        return await db.QueryAsync<UserListItemDto>(
            $"{SelectBase} WHERE org_id = @OrgId ORDER BY created_at DESC",
            new { OrgId = me.RequireOrgId() });
    }

    public async Task<UserListItemDto?> GetByIdAsync(CurrentUser me, Guid id)
    {
        if (me.IsSuperAdmin)
            return await db.QuerySingleOrDefaultAsync<UserListItemDto>(
                $"{SelectBase} WHERE id = @Id", new { Id = id });

        return await db.QuerySingleOrDefaultAsync<UserListItemDto>(
            $"{SelectBase} WHERE id = @Id AND org_id = @OrgId",
            new { Id = id, OrgId = me.RequireOrgId() });
    }

    public async Task<Guid> CreateAsync(CurrentUser me, CreateUserRequest req)
    {
        if (!UserRoleRules.CanAssign(me.Role, req.Role))
            throw new InvalidOperationException(
                $"Cannot assign role {req.Role} with your current role {me.Role}.");


        var orgId = me.RequireOrgId();

        if (db.State != System.Data.ConnectionState.Open)
            await db.OpenAsync();

        await using var tx = await db.BeginTransactionAsync();
        try
        {
            var exists = await db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM general.users WHERE email = @Email)",
                new { req.Email }, tx);

            if (exists)
                throw new InvalidOperationException(
                    $"User with email {req.Email} already exists.");
            var userId = await db.ExecuteScalarAsync<Guid>(
                """
                INSERT INTO general.users (org_id, fullname, email, password_hash, role)
                VALUES (@OrgId, @FullName, @Email, @PasswordHash, @Role::general.user_role)
                RETURNING id;
                """,
                new
                {
                    OrgId = orgId,
                    req.FullName,
                    req.Email,
                    PasswordHash = UnusablePasswordHash,
                    req.Role
                }, tx);

            var rawToken = tokens.GenerateInvitationToken();
            await db.ExecuteAsync(
                """
                    INSERT INTO general.user_tokens (user_id, type, token_hash, expires_at)
                    VALUES (@UserId, 'EMAIL_VERIFICATION', @TokenHash, @ExpiresAt);
                    """,
                new
                {
                    UserId = userId,
                    TokenHash = tokens.HashToken(rawToken),
                    ExpiresAt = DateTime.UtcNow.AddHours(_app.InvitationTokenHours)
                }, tx);

            await tx.CommitAsync();

            var link = $"{_app.FrontendBaseUrl}/confirm-account?token={rawToken}";
            await email.SendInvitationAsync(req.Email, req.FullName, link);

            return userId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(CurrentUser me, Guid id, UpdateUserRequest req)
    {
        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";

        var affected = await db.ExecuteAsync(
            $"""
            UPDATE general.users
            SET fullname = @Fullname, active = @Active
            WHERE id = @Id{whereOrg};
            """,
            new { req.Fullname, req.Active, Id = id, OrgId = me.OrgId });

        return affected > 0;
    }

    public async Task<bool> DeactivateAsync(CurrentUser me, Guid id)
    {
        if (id == me.UserId)
            throw new InvalidOperationException("Cannot deactivate your own account.");

        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";

        var affected = await db.ExecuteAsync(
            $"UPDATE general.users SET active = false WHERE id = @Id{whereOrg};",
            new { Id = id, OrgId = me.OrgId });

        return affected > 0;
    }

}