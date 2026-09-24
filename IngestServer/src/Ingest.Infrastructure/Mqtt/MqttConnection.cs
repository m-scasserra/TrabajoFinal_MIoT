using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Ingest.Infrastructure.Mqtt;

public sealed class MqttConnection : IMqttPublisher, IAsyncDisposable
{
    private readonly IMqttClient _client;
    private readonly MqttConnectionOptions _options;
    private readonly ILogger _logger;

    private readonly ConcurrentDictionary<string, byte> _subscriptions = new();
    private Func<string, ReadOnlyMemory<byte>, Task>? _messageHandler;

    private readonly CancellationTokenSource _cts = new();
    private Task? _supervisor;

    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan SuperviseInterval = TimeSpan.FromSeconds(2);

    public MqttConnection(MqttConnectionOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += OnMessageReceived;

        _client.DisconnectedAsync += args =>
        {
            _logger.LogWarning("MQTT client disconnected from {Host}: {Reasion}.",
                _options.Host, args.Reason);
            return Task.CompletedTask;
        };
    }

    public void SetMessageHandler(Func<string, ReadOnlyMemory<byte>, Task> handler)
    {
        _messageHandler = handler;
    }

    public async Task SubscribeAsync(string topic)
    {
        _subscriptions[topic] = 0;

        if (_client.IsConnected)
        {
            await ApplySubscriptionsAsync(topic);
        }
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _supervisor = Task.Run(() => SuperviseAsync(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task PublishAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (!_client.IsConnected)
        {
            _logger.LogWarning(
                "MQTT client is not connected. Cannot publish to topic {Topic}.",
                topic
            );
            return;
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload.ToArray())
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, ct);
    }

    private async Task SuperviseAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                {
                    await ConnectAndSubscribeAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "An error occurred while supervising the MQTT connection to {Host}; retrying in {ReconnectDelay} seconds.",
                    _options.Host, ReconnectDelay.TotalSeconds);
                await DelaySafe(ReconnectDelay, ct);
                continue;
            }

            await DelaySafe(SuperviseInterval, ct);
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        var clientOptions = new MqttClientOptions();
        await _client.ConnectAsync(clientOptions, ct);
        _logger.LogInformation(
            "Successfully connected to MQTT broker at {Host}:{Port}.",
            _options.Host, _options.Port);

        foreach (var topic in _subscriptions.Keys)
        {
            await ApplySubscriptionsAsync(topic);
        }
    }

    private async Task ApplySubscriptionsAsync(string topic)
    {
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();

        await _client.SubscribeAsync(options);
        _logger.LogInformation(
            "Successfully subscribed to MQTT topic {Topic} (QoS 1).",
            topic);
    }

    private static async Task DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); }
        catch (OperationCanceledException) { }
    }

    private MqttClientOptions BuildClientOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithClientId(_options.ClientId ?? $"ingest-{Guid.NewGuid():N}");

        if (_options.UseTls)
        {
            var tls = new MqttClientTlsOptionsBuilder().UseTls(true);

            var certs = LoadCertificates();
            if (certs is not null)
            {
                tls = tls.WithClientCertificates(certs);
            }

            if (_options.AllowUntrustedServerCert)
            {
                tls = tls.WithAllowUntrustedCertificates(true)
                         .WithCertificateValidationHandler(_ => true);
            }

            builder = builder.WithTlsOptions(tls.Build());
        }

        return builder.Build();
    }

    private X509Certificate2Collection? LoadCertificates()
    {
        if (string.IsNullOrEmpty(_options.ClientCertPath))
        {
            return null;
        }

        var clientCert = X509Certificate2.CreateFromPemFile(
            _options.ClientCertPath, _options.ClientKeyPath);

        var collection = new X509Certificate2Collection();
        if (!string.IsNullOrEmpty(_options.CaCertPath))
        {
            collection.Add(X509CertificateLoader.LoadCertificateFromFile(_options.CaCertPath));
        }
        collection.Add(clientCert);

        return collection;
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        if (_messageHandler is null)
        {
            return;
        }

        string topic = args.ApplicationMessage.Topic;
        ReadOnlyMemory<byte> payload = args.ApplicationMessage.Payload.ToArray();

        try
        {
            await _messageHandler(topic, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MQTT message for topic {Topic}", topic);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        if (_supervisor is not null)
        {
            try { await _supervisor; } catch { }
        }

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }
        _client.Dispose();
        _cts.Dispose();
    }
}