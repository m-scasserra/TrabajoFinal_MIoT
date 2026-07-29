using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Common.Seed;

public sealed class DbSeeder(
    NpgsqlConnection db,
    IOptions<SuperAdminSeedSettings> options,
    ILogger<DbSeeder> logger)
{
    private readonly SuperAdminSeedSettings _cfg = options.Value;

    public async Task SeedSuperAdminAsync()
    {
        // Check if a super admin user already exists
        var exists = await db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM general.users WHERE role = 'SUPERADMIN')");

        if (exists)
        {
            logger.LogInformation("Super admin user already exists. Skipping seeding.");
            return;
        }

        // Validate configuration
        if (string.IsNullOrWhiteSpace(_cfg.Email) || string.IsNullOrWhiteSpace(_cfg.Password))
        {
            logger.LogWarning("Super admin email or password is not set. Skipping seeding.");
            return;
        }

        // Create super admin user
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(_cfg.Password);

        await db.ExecuteAsync(
            """
            INSERT INTO general.users
                (org_id, fullname, email, password_hash, role, email_verified_at)
            VALUES 
                (NULL, @Fullname, @Email, @PasswordHash, 'SUPERADMIN', NOW());
            """,
            new
            {
                Fullname = _cfg.Fullname,
                Email = _cfg.Email,
                PasswordHash = passwordHash
            });

        logger.LogInformation("Super admin user seeded successfully with email {Email}.", _cfg.Email);
    }
}