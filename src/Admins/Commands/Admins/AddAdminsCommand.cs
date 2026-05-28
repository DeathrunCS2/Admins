using System;
using System.Linq;
using System.Threading.Tasks;
using Admins.Config;
using Admins.Interfaces;
using Admins.Managers;
using Dapper;
using DeathrunManager.Shared;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Sharp.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.Admins;

public class AddAdminsCommand(
    IModSharp modSharp,
    IDeathrunManager deathrunManagerApi,
    ILogger<AddAdminsCommand> logger) : IAdminCommand
{
    public string CommandString => "addadmin";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "giveadmin" ],
        Permission = "addadmin"
    };
    
    public void OnCommandExecute(IGameClient? caller, StringCommand command)
    {
        //ensure we have 3 arguments
        if (command.ArgCount is not 3)
        {
            logger.LogInformation("Invalid syntax. Correct syntax: {example}", "ms_addadmin 'steamid64' 'immunity' 'permissions'");
            return;
        }
        
        var targetSteamId64 = ulong.TryParse(command.GetArg(1), out var targetSteamId64Value) ? targetSteamId64Value : 99999999999999;
        var immunity = byte.TryParse(command.GetArg(2), out var immunityValue) ? immunityValue : (byte) 0;
        var permissions = command.GetArg(3).Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        var addedTimestamp = DateTime.UtcNow.ToTimestamp().ToString();
        
        Task.Run(async () =>
        {
            
            
            var addedAdmin = await OnAddAdminCommandAsync(targetSteamId64,
                                                            immunity,
                                                            permissions.Aggregate((current, next) => current + ',' + next).ToString(),
                                                            addedTimestamp);
            if (addedAdmin is not true) return;
            
            AdminsManager.Instance.ReloadAdmins();
        
            if (caller is null)
                logger.LogInformation("Add Admin data for {steamId} | Immunity: {immunity} | Permissions: {permissions}.", targetSteamId64, immunity, permissions);
            else
                await modSharp.InvokeFrameActionAsync(() => deathrunManagerApi.Managers.PlayersManager.GetDeathrunPlayer(caller)?.SendChatMessage($"Add Admin data for {{GREEN}}{targetSteamId64} {{DEFAULT}}| Immunity: {{GREEN}}{immunity} {{DEFAULT}}| Permissions: {{GREEN}}{permissions}{{DEFAULT}}."));
        });
    }
    
    private async Task<bool> OnAddAdminCommandAsync(ulong steamId64, byte immunity, string permissions, string addedTimestamp)
    {
        try
        {
            var hasAdminData = await HasAdminData(steamId64);
            if (hasAdminData is true)
            {
                logger.LogInformation("Admin data for {steamId} already exists. Skipping...", steamId64);
                return false;
            }
            
            await using var connection = new MySqlConnection(AdminsManager.ConnectionString);
            await connection.OpenAsync();

            var updateQuery = $@" INSERT INTO `{AdminsManager.AdminsConfig.Storage.TableName}` 
                          ( steamid64, immunity, `permissions`, `added_on` )  
                          VALUES 
                          ( @SteamId64, @Immunity, @Permissions, @AddedOn )
                          ON DUPLICATE KEY UPDATE 
                                            immunity       =  {immunity}, 
                                            `permissions`  = '{permissions}',
                                            `added_on`     =  {addedTimestamp} ";
            
            await connection.ExecuteAsync(updateQuery, new { SteamId64                = steamId64, 
                Immunity                 = immunity,
                Permissions              = permissions,
                AddedOn                  = addedTimestamp
            });
            
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