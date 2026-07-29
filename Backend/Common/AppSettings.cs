namespace Backend.Common;

public sealed class AppSettings
{
    public string FrontendBaseUrl { get; init; } = string.Empty;
    public int InvitationTokenHours { get; init; } = 24;
}