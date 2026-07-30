using Backend.Common.Security;
using Backend.Features.Auth.Dtos;

namespace Backend.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
            LoginRequest req, IAuthService auth, HttpContext ctx, IWebHostEnvironment env) =>
        {
            var (ip, ua) = GetClientInfo(ctx);
            var result = await auth.LoginAsync(req, ip, ua);
            if (result is null) return Results.Unauthorized();

            AppendRefreshCookie(ctx, env, result.RawRefreshToken);
            return Results.Ok(new AuthResponse(result.AccessToken, result.User));
        });

        group.MapPost("/refresh", async (
            IAuthService auth, HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (!ctx.Request.Cookies.TryGetValue(CookieOptionsFactory.RefreshCookieName, out var raw))
                return Results.Unauthorized();

            var (ip, ua) = GetClientInfo(ctx);
            var result = await auth.RefreshAsync(raw, ip, ua);
            if (result is null)
            {
                ctx.Response.Cookies.Delete(
                    CookieOptionsFactory.RefreshCookieName,
                    CookieOptionsFactory.CreateRefreshCookie(env));
                return Results.Unauthorized();
            }

            AppendRefreshCookie(ctx, env, result.RawRefreshToken);
            return Results.Ok(new { accessToken = result.AccessToken });
        });

        group.MapPost("/logout", async (
            IAuthService auth, HttpContext ctx, IWebHostEnvironment env) =>
        {
            if (ctx.Request.Cookies.TryGetValue(CookieOptionsFactory.RefreshCookieName, out var raw))
                await auth.LogoutAsync(raw);

            ctx.Response.Cookies.Delete(
                CookieOptionsFactory.RefreshCookieName,
                CookieOptionsFactory.CreateRefreshCookie(env));

            return Results.Ok(new { message = "Logged out successfully" });
        });

        group.MapPost("/confirm-account", async (
            ConfirmAccountRequest req, IAuthService auth) =>
        {
            var ok = await auth.ConfirmAccountAsync(req.Token, req.Password);
            return ok
                ? Results.Ok(new { message = "Account confirmed successfully. You can now log in." })
                : Results.BadRequest(new { message = "Invalid or expired token." });
        });

        group.MapGet("/me", async (CurrentUser me, IAuthService auth) =>
        {
            var user = await auth.GetCurrentAsync(me.UserId);
            return user is null ? Results.NotFound() : Results.Ok(user);
        })
        .RequireAuthorization();
    }

    private static (string? ip, string? userAgent) GetClientInfo(HttpContext ctx) =>
        (ctx.Connection.RemoteIpAddress?.ToString(),
        ctx.Request.Headers.UserAgent.ToString());

    private static void AppendRefreshCookie(HttpContext ctx, IWebHostEnvironment env, string token) =>
        ctx.Response.Cookies.Append(
            CookieOptionsFactory.RefreshCookieName,
            token,
            CookieOptionsFactory.CreateRefreshCookie(env));
}