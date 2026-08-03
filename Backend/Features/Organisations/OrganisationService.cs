using Backend.Common;
using Backend.Common.Email;
using Backend.Common.Security;
using Backend.Features.Organisations.Dtos;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Features.Organisations;

public sealed class OrganisationService(
    NpgsqlConnection db,
    ITokenService tokens,
    IEmailSender email,
    IOptions<AppSettings> appOptions) : IOrganisationService
{
    private readonly AppSettings _app = appOptions.Value;

    private const string UnusablePasswordHash = "!";

    public async Task<OrganisationDto> CreateWithAdminAsync(CreateOrganisationRequest req)
    {
        if (db.State != System.Data.ConnectionState.Open)
            await db.OpenAsync();

        await using var tx = await db.BeginTransactionAsync();

        try
        {
            // Check if user with this email already exists
            var exists = await db.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM general.users WHERE email = @Email)",
                new { Email = req.AdminEmail }, tx);

            if (exists)
                throw new InvalidOperationException("User with this email already exists");

            // Create organisation
            var org = await db.QuerySingleAsync<(Guid Id, string Name, string? Cuit, bool Active, DateTime CreatedAt)>(
                """
                INSERT INTO general.organisations (name, cuit)
                VALUES (@Name, @Cuit)
                RETURNING id AS Id, name AS Name, cuit AS Cuit, active AS Active, created_at AS CreatedAt;
                """,
                new { Name = req.Name, Cuit = req.Cuit }, tx);

            var userId = await db.ExecuteScalarAsync<Guid>(
                """
                INSERT INTO general.users (org_id, fullname, email, password_hash, role)
                VALUES (@OrgId, @FullName, @Email, @PasswordHash, 'ORG_ADMIN')
                RETURNING id;
                """,
                new
                {
                    OrgId = org.Id,
                    FullName = req.AdminFullName,
                    Email = req.AdminEmail,
                    PasswordHash = UnusablePasswordHash
                }, tx);

            // Generate invitation token
            var rawToken = tokens.GenerateInvitationToken();
            var tokenHash = tokens.HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddHours(_app.InvitationTokenHours);

            await db.ExecuteAsync(
                """
                INSERT INTO general.user_tokens (user_id, type, token_hash, expires_at)
                VALUES (@UserId, 'EMAIL_VERIFICATION', @TokenHash, @ExpiresAt);
                """,
                new { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt }, tx);

            await tx.CommitAsync();

            // Send invitation email
            var link = $"{_app.FrontendBaseUrl}/confirm-account?token={rawToken}";
            await email.SendInvitationAsync(req.AdminEmail, req.AdminFullName, link);

            return new OrganisationDto(org.Id, org.Name, org.Cuit, org.Active, org.CreatedAt);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}