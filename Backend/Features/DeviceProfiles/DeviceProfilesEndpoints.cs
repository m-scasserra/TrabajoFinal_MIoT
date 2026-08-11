using Backend.Common.Security;
using Backend.Features.DeviceProfiles.Dtos;

namespace Backend.Features.DeviceProfiles;

public static class DeviceProfileEndpoints
{
    public static void MapDeviceProfileEndpoints(this WebApplication app)
    {
        var readGroup = app.MapGroup("/api/v1/device-profiles")
            .RequireAuthorization(Policies.OrgAdminOrAbove);

        readGroup.MapGet("/", async (IDeviceProfileService service) =>
            Results.Ok(await service.ListAsync()));

        readGroup.MapGet("/{id:guid}", async (Guid id, IDeviceProfileService service) =>
        {
            var p = await service.GetAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        var writeGroup = app.MapGroup("/api/v1/device-profiles")
            .RequireAuthorization(Policies.SuperAdminOnly);

        writeGroup.MapPost("/", async (
            CreateDeviceProfileRequest req, IDeviceProfileService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(req, ct);
            return Results.Created($"/api/v1/device-profiles/{created.Id}", created);
        });

        writeGroup.MapPut("/{id:guid}", async (
            Guid id, CreateDeviceProfileRequest req, IDeviceProfileService service, CancellationToken ct) =>
        {
            var updated = await service.UpdateAsync(id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        writeGroup.MapDelete("/{id:guid}", async (
            Guid id, IDeviceProfileService service, CancellationToken ct) =>
        {
            try
            {
                var ok = await service.DeleteAsync(id, ct);
                return ok ? Results.Ok(new { message = $"Device profile marked for deletion.", id }) : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}