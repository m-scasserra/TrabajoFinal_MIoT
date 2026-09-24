using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Config;
using Ingest.Core.Decoding;
using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;
using Ingest.Infrastructure.Backend;

namespace Ingest.Infrastructure.Tests.Backend;

public class BackendMessageMapperTests
{
    private static CachedNode Node() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        DevEui = "AABBCCDD00112233",
        Alias = "node",
        OrgId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        OperativeState = NodeState.Active,
        Config = new NodeConfig { Schema = 1, Mode = AcquisitionMode.Modbus, ConfigVersion = 8 },
    };

    private static UplinkContext Ctx() => new()
    {
        DevEui = "AABBCCDD00112233",
        Payload = [],
        FrameCounter = 42,
        ReceivedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
        Rssi = -95.5,
        Snr = 7.2,
    };

    [Fact]
    public void ToAlarm_MapsFieldAndEnums()
    {
        var alarm = new Alarm(
            ConfigVersion: 4, SeqNumber: 300,
            Event: AlarmEventCode.Overvoltage, RawEventCode: 0x01,
            Phase: PhaseTag.L2, RawPhase: 0x02, Value: 2300);
        var msg = new DecodedMessage.AlarmMessage(alarm) { ProtocolVersion = 1 };

        var dto = BackendMessageMapper.ToAlarm(Node(), msg, Ctx());

        Assert.Equal("AABBCCDD00112233", dto.DevEui);
        Assert.Equal(42u, dto.FrameCounter);
        Assert.Equal(-95.5, dto.Rssi);
        Assert.Equal("Overvoltage", dto.EventCode);
        Assert.Equal(0x01, dto.RawEventCode);
        Assert.Equal("L2", dto.Phase);
        Assert.Equal(2300, dto.Value);
    }

    [Fact]
    public void ToDesync_IncludesBothVersions()
    {
        var dto = BackendMessageMapper.ToDesync(Node(), payloadVersion: 7, Ctx());

        Assert.Equal(7, dto.PayloadConfigVersion);
        Assert.Equal(8, dto.CurrentConfigVersion);
    }

    [Fact]
    public void ToConfigReport_MapsRegisters()
    {
        var report = new ConfigReport(
            ConfigVersion: 4, ConfigHash: 0x9F3A,
            Fragment: new FragInfo(0, 1),
            Mode: 2, PhaseConfig: 0x0F, SlaveId: 1, BaudrateCode: 0, Parity: 0,
            RegCount: 2, RegsInFrag: 2,
            Registers:
            [
                new RegisterEntry(0x0000, 0x00, 0x0A, 0x03, 0x01),
                new RegisterEntry(0x0002, 0x00, 0x0A, 0x03, 0x02)
            ]);
        var msg = new DecodedMessage.ConfigReportMessage(report) { ProtocolVersion = 1 };

        var dto = BackendMessageMapper.ToConfigReport(Node(), msg, Ctx());

        Assert.Equal(2, dto.Registers.Count);
        Assert.Equal(0x0000, dto.Registers[0].RegAddr);
        Assert.Equal(0x03, dto.Registers[0].FnCode);
        Assert.Equal(0x01, dto.Registers[0].PhaseTag);
        Assert.Equal(0, dto.FragIndex);
        Assert.Equal(1, dto.FragTotal);
    }
}