namespace Backend.Features.Organisations.Dtos;

public record OrganisationDto(Guid Id, string Name, string? Cuit, bool Active, DateTime CreatedAt);