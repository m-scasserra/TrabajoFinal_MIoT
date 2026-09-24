using System.Text.Json;
using Ingest.Application.Nodes;
using Ingest.Application.Output;
using Ingest.Application.Pipeline;
using Ingest.Core.Decoding;
using Ingest.Infrastructure.Mqtt;

namespace Ingest.Infrastructure.Backend;

public sealed class MqttBackendPublisher : IBackendPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IMqttPublisher _mqtt;
    private readonly BackendTopicOptions _topics;

    public MqttBackendPublisher(IMqttPublisher mqtt, BackendTopicOptions topics)
    {
        _mqtt = mqtt;
        _topics = topics;
    }

    public Task PublishAlarmAsync(
        CachedNode node, DecodedMessage.AlarmMessage alarm, UplinkContext ctx, CancellationToken ct = default)
    {
        var dto = BackendMessageMapper.ToAlarm(node, alarm, ctx);
        return PublishAsync(_topics.ResolveAlarm(node.DevEui), dto, ct);
    }

    public Task PublishConfigAckAsync(
        CachedNode node, DecodedMessage.ConfigAckMessage ack, UplinkContext ctx, CancellationToken ct = default)
    {
        var dto = BackendMessageMapper.ToConfigAck(node, ack, ctx);
        return PublishAsync(_topics.ResolveConfigAck(node.DevEui), dto, ct);
    }

    public Task PublishConfigReportAsync(
        CachedNode node, DecodedMessage.ConfigReportMessage report, UplinkContext ctx, CancellationToken ct = default)
    {
        var dto = BackendMessageMapper.ToConfigReport(node, report, ctx);
        return PublishAsync(_topics.ResolveConfigReport(node.DevEui), dto, ct);
    }

    public Task PublishDesyncAsync(
        CachedNode node, byte payloadConfigVersion, UplinkContext ctx, CancellationToken ct = default)
    {
        var dto = BackendMessageMapper.ToDesync(node, payloadConfigVersion, ctx);
        return PublishAsync(_topics.ResolveDesync(node.DevEui), dto, ct);
    }

    private Task PublishAsync<T>(string topic, T dto, CancellationToken ct)
    {
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);
        return _mqtt.PublishAsync(topic, json, ct);
    }
}