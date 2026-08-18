using Backend.Common.ChirpStack;
using Backend.Common.Security;
using Backend.Features.Nodes.Dtos;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Backend.Features.Nodes;

public sealed class NodeService(
    NpgsqlConnection db,
    IChirpStackClient chirpstack,
    IAppKeyCipher cipher,
    IOptions<ChirpStackSettings> csOptions,
    ILogger<NodeService> logger) : INodeService
{
    private readonly ChirpStackSettings _cs = csOptions.Value;
    private const string DefaultJoinEui = "0000000000000000";
    private const string SelectBase =
        """
        SELECT id AS Id, dev_eui as DevEui, mac_address AS MacAddress, org_id AS OrgId,
               alias AS Alias, meter_type AS MeterType, operative_state AS OperativeState,
               sync_status AS SyncStatus, sync_error AS SyncError,
               hw_revision AS HwRevision, fw_revision AS FwRevision,
               ST_Y(coordinates::geometry) AS Latitude,
               ST_X(coordinates::geometry) AS Longitude,
               created_at AS CreatedAt
        FROM general.nodes
        """;
    public async Task<IEnumerable<NodeDto>> ListAsync(CurrentUser me)
    {
        IEnumerable<NodeRow> rows;
        if (me.IsSuperAdmin)
            rows = await db.QueryAsync<NodeRow>(
                $"{SelectBase} WHERE org_id IS NOT NULL ORDER BY created_at DESC");

        else
            rows = await db.QueryAsync<NodeRow>(
                $"{SelectBase} WHERE org_id = @OrgId ORDER BY created_at DESC",
                new { OrgId = me.RequireOrgId() });

        return rows.Select(MapRow);
    }

    public async Task<NodeDto?> GetByEuiAsync(CurrentUser me, string devEui)
    {

        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";
        var row = await db.QuerySingleOrDefaultAsync<NodeRow>(
                $"{SelectBase} WHERE dev_eui = @Eui{whereOrg}",
                new { Eui = devEui.ToLowerInvariant(), OrgId = me.OrgId });
        return row is null ? null : MapRow(row);
    }

    public async Task<NodeDto> ActivateAsync(CurrentUser me, ActivateNodeRequest req, CancellationToken ct = default)
    {
        var orgId = me.RequireOrgId();
        var mac = req.MacAddress.ToLowerInvariant();

        var provisioned = await db.QuerySingleOrDefaultAsync<(Guid Id, string DevEui, byte[] AppKeyEnc)>(
            """
            SELECT id AS Id, dev_eui AS DevEui, app_key_encrypted AS AppKeyEnc
            FROM general.nodes
            WHERE mac_address = @Mac and org_id IS NULL
            """,
            new { Mac = mac });

        if (provisioned.Id == Guid.Empty)
            throw new InvalidOperationException(
                $"Node with MAC address {mac} is not provisioned or already activated.");

        var point = (req.Latitude.HasValue && req.Longitude.HasValue)
            ? $"SRID=4326;POINT({req.Longitude} {req.Latitude})"
            : null;

        await db.ExecuteAsync(
            """
            UPDATE general.nodes
            SET org_id = @OrgId,
                alias = @Alias,
                meter_type = @MeterType::general.node_type,
                freq_minutes = @FreqMinutes,
                device_profile_id = @DeviceProfileId::uuid,
                coordinates = CASE WHEN @Point IS NULL THEN NULL ELSE ST_GeomFromEWKT(@Point) END,
                sync_status = 'PENDING'
            WHERE id = @Id;
            """,
            new
            {
                OrgId = orgId,
                Alias = req.Alias,
                MeterType = req.MeterType,
                FreqMinutes = req.FreqMinutes,
                DeviceProfileId = req.DeviceProfileId,
                Point = point,
                Id = provisioned.Id
            });

        await TrySyncNodeAsync(provisioned.Id, provisioned.DevEui, req.Alias,
            req.DeviceProfileId, provisioned.AppKeyEnc, ct);

        return (await GetByEuiAsync(me, provisioned.DevEui))!;
    }

    private async Task TrySyncNodeAsync(
        Guid id, string devEui, string alias, Guid deviceProfileId,
        byte[] appKeyEncrypted, CancellationToken ct)
    {
        try
        {
            var appKeyHex = cipher.Decrypt(appKeyEncrypted);

            var chirpstackProfileId = await db.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT chirpstack_id FROM general.device_profiles WHERE id = @Id;",
                new { Id = deviceProfileId });

            if (chirpstackProfileId is null)
                throw new InvalidOperationException(
                    $"Device profile {deviceProfileId} does not exist or is not synced with ChirpStack.");

            await chirpstack.CreateDeviceAsync(
                devEui, DefaultJoinEui, alias, _cs.ApplicationId, chirpstackProfileId.Value.ToString(), appKeyHex, ct);

            var exists = await chirpstack.DeviceExistsAsync(devEui, ct);
            if (!exists)
                throw new InvalidOperationException("ChirpStack device creation failed.");

            await db.ExecuteAsync(
                "UPDATE general.nodes SET sync_status = 'SYNCED', synced_at = NOW(), sync_error = NULL WHERE id = @Id;",
                new { Id = id });

            logger.LogInformation("Node {DevEui} synced successfully with ChirpStack.", devEui);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.nodes SET sync_status = 'FAILED', sync_error = @Error WHERE id = @Id;",
                new { Id = id, Error = ex.Message });

            logger.LogWarning(ex, "Failed to sync node {DevEui} with ChirpStack.", devEui);
        }
    }

    public async Task<bool> DeleteAsync(CurrentUser me, Guid id, CancellationToken ct = default)
    {
        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";

        var devEui = await db.QuerySingleOrDefaultAsync<string>(
            $"SELECT dev_eui FROM general.nodes WHERE id = @Id{whereOrg}",
            new { Id = id, OrgId = me.OrgId });

        if (devEui is null)
            return false;

        await db.ExecuteAsync(
            $"UPDATE general.nodes SET sync_status = 'PENDING_DELETE' WHERE id = @Id{whereOrg}",
            new { Id = id, OrgId = me.OrgId });

        await TryDeleteNodeAsync(id, devEui, ct);
        return true;
    }

    private async Task TryDeleteNodeAsync(Guid id, string dev_eui, CancellationToken ct)
    {
        try
        {
            await chirpstack.DeleteDeviceAsync(dev_eui, ct);

            await db.ExecuteAsync(
                """
                UPDATE general.nodes
                SET org_id = NULL, alias = '', meter_type = NULL, freq_minutes = NULL,
                    device_profile_id = NULL, coordinates = NULL,
                    operative_state = 'INACTIVE', sync_status = 'PENDING',
                    sync_error = NULL, synced_at = NULL
                WHERE id = @Id;
                """,
                new { Id = id });

            logger.LogInformation("Node {DevEui} deleted successfully from ChirpStack.", dev_eui);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.nodes SET sync_status = 'DELETE_FAILED', sync_error = @Error WHERE id = @Id;",
                new { Id = id, Error = ex.Message });

            logger.LogWarning(ex, "Failed to delete node {DevEui} from ChirpStack.", dev_eui);
        }
    }

    public async Task<int> ReconcilePendingAsync(CancellationToken ct = default)
    {
        var count = 0;

        var toSync = await db.QueryAsync<(Guid Id, string DevEui, string Alias, Guid ProfileId, byte[] AppKeyEnc)>(
            """
            SELECT id as Id, dev_eui as DevEui, alias AS Alias,
                   device_profile_id AS ProfileId, app_key_encrypted AS AppKeyEnc
            FROM general.nodes
            WHERE org_id IS NOT NULL AND sync_status in ('PENDING', 'FAILED');
            """);

        foreach (var n in toSync)
        {
            ct.ThrowIfCancellationRequested();
            if (await chirpstack.DeviceExistsAsync(n.DevEui, ct))
                await db.ExecuteAsync(
                    "UPDATE general.nodes SET sync_status = 'SYNCED', synced_at = NOW(), sync_error = NULL WHERE id = @Id;",
                    new { Id = n.Id });
            else
                await TrySyncNodeAsync(n.Id, n.DevEui, n.Alias, n.ProfileId, n.AppKeyEnc, ct);
            count++;
        }

        var toDelete = await db.QueryAsync<(Guid Id, string DevEui)>(
            """
            SELECT id AS Id, dev_eui AS DevEui FROM general.nodes
            WHERE sync_status in ('PENDING_DELETE', 'DELETE_FAILED');
            """);

        foreach (var n in toDelete)
        {
            ct.ThrowIfCancellationRequested();
            await TryDeleteNodeAsync(n.Id, n.DevEui, ct);
            count++;
        }

        if (count > 0)
            logger.LogInformation("Reconciled {Count} pending node sync operations.", count);

        return count;
    }

    private static string ComposeDevEui(int hwRev, int fwRev, string mac) =>
        $"{hwRev:X2}{fwRev:X2}{mac.ToLowerInvariant()}";

    public async Task<ProvisionNodeResponse> ProvisionAsync(ProvisionNodeRequest req)
    {
        var mac = req.MacAddress.ToLowerInvariant();
        var devEui = ComposeDevEui(req.HwRevision, req.FwRevision, mac);

        var appKeyBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var appKeyHex = Convert.ToHexString(appKeyBytes);
        var appKeyEncrypted = cipher.Encrypt(appKeyHex);

        Guid id;
        try
        {
            id = await db.ExecuteScalarAsync<Guid>(
                """
                INSERT INTO general.nodes
                    (dev_eui, mac_address, hw_revision, fw_revision, app_key_encrypted, alias)
                VALUES
                    (@DevEui, @Mac, @HwRev, @FwRev, @AppKeyEnc, '')
                RETURNING id;
                """,
                new
                {
                    DevEui = devEui,
                    Mac = mac,
                    HwRev = req.HwRevision,
                    FwRev = req.FwRevision,
                    AppKeyEnc = appKeyEncrypted
                });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException(
                $"Node with MAC address {mac} or DevEUI {devEui} is already provisioned.", ex);
        }

        return new ProvisionNodeResponse(id, devEui, mac, req.HwRevision, req.FwRevision, appKeyHex);
    }

    public async Task<NodeDto?> UpdateProvisionAsync(Guid id, UpdateProvisionRequest req)
    {
        var mac = await db.QuerySingleOrDefaultAsync<string>(
            "SELECT mac_address FROM general.nodes WHERE id = @Id AND org_id IS NULL;",
            new { Id = id });

        if (mac is null)
            return null;

        var newDevEui = ComposeDevEui(req.HwRevision, req.FwRevision, mac);

        try
        {
            await db.ExecuteAsync(
                """
                UPDATE general.nodes
                SET hw_revision = @HwRev, fw_revision = @FwRev, dev_eui = @DevEui
                WHERE id = @Id;
                """,
                new { HwRev = req.HwRevision, FwRev = req.FwRevision, DevEui = newDevEui, Id = id });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException(
                $"Node DevEUI {newDevEui} is already in use.", ex);
        }

        return await db.QuerySingleOrDefaultAsync<NodeDto>(
            $"{SelectBase} WHERE id = @Id", new { Id = id });
    }

    public async Task<string> RotateAppKeyAsync(Guid id)
    {
        var exists = await db.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM general.nodes WHERE id = @Id AND org_id IS NOT NULL);",
            new { Id = id });


        if (!exists)
            throw new InvalidOperationException("Node does not exist or is not activated.");

        var appKeyBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var appKeyHex = Convert.ToHexString(appKeyBytes);
        var appKeyEncrypted = cipher.Encrypt(appKeyHex);

        await db.ExecuteAsync(
            "UPDATE general.nodes SET app_key_encrypted = @Enc WHERE id = @Id;",
            new { Enc = appKeyEncrypted, Id = id });

        return appKeyHex;
    }

    public async Task<IEnumerable<NodeDto>> ListProvisionedAsync()
    {
        var rows = await db.QueryAsync<NodeRow>(
               $"{SelectBase} WHERE org_id IS NULL ORDER BY created_at DESC");
        return rows.Select(MapRow);
    }

    private static NodeDto MapRow(NodeRow r) => new(
        r.Id, r.DevEui, r.MacAddress, r.OrgId, r.Alias, r.MeterType,
        r.OperativeState, r.SyncStatus, r.SyncError, r.HwRevision,
        r.FwRevision, r.Latitude, r.Longitude, r.CreatedAt
    );
}