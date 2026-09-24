namespace Ingest.Infrastructure.Mqtt;

public interface IMqttPublisher
{
    Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken ct = default);
}