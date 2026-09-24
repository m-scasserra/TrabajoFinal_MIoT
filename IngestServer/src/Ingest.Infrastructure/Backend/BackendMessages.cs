using Ingest.Core.Protocol;

namespace Ingest.Infrastructure.Backend;

public abstract record BackendEnvelope
{
    public required Guid NodeId { get; init; }
    public required string DevEui { get; init; }
    public Guid? OrgId { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
    public required uint FrameCounter { get; init; }
    public double? Rssi { get; init; }
    public double? Snr { get; init; }
}

public sealed record AlarmEnvelope : BackendEnvelope
{
    public required byte ConfigVersion { get; init; }
    public required ushort SeqNumber { get; init; }
    public required string EventCode { get; init; }
    public required byte RawEventCode { get; init; }
    public required string Phase { get; init; }
    public required byte RawPhase { get; init; }
    public required ushort Value { get; init; }
}

public sealed record ConfigAckEnvelope : BackendEnvelope
{
    public required byte CmdId { get; init; }
    public required string ResultCode { get; init; }
    public required byte RawResultCode { get; init; }
    public required byte ConfigVersion { get; init; }
    public required ushort ConfigHash { get; init; }
}

public sealed record ConfigReportEnvelope : BackendEnvelope
{
    public required byte ConfigVersion { get; init; }
    public required ushort ConfigHash { get; init; }
    public required byte FragIndex { get; init; }
    public required byte FragTotal { get; init; }
    public required byte Mode { get; init; }
    public required byte PhaseConfig { get; init; }
    public required byte SlaveId { get; init; }
    public required byte BaudrateCode { get; init; }
    public required byte Parity { get; init; }
    public required byte RegCount { get; init; }
    public required byte RegsInFrag { get; init; }
    public required IReadOnlyList<RegisterEntryDto> Registers { get; init; }
}

public sealed record RegisterEntryDto(
    ushort RegAddr, byte RegType, byte Scale, byte FnCode, byte PhaseTag);

public sealed record DesyncEnvelope : BackendEnvelope
{
    public required byte PayloadConfigVersion { get; init; }
    public required byte CurrentConfigVersion { get; init; }
}