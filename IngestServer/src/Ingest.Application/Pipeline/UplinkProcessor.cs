using Ingest.Application.Nodes;
using Ingest.Application.Output;
using Ingest.Core.Decoding;
using Microsoft.Extensions.Logging;

namespace Ingest.Application.Pipeline;

public sealed class UplinkProcessor
{
    private readonly INodeCache _cache;
    private readonly IDeduplicationStore _dedup;
    private readonly IMeasurementWriter _measurementWriter;
    private readonly IHealthWriter _healthWriter;
    private readonly IBackendPublisher _backend;
    private readonly ILogger<UplinkProcessor> _logger;

    public UplinkProcessor(
        INodeCache cache,
        IDeduplicationStore dedup,
        IMeasurementWriter measurementWriter,
        IHealthWriter healthWriter,
        IBackendPublisher backend,
        ILogger<UplinkProcessor> logger)
    {
        _cache = cache;
        _dedup = dedup;
        _measurementWriter = measurementWriter;
        _healthWriter = healthWriter;
        _backend = backend;
        _logger = logger;
    }

    public async Task<ProcessOutcome> ProcessAsync(UplinkContext ctx, CancellationToken ct = default)
    {
        if (!_dedup.TryRegister(ctx.DevEui, ctx.FrameCounter))
        {
            _logger.LogDebug(
                "Duplicate frame detected for DevEui: {DevEui}, FrameCounter: {FrameCounter}",
                 ctx.DevEui, ctx.FrameCounter);
            return ProcessOutcome.Duplicate;
        }

        var node = await _cache.GetAsync(ctx.DevEui, ct);
        if (node is null)
        {
            _logger.LogWarning(
                "Node not found for DevEui: {DevEui}",
                ctx.DevEui);
            return ProcessOutcome.UnknownDevice;
        }

        var result = PayloadDecoder.Decode(ctx.Payload, node.Config);
        if (!result.IsSuccess)
        {
            return HandleDecodeFailure(node, result);
        }

        return await RouteAsync(node, result.Message!, ctx, ct);
    }

    private ProcessOutcome HandleDecodeFailure(CachedNode node, DecodeResult result)
    {
        switch (result.Error)
        {
            case DecodeError.UnsupportedMessageType:
                _logger.LogWarning(
                    "Unsupported message type for DevEui: {DevEui} Detail: {Detail}",
                    node.DevEui, result.ErrorDetail);
                return ProcessOutcome.Unsupported;

            default:
                _logger.LogWarning(
                    "Failed to decode message for DevEui: {DevEui} Error: {Error} Detail: {Detail}",
                    node.DevEui, result.Error, result.ErrorDetail);
                return ProcessOutcome.Malformed;

        }
    }

    private async Task<ProcessOutcome> RouteAsync(
        CachedNode node, DecodedMessage message, UplinkContext ctx, CancellationToken ct = default)
    {
        switch (message)
        {
            case DecodedMessage.MeasurementMessage m:
                return await HandleMeasurementAsync(node, m, ctx, ct);

            case DecodedMessage.HeartbeatMessage h:
                await _healthWriter.WriteHeartbeatAsync(node, h, ctx, ct);
                return ProcessOutcome.Processed;

            case DecodedMessage.AlarmMessage a:
                await _backend.PublishAlarmAsync(node, a, ctx, ct);
                return ProcessOutcome.Processed;

            case DecodedMessage.ConfigAckMessage ack:
                await _backend.PublishConfigAckAsync(node, ack, ctx, ct);
                return ProcessOutcome.Processed;

            case DecodedMessage.ConfigReportMessage rep:
                await _backend.PublishConfigReportAsync(node, rep, ctx, ct);
                return ProcessOutcome.Processed;

            default:
                _logger.LogError(
                    "Unhandled message type for DevEui: {DevEui} Message: {Message}",
                    node.DevEui, message.GetType().Name);
                return ProcessOutcome.Unsupported;
        }
    }

    private async Task<ProcessOutcome> HandleMeasurementAsync(
        CachedNode node, DecodedMessage.MeasurementMessage m, UplinkContext ctx, CancellationToken ct = default
    )
    {
        byte payloadVersion = m.Header.ConfigVersion;
        if (payloadVersion != node.Config.ConfigVersion)
        {
            _logger.LogInformation(
                "Desincronization detected for DevEui: {DevEui} PayloadVersion: {PayloadVersion} NodeConfigVersion: {NodeConfigVersion}",
                node.DevEui, payloadVersion, node.Config.ConfigVersion
            );

            await _backend.PublishDesyncAsync(node, payloadVersion, ctx, ct);
            return ProcessOutcome.Desynchronized;
        }

        await _measurementWriter.WriteAsync(node, m, ctx, ct);
        return ProcessOutcome.Processed;
    }
}