using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Users.Dtos;

public record UpdateUserRequest(
    [property: Required, MaxLength(150)] string Fullname,
    [property: Required] bool Active);