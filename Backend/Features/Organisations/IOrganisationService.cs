using Backend.Features.Organisations.Dtos;

namespace Backend.Features.Organisations;

public interface IOrganisationService
{
    Task<OrganisationDto> CreateWithAdminAsync(CreateOrganisationRequest req);
}