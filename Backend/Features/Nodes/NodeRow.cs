namespace Backend.Features.Nodes;

internal sealed record NodeRow
{
    public Guid Id { get; init; }
    public string DevEui { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public Guid? OrgId { get; init; }
    public string Alias { get; init; } = string.Empty;
    public string? MeterType { get; init; }
    public string OperativeState { get; init; } = string.Empty;
    public string SyncStatus { get; init; } = string.Empty;
    public string? SyncError { get; init; }
    public short HwRevision { get; init; }
    public short FwRevision { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime CreatedAt { get; init; }
}