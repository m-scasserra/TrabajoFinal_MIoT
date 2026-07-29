namespace Backend.Features.Users.Dtos;

public record UserListItemDto(
    Guid Id,
    Guid? OrgId,
    string FullName,
    string Email,
    string Role,
    bool Active,
    bool EmailVerified,
    DateTime CreatedAt);