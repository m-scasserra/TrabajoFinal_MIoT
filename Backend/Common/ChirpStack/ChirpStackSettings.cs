namespace Backend.Common.ChirpStack;

public sealed class ChirpStackSettings
{
    public string GrpcAddress { get; init; } = string.Empty;
    public string ApiToken { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}