using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Users.Dtos;

public record CreateUserRequest(
    [property: Required, MaxLength(150)] string Fullname,
    [property: Required, EmailAddress, MaxLength(150)] string Email,
    [property: Required] string Role);