namespace Backend.Features.Gateways.Dtos;

public record GatewayDto(
    string GatewayEui,
    Guid? OrgId,
    string Alias,
    string Model,
    string OperativeState,
    string SyncStatus,
    string? SyncError,
    double? Latitude,
    double? Longitude
    DateTime? LastSeen,
    DateTime CreatedAt);