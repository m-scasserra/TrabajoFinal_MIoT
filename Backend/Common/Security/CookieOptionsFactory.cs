namespace Backend.Common.Security;

public static class CookieOptionsFactory
{
    public const string RefreshCookieName  = "refreshToken";

    public static CookieOptions CreateRefreshCookie(IWebHostEnvironment env) => new()
    {
        HttpOnly = true,
        Secure = env.IsProduction(),
        SameSite = SameSiteMode.Lax,
        Path = "/api/v1/auth",
        Expires = DateTime.UtcNow.AddDays(7)
    };
}