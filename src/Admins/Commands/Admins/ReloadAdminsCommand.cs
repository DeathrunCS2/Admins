using Admins.Config;
using Admins.Interfaces;
using Admins.Managers;
using DeathrunManager.Shared;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.Admins;

public class ReloadAdminsCommand(
    IDeathrunManager deathrunManagerApi,
    ILogger<ReloadAdminsCommand> logger) : IAdminCommand
{
    public string CommandString => "reloadadmins";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "refreshadmins" ],
        Permission = "reloadadmins"
    };
    
    public void OnCommandExecute(IGameClient? caller, StringCommand command)
    {
        if (caller is null)
            logger.LogInformation("Admins cache {coloredMsg}!", "reloaded successfully");
        else
            deathrunManagerApi.Managers.PlayersManager.GetDeathrunPlayer(caller)?.SendChatMessage("Admins cache reloaded successfully!");
        
        AdminsManager.Instance.ReloadAdmins();
    }
}