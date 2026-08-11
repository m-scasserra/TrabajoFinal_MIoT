namespace Backend.Common.ChirpStack;

public sealed record DeviceProfileData(
    string Name, string Region, string MacVersion, string RegParamsRevision,
    string? RegionConfigId, string AdrAlgorithmId, int UplinkInterval,
    int DeviceStatusReqInterval, bool SupportsOtaa, bool FlushQueueOnActivate,
    bool AutoDetectMeasurements, int? Ts003FPort, int? Ts004FPort, int? Ts005FPort);