using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Auth.Dtos;

public record ConfirmAccountRequest(
    [property: Required] string Token,
    [property: Required, MinLength(8), MaxLength(72)] string Password);