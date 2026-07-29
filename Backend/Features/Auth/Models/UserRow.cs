namespace Backend.Features.Auth.Models;

internal record UserRow(
    Guid Id, string Email, string PasswordHash, string Role,
    Guid? OrgId, bool Active, DateTime? EmailVerifiedAt);