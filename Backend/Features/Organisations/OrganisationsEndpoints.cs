using Backend.Common.Security;
using Backend.Features.Organisations.Dtos;

namespace Backend.Features.Organisations;

public static class OrganisationsEndpoints
{
    public static void MapOrganisationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/organisations");

        //
        group.MapPost("/", async (
            CreateOrganisationRequest req, IOrganisationService orgs) =>
        {
            var created = await orgs.CreateWithAdminAsync(req);
            return Results.Created($"/api/v1/organisations/{created.Id}", created);
        })
        .RequireAuthorization(Policies.SuperAdminOnly);
    }
}