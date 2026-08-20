namespace Ingest.Core.Protocol;


public enum MessageType : byte
{
    Heartbeat = 0x0,
    Measurement = 0x1,
    MeasurementBacklog = 0x2,
    Alarm = 0x3,
    ConfigAck = 0x4,
    ConfigReport = 0x5,
    CmdConfig = 0x8,
    CmdSetParam = 0x9,
    CmdQuery = 0xA,
    CmdControl = 0xB
}
