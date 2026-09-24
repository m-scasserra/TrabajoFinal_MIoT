using Ingest.Application.Nodes;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;

namespace Ingest.Infrastructure.Backend;

public static class BackendMessageMapper
{
    public static AlarmEnvelope ToAlarm(
        CachedNode node, DecodedMessage.AlarmMessage msg, UplinkContext ctx)
    {
        var a = msg.Alarm;
        return new AlarmEnvelope
        {
            NodeId = node.Id,
            DevEui = node.DevEui,
            OrgId = node.OrgId,
            ReceivedAt = ctx.ReceivedAt,
            FrameCounter = ctx.FrameCounter,
            Rssi = ctx.Rssi,
            Snr = ctx.Snr,
            ConfigVersion = a.ConfigVersion,
            SeqNumber = a.SeqNumber,
            EventCode = a.Event.ToString(),
            RawEventCode = a.RawEventCode,
            Phase = a.Phase.ToString(),
            RawPhase = a.RawPhase,
            Value = a.Value,
        };
    }

    public static ConfigAckEnvelope ToConfigAck(
        CachedNode node, DecodedMessage.ConfigAckMessage msg, UplinkContext ctx)
    {
        var ack = msg.ConfigAck;
        return new ConfigAckEnvelope
        {
            NodeId = node.Id,
            DevEui = node.DevEui,
            OrgId = node.OrgId,
            ReceivedAt = ctx.ReceivedAt,
            FrameCounter = ctx.FrameCounter,
            Rssi = ctx.Rssi,
            Snr = ctx.Snr,
            CmdId = ack.CmdId,
            ResultCode = ack.Result.ToString(),
            RawResultCode = ack.RawResultCode,
            ConfigVersion = ack.ConfigVersion,
            ConfigHash = ack.ConfigHash,
        };
    }

    public static ConfigReportEnvelope ToConfigReport(
        CachedNode node, DecodedMessage.ConfigReportMessage msg, UplinkContext ctx)
    {
        var r = msg.ConfigReport;
        return new ConfigReportEnvelope
        {
            NodeId = node.Id,
            DevEui = node.DevEui,
            OrgId = node.OrgId,
            ReceivedAt = ctx.ReceivedAt,
            FrameCounter = ctx.FrameCounter,
            Rssi = ctx.Rssi,
            Snr = ctx.Snr,
            ConfigVersion = r.ConfigVersion,
            ConfigHash = r.ConfigHash,
            FragIndex = r.Fragment.Index,
            FragTotal = r.Fragment.Total,
            Mode = r.Mode,
            PhaseConfig = r.PhaseConfig,
            SlaveId = r.SlaveId,
            BaudrateCode = r.BaudrateCode,
            Parity = r.Parity,
            RegCount = r.RegCount,
            RegsInFrag = r.RegsInFrag,
            Registers = r.Registers
                .Select(reg => new RegisterEntryDto(
                    reg.RegAddr, reg.RegType, reg.Scale, reg.FnCode, reg.PhaseTag))
                .ToList(),
        };
    }

    public static DesyncEnvelope ToDesync(
        CachedNode node, byte payloadVersion, UplinkContext ctx)
    {
        return new DesyncEnvelope
        {
            NodeId = node.Id,
            DevEui = node.DevEui,
            OrgId = node.OrgId,
            ReceivedAt = ctx.ReceivedAt,
            FrameCounter = ctx.FrameCounter,
            Rssi = ctx.Rssi,
            Snr = ctx.Snr,
            PayloadConfigVersion = payloadVersion,
            CurrentConfigVersion = node.Config.ConfigVersion,
        };
    }
}