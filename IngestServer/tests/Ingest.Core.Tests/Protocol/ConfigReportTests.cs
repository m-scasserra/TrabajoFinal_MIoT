using Ingest.Core.Protocol;
using Ingest.Core.Protocol.Messages;

namespace Ingest.Core.Tests.Protocol;

public class ConfigReportTests
{
    [Fact]
    public void Vector_TreePhaseModbus_DecodeGlobalStatusAndRegisters()
    {
        byte[] payload =
        [
            0x15,                       // header: v1, config_report (0x5)
            0x04,                       // config_version = 4
            0x9F, 0x3A,                 // config_hash = 0x9F3A
            0x01,                       // frag_info: index=0, total=1
            0x02,                       // mode = 2 (Modbus)
            0x0F,                       // phase_config = 0x0F (all phases)
            0x01,                       // slave_id = 1
            0x00,                       // bus_params: baud=0 (9600), parity=0 (None)
            0x03,                       // reg_count = 3
            0x03,                       // regs_in_frag = 3
            0x00, 0x00, 0x00, 0x0A, 0x03, 0x01, // reg_L1: addr=0, type=0, scale=0x0A, fn=holding, phase=L1
            0x00, 0x02, 0x00, 0x0A, 0x03, 0x02, // reg_L2: addr=2, type=0, scale=0x0A, fn=holding, phase=L2
            0x00, 0x04, 0x00, 0x0A, 0x03, 0x03, // reg_L3: addr=4, type=0, scale=0x0A, fn=holding, phase=L3
        ];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var report = ConfigReport.Decode(ref reader);

        Assert.Equal(MessageType.ConfigReport, header.Type);
        Assert.Equal(4, report.ConfigVersion);
        Assert.Equal(0x9F3A, report.ConfigHash);
        Assert.Equal(0, report.Fragment.Index);
        Assert.Equal(1, report.Fragment.Total);
        Assert.True(report.Fragment.IsSingleFragment);
        Assert.True(report.Fragment.IsLast);
        Assert.Equal(2, report.Mode);
        Assert.Equal(0x0F, report.PhaseConfig);
        Assert.Equal(1, report.SlaveId);
        Assert.Equal(0, report.BaudrateCode);
        Assert.Equal(0, report.Parity);
        Assert.Equal(3, report.RegCount);
        Assert.Equal(3, report.RegsInFrag);

        Assert.Equal(3, report.Registers.Count);
        Assert.Equal(0x0000, report.Registers[0].RegAddr);
        Assert.Equal(0x03, report.Registers[0].FnCode);
        Assert.Equal(0x01, report.Registers[0].PhaseTag);
        Assert.Equal(0x0002, report.Registers[1].RegAddr);
        Assert.Equal(0x03, report.Registers[1].FnCode);
        Assert.Equal(0x02, report.Registers[1].PhaseTag);
        Assert.Equal(0x0004, report.Registers[2].RegAddr);
        Assert.Equal(0x03, report.Registers[2].FnCode);
        Assert.Equal(0x03, report.Registers[2].PhaseTag);

        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void Vector_AnalogMode_NoRegisterJustGlobalState()
    {
        byte[] payload =
        [
            0x11,                       // header: v1, config_report (0x5)
            0x03,                       // config_version = 3
            0x1A, 0x2B,                 // config_hash = 0x1A2B
            0x01,                       // frag_info: index=0, total=1
            0x00,                       // mode = 0 (ATM90E32AS)
            0x02,                       // phase_config = 0x02 (1 phase L1)
            0x00,                       // slave_id = 0
            0x00,                       // bus_params = 0
            0x00,                       // reg_count = 0
            0x00,                       // regs_in_frag = 0
        ];

        var reader = new PayloadReader(payload);
        var header = Header.Parse(ref reader);
        var report = ConfigReport.Decode(ref reader);

        Assert.Equal(0, report.Mode);
        Assert.Equal(0, report.RegCount);
        Assert.Empty(report.Registers);
        Assert.Equal(0, reader.Remaining);
    }
}