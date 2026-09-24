namespace Ingest.Infrastructure.Backend;

public sealed class BackendTopicOptions
{
    public string Alarm { get; init; } = "backend/alarm/{dev_eui}";
    public string ConfigAck { get; init; } = "backend/config_ack/{dev_eui}";
    public string ConfigReport { get; init; } = "backend/config_report/{dev_eui}";
    public string Desync { get; init; } = "backend/desync/{dev_eui}";

    public string ResolveAlarm(string deveui) => Alarm.Replace("{dev_eui}", deveui);
    public string ResolveConfigAck(string deveui) => ConfigAck.Replace("{dev_eui}", deveui);
    public string ResolveConfigReport(string deveui) => ConfigReport.Replace("{dev_eui}", deveui);
    public string ResolveDesync(string deveui) => Desync.Replace("{dev_eui}", deveui);
}