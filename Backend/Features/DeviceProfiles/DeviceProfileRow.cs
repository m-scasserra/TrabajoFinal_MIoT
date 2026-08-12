namespace Backend.Features.DeviceProfiles;

internal sealed record DeviceProfileRow
{
    public Guid Id { get; init; }
    public Guid? ChirpStackId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string MacVersion { get; init; } = string.Empty;
    public string RegParamsRevision { get; init; } = string.Empty;
    public string? RegionConfigId { get; init; }
    public string AdrAlgorithmId { get; init; } = string.Empty;
    public int UplinkInterval { get; init; }
    public int DeviceStatusReqInterval { get; init; }
    public bool SupportsOtaa { get; init; }
    public bool FlushQueueOnActivate { get; init; }
    public bool AutoDetectMeasurements { get; init; }
    public string? AppLayerParamsJson { get; init; }
    public string SyncStatus { get; init; } = string.Empty;
    public string? SyncError { get; init; }
    public DateTime CreatedAt { get; init; }
}