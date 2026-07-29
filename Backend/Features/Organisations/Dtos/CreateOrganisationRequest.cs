using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Organisations.Dtos;

public record CreateOrganisationRequest(
    [property: Required, MaxLength(100)] string Name,
    [property: MaxLength(20)] string? Cuit,
    [property: Required, MaxLength(150)] string AdminFullName,
    [property: Required, EmailAddress, MaxLength(150)] string AdminEmail);