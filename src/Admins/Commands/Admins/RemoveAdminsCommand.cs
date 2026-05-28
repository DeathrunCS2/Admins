using System;
using System.Threading.Tasks;
using Admins.Config;
using Admins.Interfaces;
using Admins.Managers;
using Dapper;
using DeathrunManager.Shared;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.Admins;

public class RemoveAdminsCommand(
    IModSharp modSharp,
    IDeathrunManager deathrunManagerApi,
    ILogger<RemoveAdminsCommand> logger) : IAdminCommand
{
    public string CommandString => "removeadmin";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "takeadmin" ],
        Permission = "removeadmin"
    };
    
    public void OnCommandExecute(IGameClient? caller, StringCommand command)
    {
        //ensure we have 1 argument
        if (command.ArgCount is not 1)
        {
            logger.LogInformation("Invalid syntax. Correct syntax: {example}", "ms_removeadmin 'steamid64'");
            return;
        }
        
        var targetSteamId64 = ulong.TryParse(command.GetArg(1), out var targetSteamId64Value) ? targetSteamId64Value : 99999999999999;
        
        Task.Run(async () =>
        {
            var removedAdmin = await OnRemoveAdminCommandAsync(targetSteamId64);
            if (removedAdmin is not true) return;
            
            AdminsManager.Instance.ReloadAdmins();
        
            if (caller is null)
                logger.LogInformation("Removed Admin data for {steamId}.", targetSteamId64);
            else
                await modSharp.InvokeFrameActionAsync(() => deathrunManagerApi.Managers.PlayersManager.GetDeathrunPlayer(caller)?.SendChatMessage($"Removed Admin data for {{GREEN}}{targetSteamId64}."));
        });
    }
    
    private async Task<bool> OnRemoveAdminCommandAsync(ulong steamId64)
    {
        try
        {
            var hasAdminData = await HasAdminData(steamId64);
            if (hasAdminData is not true)
            {
                logger.LogInformation("No Admin data found for {steamId}", steamId64);
                return false;
            }
            
            await using var connection = new MySqlConnection(AdminsManager.ConnectionString);
            await connection.OpenAsync();
    
            await connection.QueryFirstOrDefaultAsync
            ($@"DELETE FROM {AdminsManager.AdminsConfig.Storage.TableName} WHERE steamid64 = @SteamId64",
                new { SteamId64 = steamId64 }
            );

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }
    
    private static async Task<bool> HasAdminData(ulong steamId64)
    {
        try
        {
            await using var connection = new MySqlConnection(AdminsManager.ConnectionString);
            await connection.OpenAsync();
    
            var hasAdminData 
                = await connection.QueryFirstOrDefaultAsync<bool>
                ($@"SELECT EXISTS(SELECT 1 FROM `{AdminsManager.AdminsConfig.Storage.TableName}`
                                            WHERE steamid64 = @SteamId64 LIMIT 1)
                                         ",
                    new { SteamId64 = steamId64 }
                                    
                );
            
            return hasAdminData;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return false;
    }
}