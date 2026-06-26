using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Admins.Interfaces.Managers;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;

namespace Admins.Managers;

internal sealed class ServerUserLoginIpManager(
    IClientManager clientManager,
    ILogger<ServerUserLoginIpManager> logger) : IManager, IClientListener
{
    private const string TableName = "deathrun_server_user_login_ips";

    private readonly ConcurrentDictionary<ulong, string> _lastKnownIpBySteamId = new();
    private readonly SemaphoreSlim _databaseTableSemaphore = new(1, 1);
    private volatile bool _databaseTableReady;

    public bool Init()
    {
        try
        {
            EnsureDatabaseTableAsync().GetAwaiter().GetResult();
            _databaseTableReady = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure server user IP logging database table. Runtime IP logging will retry on player events.");
        }

        try
        {
            clientManager.InstallClientListener(this);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install server user IP listener.");
            return false;
        }
    }

    public void Shutdown()
    {
        try
        {
            clientManager.RemoveClientListener(this);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Server user IP listener was already removed or was not installed.");
        }

        _lastKnownIpBySteamId.Clear();
    }

    public void OnClientConnected(IGameClient client)
    {
        if (TryGetClientIpInfo(client, out var steamId64, out var playerName, out var ipAddress) is not true)
            return;

        _lastKnownIpBySteamId[steamId64] = ipAddress;

        _ = LogConnectAsync(steamId64, playerName, ipAddress);
    }

    public void OnClientDisconnecting(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (TryGetClientIpInfo(client, out var steamId64, out var playerName, out var ipAddress) is not true)
            return;

        _ = LogDisconnectAsync(steamId64, playerName, ipAddress);
        _lastKnownIpBySteamId.TryRemove(steamId64, out _);
    }

    private bool TryGetClientIpInfo(IGameClient client, out ulong steamId64, out string playerName, out string ipAddress)
    {
        steamId64 = 0;
        playerName = "";
        ipAddress = "";

        try
        {
            if (client.IsFakeClient || client.IsHltv)
                return false;

            steamId64 = client.SteamId;
            if (steamId64 is 0)
                return false;

            playerName = Truncate(client.Name, 128);
            ipAddress = client.GetAddress(false) ?? client.Address ?? "";

            if (string.IsNullOrWhiteSpace(ipAddress) && _lastKnownIpBySteamId.TryGetValue(steamId64, out var cachedIp))
                ipAddress = cachedIp;

            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            ipAddress = Truncate(ipAddress.Trim(), 64);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to collect player IP information for logging.");
            return false;
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static async Task EnsureDatabaseTableAsync()
    {
        await using var dbConnection = new MySqlConnection(AdminsManager.ConnectionString);
        await dbConnection.OpenAsync();

        await dbConnection.ExecuteAsync($@"
            CREATE TABLE IF NOT EXISTS `{TableName}`
            (
                `id` BIGINT NOT NULL AUTO_INCREMENT,
                `steamid64` BIGINT(255) NOT NULL,
                `player_name` VARCHAR(128) NOT NULL DEFAULT '',
                `ip_address` VARCHAR(64) NOT NULL,
                `first_connect_time` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `last_connect_time` TIMESTAMP NULL DEFAULT NULL,
                `last_disconnect_time` TIMESTAMP NULL DEFAULT NULL,
                `connect_count` INT UNSIGNED NOT NULL DEFAULT 0,
                `disconnect_count` INT UNSIGNED NOT NULL DEFAULT 0,

                PRIMARY KEY (`id`),
                UNIQUE KEY `ux_deathrun_server_user_login_ips_steam_ip` (`steamid64`, `ip_address`),
                KEY `ix_deathrun_server_user_login_ips_steamid64` (`steamid64`),
                KEY `ix_deathrun_server_user_login_ips_ip_address` (`ip_address`)
            )");
    }
    private async Task EnsureDatabaseTableReadyAsync()
    {
        if (_databaseTableReady)
            return;

        await _databaseTableSemaphore.WaitAsync();
        try
        {
            if (_databaseTableReady)
                return;

            await EnsureDatabaseTableAsync();
            _databaseTableReady = true;
        }
        finally
        {
            _databaseTableSemaphore.Release();
        }
    }


    private async Task LogConnectAsync(ulong steamId64, string playerName, string ipAddress)
    {
        try
        {
            await EnsureDatabaseTableReadyAsync();

            await using var dbConnection = new MySqlConnection(AdminsManager.ConnectionString);
            await dbConnection.OpenAsync();

            await dbConnection.ExecuteAsync($@"
                INSERT INTO `{TableName}`
                    (`steamid64`, `player_name`, `ip_address`, `first_connect_time`, `last_connect_time`, `connect_count`)
                VALUES
                    (@SteamId64, @PlayerName, @IpAddress, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 1)
                ON DUPLICATE KEY UPDATE
                    `player_name` = VALUES(`player_name`),
                    `last_connect_time` = UTC_TIMESTAMP(),
                    `connect_count` = `connect_count` + 1",
                new
                {
                    SteamId64 = steamId64,
                    PlayerName = playerName,
                    IpAddress = ipAddress
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log connect IP for SteamID64 {SteamId64}.", steamId64);
        }
    }

    private async Task LogDisconnectAsync(ulong steamId64, string playerName, string ipAddress)
    {
        try
        {
            await EnsureDatabaseTableReadyAsync();

            await using var dbConnection = new MySqlConnection(AdminsManager.ConnectionString);
            await dbConnection.OpenAsync();

            await dbConnection.ExecuteAsync($@"
                INSERT INTO `{TableName}`
                    (`steamid64`, `player_name`, `ip_address`, `first_connect_time`, `last_disconnect_time`, `disconnect_count`)
                VALUES
                    (@SteamId64, @PlayerName, @IpAddress, UTC_TIMESTAMP(), UTC_TIMESTAMP(), 1)
                ON DUPLICATE KEY UPDATE
                    `player_name` = VALUES(`player_name`),
                    `last_disconnect_time` = UTC_TIMESTAMP(),
                    `disconnect_count` = `disconnect_count` + 1",
                new
                {
                    SteamId64 = steamId64,
                    PlayerName = playerName,
                    IpAddress = ipAddress
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log disconnect IP for SteamID64 {SteamId64}.", steamId64);
        }
    }

    public int ListenerVersion => IClientListener.ApiVersion;
    public int ListenerPriority => -100;
}
