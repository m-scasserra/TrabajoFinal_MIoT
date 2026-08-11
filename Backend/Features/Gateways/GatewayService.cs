using Backend.Common.ChirpStack;
using Backend.Common.Security;
using Backend.Features.Gateways.Dtos;
using Dapper;
using Npgsql;

namespace Backend.Features.Gateways;

public sealed class GatewayService(
    NpgsqlConnection db,
    IChirpStackClient chirpstack,
    ILogger<GatewayService> logger) : IGatewayService
{
    private const string SelectBase = """
        SELECT gateway_eui AS GatewayEui, org_id AS OrgId, alias AS Alias, model AS Model,
               operative_state AS OperativeState, sync_status AS SyncStatus,
               sync_error as SyncError,
               ST_Y(coordinates::geometry) AS Latitude,
               ST_X(coordinates::geometry) AS Longitude,
               last_seen AS LastSeen, created_at AS CreatedAt
        FROM general.gateways
        """;

    public async Task<IEnumerable<GatewayDto>> ListAsync(CurrentUser me)
    {
        const string notDeleted = "sync_status NOT IN ('PENDING_DELETE', 'DELETE_FAILED')";

        if (me.IsSuperAdmin)
            return await db.QueryAsync<GatewayDto>(
                $"{SelectBase} WHERE {notDeleted} ORDER BY created_at DESC");

        return await db.QueryAsync<GatewayDto>(
            $"{SelectBase} WHERE {notDeleted} AND org_id = @OrgId ORDER BY created_at DESC",
            new { OrgId = me.RequireOrgId() });
    }

    public async Task<GatewayDto?> GetByEuiAsync(CurrentUser me, string eui)
    {
        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";
        return await db.QuerySingleOrDefaultAsync<GatewayDto>(
            $"""
            {SelectBase}
            WHERE gateway_eui = @Eui
              AND sync_status NOT IN ('PENDING_DELETE', 'DELETE_FAILED'){whereOrg}
            """,
            new { Eui = eui.ToLowerInvariant(), OrgId = me.OrgId });
    }

    public async Task<GatewayDto> CreateAsync(CurrentUser me, CreateGatewayRequest req, CancellationToken ct = default)
    {
        var orgId = me.RequireOrgId();
        var eui = req.GatewayEui.ToLowerInvariant();

        var point = (req.Latitude.HasValue && req.Longitude.HasValue)
            ? $"SRID=4326;POINT({req.Longitude} {req.Latitude})"
            : null;

        try
        {
            await db.ExecuteAsync(
                """
                INSERT INTO general.gateways
                    (gateway_eui, org_id, alias, model, coordinates, sync_status)
                VALUES
                    (@Eui, @OrgId, @Alias, @Model,
                    CASE WHEN @Point IS NULL THEN NULL ELSE ST_GeomFromEWKT(@Point) END,
                    'PENDING');
                """,
                new { Eui = eui, OrgId = orgId, Alias = req.Alias, @Model = req.Model, Point = point });
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException($"Gateway with EUI '{eui}' already exists.");
        }

        await TrySyncGatewayAsync(eui, req.Alias, req.Latitude, req.Longitude, ct);

        return (await GetByEuiAsync(me, eui))!;
    }

    private async Task TrySyncGatewayAsync(
        string eui, string alias, double? lat, double? lng, CancellationToken ct)
    {
        try
        {
            await chirpstack.CreateGatewayAsync(eui, alias, lat, lng, ct);

            var exists = await chirpstack.GatewayExistsAsync(eui, ct);
            if (!exists)
                throw new InvalidOperationException("ChirpStack gateway creation failed.");

            await db.ExecuteAsync(
                """
                UPDATE general.gateways
                SET sync_status = 'SYNCED', synced_at = NOW(), sync_error = NULL
                WHERE gateway_eui = @Eui;
                """,
                new { Eui = eui });

            logger.LogInformation("Gateway {Eui} synced successfully with ChirpStack.", eui);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                """
                UPDATE general.gateways
                SET sync_status = 'FAILED', sync_error = @Error
                WHERE gateway_eui = @Eui;
                """,
                new { Eui = eui, Error = ex.Message });

            logger.LogWarning(ex, "Failed to sync gateway {Eui} with ChirpStack.", eui);
        }
    }

    public async Task<int> ReconcilePendingAsync(CancellationToken ct = default)
    {
        var count = 0;

        var pending = await db.QueryAsync<(string Eui, string Alias, double? Lat, double? Lng)>(
            """
            SELECT gateway_eui AS Eui, alias AS Alias,
                   ST_Y(coordinates::geometry) AS Lat, ST_X(coordinates::geometry) AS Lng
            FROM general.gateways
            WHERE sync_status IN ('PENDING', 'FAILED');
            """);


        foreach (var g in pending)
        {
            ct.ThrowIfCancellationRequested();

            if (await chirpstack.GatewayExistsAsync(g.Eui, ct))
                await TryUpdateGatewayAsync(g.Eui, g.Alias, g.Lat, g.Lng, ct);
            else
                await TrySyncGatewayAsync(g.Eui, g.Alias, g.Lat, g.Lng, ct);

            count++;
        }

        var toDelete = await db.QueryAsync<string>(
            """
            SELECT gateway_eui
            FROM general.gateways
            WHERE sync_status IN ('PENDING_DELETE', 'DELETE_FAILED');
            """);

        foreach (var eui in toDelete)
        {
            ct.ThrowIfCancellationRequested();

            await TryDeleteGatewayAsync(eui, ct);

            count++;
        }

        if (count > 0)
            logger.LogInformation("Reconciled {Count} pending gateways with ChirpStack.", count);

        return count;
    }

    public async Task<GatewayDto?> UpdateAsync(
        CurrentUser me, string eui, UpdateGatewayRequest req, CancellationToken ct = default)
    {
        eui = eui.ToLowerInvariant();
        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";

        var point = (req.Latitude.HasValue && req.Longitude.HasValue)
            ? $"SRID=4326;POINT({req.Longitude} {req.Latitude})"
            : null;

        var affected = await db.ExecuteAsync(
            $"""
            UPDATE general.gateways
            SET alias = @Alias,
                model = @Model,
                operative_state = @OperativeState::general.gateway_state,
                coordinates = CASE WHEN @Point IS NULL THEN NULL ELSE ST_GeomFromEWKT(@Point) END,
                sync_status = 'PENDING'
            WHERE gateway_eui = @Eui{whereOrg};
            """,
            new { Alias = req.Alias, Model = req.Model, OperativeState = req.OperativeState, Point = point, Eui = eui, OrgId = me.OrgId });

        if (affected == 0)
            return null;

        await TryUpdateGatewayAsync(eui, req.Alias, req.Latitude, req.Longitude, ct);

        return await GetByEuiAsync(me, eui);
    }

    private async Task TryUpdateGatewayAsync(
        string eui, string alias, double? lat, double? lng, CancellationToken ct)
    {
        try
        {
            await chirpstack.UpdateGatewayAsync(eui, alias, lat, lng, ct);

            await db.ExecuteAsync(
                "UPDATE general.gateways SET sync_status = 'SYNCED', synced_at = NOW(), sync_error = NULL WHERE gateway_eui = @Eui;",
                new { Eui = eui });

            logger.LogInformation("Gateway {Eui} updated successfully in ChirpStack.", eui);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.gateways SET sync_status = 'FAILED', sync_error = @Error WHERE gateway_eui = @Eui;",
                new { Eui = eui, Error = ex.Message });

            logger.LogWarning(ex, "Failed to update gateway {Eui} in ChirpStack.", eui);
        }
    }

    public async Task<bool> DeleteAsync(CurrentUser me, string eui, CancellationToken ct = default)
    {
        eui = eui.ToLowerInvariant();
        var whereOrg = me.IsSuperAdmin ? "" : " AND org_id = @OrgId";

        var affected = await db.ExecuteAsync(
            $"UPDATE general.gateways SET sync_status = 'PENDING_DELETE' WHERE gateway_eui = @Eui{whereOrg};",
            new { Eui = eui, OrgId = me.OrgId });

        if (affected == 0)
            return false;

        await TryDeleteGatewayAsync(eui, ct);

        return true;
    }

    private async Task TryDeleteGatewayAsync(
        string eui, CancellationToken ct)
    {
        try
        {
            await chirpstack.DeleteGatewayAsync(eui, ct);

            await db.ExecuteAsync(
                "DELETE FROM general.gateways WHERE gateway_eui = @Eui;",
                new { Eui = eui });

            logger.LogInformation("Gateway {Eui} deleted successfully in ChirpStack.", eui);
        }
        catch (Exception ex)
        {
            await db.ExecuteAsync(
                "UPDATE general.gateways SET sync_status = 'DELETE_FAILED', sync_error = @Error WHERE gateway_eui = @Eui;",
                new { Eui = eui, Error = ex.Message });

            logger.LogWarning(ex, "Failed to delete gateway {Eui} in ChirpStack.", eui);
        }
    }
}