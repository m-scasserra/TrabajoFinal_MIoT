using Backend.Common.Security;
using Backend.Features.Gateways.Dtos;

namespace Backend.Features.Gateways;

public static class GatewayEndpoints
{
    public static void MapGatewayEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/gateways")
            .RequireAuthorization(Policies.OrgAdminOrAbove);

        group.MapGet("/", async (CurrentUser me, IGatewayService gw) =>
            Results.Ok(await gw.ListAsync(me)));

        group.MapGet("/{eui}", async (string eui, CurrentUser me, IGatewayService gw) =>
        {
            var g = await gw.GetByEuiAsync(me, eui);
            return g is null ? Results.NotFound() : Results.Ok(g);
        });

        group.MapPost("/", async (
            CreateGatewayRequest req, CurrentUser me, IGatewayService gw, CancellationToken ct) =>
        {
            try
            {
                var created = await gw.CreateAsync(me, req, ct);

                return Results.Created($"/api/v1/gateways/{created.GatewayEui}", created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPut("/{eui}", async (
            string eui, UpdateGatewayRequest req, CurrentUser me, IGatewayService gw, CancellationToken ct) =>
        {
            var updated = await gw.UpdateAsync(me, eui, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{eui}", async (
            string eui, CurrentUser me, IGatewayService gw, CancellationToken ct) =>
        {
            var ok = await gw.DeleteAsync(me, eui, ct);
            return ok
                ? Results.Ok(new { message = $"Gateway marked for deletion.", eui })
                : Results.NotFound();
        });
    }
}