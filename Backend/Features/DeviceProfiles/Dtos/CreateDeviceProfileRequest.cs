using System.ComponentModel.DataAnnotations;

namespace Backend.Features.DeviceProfiles.Dtos;

public record CreateDeviceProfileRequest(
    [property: Required, MaxLength(100)] string Name,
    [property: Required, MaxLength(100)] string Region,
    [property: Required] string MacVersion,
    [property: Required, MaxLength(20)] string RegParamsRevision,
    string? RegionConfigId,
    string? AdrAlgorithmId,
    [property: Required, Range(1, 86400)] int UplinkInterval,
    [property: Required, Range(1, 1000)] int DeviceStatusReqInterval,
    bool SupportsOtaa,
    bool FlushQueueOnActivate,
    bool AutoDetectMeasurements,
    AppLayerParams? AppLayerParams
);