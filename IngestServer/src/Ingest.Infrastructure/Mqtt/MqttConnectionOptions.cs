namespace Ingest.Infrastructure.Mqtt;

public sealed class MqttConnectionOptions
{
    public required string Host { get; init; }
    public int Port { get; init; } = 8883;
    public string? ClientId { get; init; }

    public bool UseTls { get; init; } = true;

    public string? Username { get; init; }
    public string? Password { get; init; }

    public string? CaCertPath { get; init; }
    public string? ClientCertPath { get; init; }
    public string? ClientKeyPath { get; init; }

    public bool AllowUntrustedServerCert { get; init; }
}