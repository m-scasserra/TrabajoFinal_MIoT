namespace Backend.Features.DeviceProfiles.Dtos;

public record DeviceProfileDto(
    Guid Id,
    Guid? ChirpStackId,
    string Name,
    string Region,
    string MacVersion,
    string RegParamsRevision,
    string? RegionConfigId,
    string AdrAlgorithmId,
    int UplinkInterval,
    int DeviceStatusReqInterval,
    bool SupportsOtaa,
    bool FlushQueueOnActivate,
    bool AutoDetectMeasurements,
    AppLayerParams? AppLayerParams,
    string SyncStatus,
    string? SyncError,
    DateTime CreatedAt
);