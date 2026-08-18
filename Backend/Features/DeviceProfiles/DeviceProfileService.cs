using Backend.Common.ChirpStack;
using Backend.Features.DeviceProfiles.Dtos;
using Dapper;
using Npgsql;
using System.Text.Json;

namespace Backend.Features.DeviceProfiles;

public sealed class DeviceProfileService(
    NpgsqlConnection db,
    IChirpStackClient chirpstack,
    ILogger<DeviceProfileService> logger) : IDeviceProfileService
{
    private const string SelectBase = """
        SELECT id AS Id,
               chirpstack_id AS ChirpStackId,
               name AS Name,
               region AS Region,
               mac_version AS MacVersion,
               reg_params_revision AS RegParamsRevision,
               region_config_id AS RegionConfigId,
               adr_algorithm_id AS AdrAlgorithmId,
               uplink_interval AS UplinkInterval,
               device_status_req_interval AS DeviceStatusReqInterval,
               supports_otaa AS SupportsOtaa,
               flush_queue_on_activate AS FlushQueueOnActivate,
               auto_detect_measurements AS AutoDetectMeasurements,
               app_layer_params::text AS AppLayerParamsJson,
               sync_status AS SyncStatus,
               sync_error AS SyncError,
               created_at AS CreatedAt
        FROM general.device_profiles
        """;

    public async Task<IEnumerable<DeviceProfileDto>> ListAsync()
    {
        var rows = await db.QueryAsync<DeviceProfileRow>(SelectBase + " ORDER BY name");
        return rows.Select(MapRow);
    }

    public async Task<DeviceProfileDto?> GetAsync(Guid id)
    {
        var row = await db.QuerySingleOrDefaultAsync<DeviceProfileRow>(
            $"{SelectBase} WHERE id = @Id", new { Id = id });
        return row is null ? null : MapRow(row);
    }

    public async Task<DeviceProfileDto> CreateAsync(CreateDeviceProfileRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var appLayerJson = req.AppLayerParams is null
            ? null
            : JsonSerializer.Serialize(req.AppLayerParams, JsonOpts);

        await db.ExecuteAsync(
            """
            INSERT INTO general.device_profiles
                (id, name, region, mac_version, reg_params_revision, region_config_id,
                 adr_algorithm_id, uplink_interval, device_status_req_interval,
                 supports_otaa, flush_queue_on_activate, auto_detect_measurements,
                 app_layer_params, sync_status)
            VALUES
                (@Id, @Name, @Region, @MacVersion::general.lorawan_mac_version, @RegParamsRevision,
                 @RegionConfigId, @AdrAlgorithmId, @UplinkInterval, @DeviceStatusReqInterval,
                 @SupportsOtta, @FlushQueueOnActivate, @AutoDetectMeasurements,
                 @AppLayerJson::jsonb, 'PENDING');
            """,
            new
            {
                Id = id,
                Name = req.Name,
                Region = req.Region,
                MacVersion = req.MacVersion,
                RegParamsRevision = req.RegParamsRevision,
                RegionConfigId = req.RegionConfigId,
                AdrAlgorithmId = req.AdrAlgorithmId ?? "default",
                UplinkInterval = req.UplinkInterval,
                DeviceStatusReqInterval = req.DeviceStatusReqInterval,
                SupportsOtta = req.SupportsOtaa,
                FlushQueueOnActivate = req.FlushQueueOnActivate,
                AutoDetectMeasurements = req.AutoDetectMeasurements,
                AppLayerJson = appLayerJson
            });

        await TrySyncCreateAsync(id, req, ct);
        return (await GetAsync(id))!;
    }

    private async Task TrySyncCreateAsync(Guid id, CreateDeviceProfileRequest req, CancellationToken ct)
    {
        try
        {
            var data = ToData(req);
            var chirpstackId = await chirpstack.CreateDeviceProfileAsync(data, ct);

            await db.ExecuteAsync(
                """
                UPDATE general.device_profiles
                SET chirpstack_id = @CsId::uuid, sync_status = 'SYNCED', sync_error = NULL
                WHERE id = @Id;
                """,
                new { CsId = chirpstackId, Id = id });

            logger.LogInformation("Device profile {Id} synced to ChirpStack with ID {CsId}", id, chirpstackId);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.device_profiles SET sync_status = 'FAILED', sync_error = @Error WHERE id = @Id",
                new { Error = ex.Message, Id = id });
            logger.LogWarning(ex, "Failed to sync device profile {Id} to ChirpStack", id);
        }
    }

    public async Task<DeviceProfileDto?> UpdateAsync(Guid id, CreateDeviceProfileRequest req, CancellationToken ct = default)
    {
        var existing = await db.QuerySingleOrDefaultAsync<(Guid Id, Guid? CsId)>(
            "SELECT id AS Id, chirpstack_id AS CsId FROM general.device_profiles WHERE id = @Id",
            new { Id = id });

        if (existing.Id == Guid.Empty)
            return null;

        var appLayerJson = req.AppLayerParams is null
            ? null
            : JsonSerializer.Serialize(req.AppLayerParams, JsonOpts);

        await db.ExecuteAsync(
            """
            UPDATE general.device_profiles
            SET  name = @Name, region = @Region, mac_version = @MacVersion::general.lorawan_mac_version,
                 reg_params_revision = @RegParamsRevision, region_config_id = @RegionConfigId,
                 adr_algorithm_id = @AdrAlgorithmId, uplink_interval = @UplinkInterval,
                 device_status_req_interval = @DeviceStatusReqInterval, supports_otaa = @SupportsOtta,
                 flush_queue_on_activate = @FlushQueueOnActivate, auto_detect_measurements = @AutoDetectMeasurements,
                 app_layer_params = @AppLayerJson::jsonb, sync_status = 'PENDING'
            WHERE id = @Id;
            """,
            new
            {
                Id = id,
                Name = req.Name,
                Region = req.Region,
                MacVersion = req.MacVersion,
                RegParamsRevision = req.RegParamsRevision,
                RegionConfigId = req.RegionConfigId,
                AdrAlgorithmId = req.AdrAlgorithmId ?? "default",
                UplinkInterval = req.UplinkInterval,
                DeviceStatusReqInterval = req.DeviceStatusReqInterval,
                SupportsOtta = req.SupportsOtaa,
                FlushQueueOnActivate = req.FlushQueueOnActivate,
                AutoDetectMeasurements = req.AutoDetectMeasurements,
                AppLayerJson = appLayerJson
            });

        await TrySyncUpdateAsync(id, existing.CsId, req, ct);
        return (await GetAsync(id))!;
    }

    private async Task TrySyncUpdateAsync(Guid id, Guid? chirpstackId, CreateDeviceProfileRequest req, CancellationToken ct)
    {
        try
        {
            if (chirpstackId is null)
            {
                await TrySyncCreateAsync(id, req, ct);
                return;
            }

            await chirpstack.UpdateDeviceProfileAsync(chirpstackId.Value.ToString(), ToData(req), ct);
            await db.ExecuteAsync(
                "UPDATE general.device_profiles SET sync_status = 'SYNCED', sync_error = NULL WHERE id = @Id",
                new { Id = id });
            logger.LogInformation("Device profile {Id} updated successfully in ChirpStack.", id);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.device_profiles SET sync_status = 'FAILED', sync_error = @Error WHERE id = @Id",
                new { Error = ex.Message, Id = id });
            logger.LogWarning(ex, "Failed to update device profile {Id} in ChirpStack.", id);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<(Guid Id, Guid? CsId)>(
            "SELECT id AS Id, chirpstack_id AS CsId FROM general.device_profiles WHERE id = @Id",
            new { Id = id });

        if (row.Id == Guid.Empty)
            return false;

        var inUse = await db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM general.nodes WHERE device_profile_id = @Id)",
            new { Id = id });

        if (inUse)
            throw new InvalidOperationException(
                "Cannot delete device profile that is in use by nodes.");

        await db.ExecuteAsync(
            "UPDATE general.device_profiles SET sync_status = 'PENDING_DELETE' WHERE id = @Id",
            new { Id = id });

        await TryDeleteAsync(id, row.CsId, ct);
        return true;
    }

    private async Task TryDeleteAsync(Guid id, Guid? chirpstackId, CancellationToken ct)
    {
        try
        {
            if (chirpstackId is not null)
                await chirpstack.DeleteDeviceProfileAsync(chirpstackId.Value.ToString(), ct);

            await db.ExecuteAsync("DELETE FROM general.device_profiles WHERE id = @Id", new { Id = id });
            logger.LogInformation("Device profile {Id} deleted successfully in ChirpStack.", id);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.device_profiles SET sync_status = 'DELETE_FAILED', sync_error = @Error WHERE id = @Id",
                new { Error = ex.Message, Id = id });
            logger.LogWarning(ex, "Failed to delete device profile {Id} in ChirpStack.", id);
        }
    }

    public async Task<int> ReconcilePendingAsync(CancellationToken ct = default)
    {
        var count = 0;

        var toSync = await db.QueryAsync<DeviceProfileRow>(
            $"{SelectBase} WHERE sync_status IN ('PENDING', 'FAILED')");

        foreach (var row in toSync)
        {
            ct.ThrowIfCancellationRequested();
            var dto = MapRow(row);
            var req = ToRequest(dto);

            if (dto.ChirpStackId is not null && await chirpstack.DeviceProfileExistsAsync(dto.ChirpStackId.Value.ToString(), ct))
                await TrySyncUpdateAsync(dto.Id, dto.ChirpStackId, req, ct);
            else
                await TrySyncCreateAsync(dto.Id, req, ct);
            count++;
        }

        var toDelete = await db.QueryAsync<(Guid Id, Guid? CsId)>(
            "SELECT id AS Id, chirpstack_id AS CsId FROM general.device_profiles WHERE sync_status IN ('PENDING_DELETE', 'DELETE_FAILED')");

        foreach (var (pid, csid) in toDelete)
        {
            ct.ThrowIfCancellationRequested();
            await TryDeleteAsync(pid, csid, ct);
            count++;
        }

        if (count > 0)
            logger.LogInformation("Reconciled {Count} device profiles with ChirpStack.", count);
        return count;
    }

    private static DeviceProfileData ToData(CreateDeviceProfileRequest r) => new(
        r.Name, r.Region, r.MacVersion, r.RegParamsRevision, r.RegionConfigId,
        r.AdrAlgorithmId ?? "default", r.UplinkInterval, r.DeviceStatusReqInterval,
        r.SupportsOtaa, r.FlushQueueOnActivate, r.AutoDetectMeasurements,
        r.AppLayerParams?.Ts003FPort, r.AppLayerParams?.Ts004FPort, r.AppLayerParams?.Ts005FPort);

    private static DeviceProfileDto MapRow(DeviceProfileRow row)
    {
        AppLayerParams? alp = string.IsNullOrEmpty(row.AppLayerParamsJson)
            ? null
            : JsonSerializer.Deserialize<AppLayerParams>(
                row.AppLayerParamsJson,
                JsonOpts);

        return new DeviceProfileDto(
            row.Id, row.ChirpStackId, row.Name, row.Region, row.MacVersion,
            row.RegParamsRevision, row.RegionConfigId, row.AdrAlgorithmId,
            row.UplinkInterval, row.DeviceStatusReqInterval, row.SupportsOtaa,
            row.FlushQueueOnActivate, row.AutoDetectMeasurements, alp,
            row.SyncStatus, row.SyncError, row.CreatedAt);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static CreateDeviceProfileRequest ToRequest(DeviceProfileDto d) => new(
        d.Name, d.Region, d.MacVersion, d.RegParamsRevision, d.RegionConfigId,
        d.AdrAlgorithmId, d.UplinkInterval, d.DeviceStatusReqInterval,
        d.SupportsOtaa, d.FlushQueueOnActivate, d.AutoDetectMeasurements, d.AppLayerParams);
}