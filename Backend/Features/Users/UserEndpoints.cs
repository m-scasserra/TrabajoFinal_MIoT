using Backend.Common.Security;
using Backend.Features.Users.Dtos;
using Backend.Features.Auth;

namespace Backend.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users")
            .RequireAuthorization(Policies.OrgAdminOrAbove);

        group.MapGet("/", async (CurrentUser me, IUserService users) =>
            Results.Ok(await users.ListAsync(me)));

        group.MapGet("/{id:guid}", async (Guid id, CurrentUser me, IUserService users) =>
        {
            var user = await users.GetByIdAsync(me, id);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        group.MapPost("/", async (
            CreateUserRequest req, CurrentUser me, IUserService users) =>
        {
            try
            {
                var id = await users.CreateAsync(me, req);
                return Results.Created($"/api/v1/users/{id}", new { id });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateUserRequest req, CurrentUser me, IUserService users) =>
        {
            try
            {
                var ok = await users.UpdateAsync(me, id, req);
                return ok ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });
    }
}