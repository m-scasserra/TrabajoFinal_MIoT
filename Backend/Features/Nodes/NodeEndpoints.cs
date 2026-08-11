using Backend.Common.Security;
using Backend.Features.Nodes.Dtos;

namespace Backend.Features.Nodes;

public static class NodeEndpoints
{
    public static void MapNodeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/nodes")
            .RequireAuthorization(Policies.OrgAdminOrAbove);

        group.MapGet("/", async (CurrentUser me, INodeService nodes) =>
            Results.Ok(await nodes.ListAsync(me)));

        group.MapGet("/{devEui}", async (string devEui, CurrentUser me, INodeService nodes) =>
        {
            var n = await nodes.GetByEuiAsync(me, devEui);
            return n is null ? Results.NotFound() : Results.Ok(n);
        });

        group.MapPost("/activate", async (
            ActivateNodeRequest req, CurrentUser me, INodeService nodes, CancellationToken ct) =>
        {
            try
            {
                var created = await nodes.ActivateAsync(me, req, ct);
                return Results.Created($"/api/v1/nodes/{created.DevEui}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, CurrentUser me, INodeService nodes, CancellationToken ct) =>
        {
            var ok = await nodes.DeleteAsync(me, id, ct);
            return ok
                ? Results.Ok(new { message = "Node liberated and marked for deletion." })
                : Results.NotFound();
        });

        var provGroup = app.MapGroup("/api/v1/provisioning/nodes")
            .RequireAuthorization(Policies.SuperAdminOnly);

        provGroup.MapPost("/", async (
            ProvisionNodeRequest req, INodeService nodes) =>
        {
            try
            {
                var result = await nodes.ProvisionAsync(req);
                return Results.Created($"/api/v1/nodes/{result.DevEui}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        provGroup.MapPut("/{id:guid}", async (
            Guid id, UpdateProvisionRequest req, INodeService nodes) =>
        {
            try
            {
                var updated = await nodes.UpdateProvisionAsync(id, req);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        provGroup.MapPost("/{id:guid}/rotate-key", async (
            Guid id, INodeService nodes) =>
        {
            try
            {
                var newKey = await nodes.RotateAppKeyAsync(id);
                return Results.Ok(new { appKey = newKey });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}