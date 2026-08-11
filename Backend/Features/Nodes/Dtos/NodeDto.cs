namespace Backend.Features.Nodes.Dtos;

public record NodeDto(
    Guid Id,
    string DevEui,
    string MacAddress,
    Guid? OrgId,
    string Alias,
    string? MeterType,
    string OperativeState,
    string SyncStatus,
    string? SyncError,
    int HwRevision,
    int FwRevision,
    double? Latitude,
    double? Longitude,
    DateTime CreatedAt);