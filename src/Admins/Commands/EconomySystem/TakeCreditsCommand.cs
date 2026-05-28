using Admins.Config;
using Admins.Interfaces;
using Admins.Services;
using DeathrunManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.EconomySystem;

public class TakeCreditsCommand(
    IDeathrunManager deathrunManagerApi) : IAdminCommand
{
    public string CommandString => "takecredits";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "removecredits" ],
        Permission = "takecredits"
    };
    
    public void OnCommandExecute(IGameClient? caller, StringCommand command)
    {
        if (caller is null) return;
        
        var callerDeathrunPlayer = deathrunManagerApi.Managers.PlayersManager.GetDeathrunPlayer(caller);
        if (callerDeathrunPlayer is null) return;
        
        var targetDeathrunPlayers = CommandServices.CommandTargeting.GetCommandTargets(callerDeathrunPlayer, command);
        if (targetDeathrunPlayers is not null)
        {
            if (command.ArgCount is not 2)
            {
                callerDeathrunPlayer.SendChatMessage("Usage: /takecredits <name> <amount>");
                return;
            }
            
            var creditsAmount = command.GetArg(2);
            if (int.TryParse(creditsAmount, out var parsedCreditsNum) && parsedCreditsNum > 0 )
            {
                foreach (var targetDeathrunPlayer in targetDeathrunPlayers)
                {
                    if (targetDeathrunPlayer == callerDeathrunPlayer)
                    {
                        targetDeathrunPlayer.EconomySystem?.DeductCreditsNum(parsedCreditsNum);
                        targetDeathrunPlayer.SendChatMessage($"You took {{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits from yourself!");
                        continue;
                    }
                    
                    targetDeathrunPlayer.EconomySystem?.DeductCreditsNum(parsedCreditsNum);
                    targetDeathrunPlayer.SendChatMessage($"Admin {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}} "
                                                         + $"took {{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits from {{GREEN}}{targetDeathrunPlayer.Client.Name}{{DEFAULT}}!",
                        new RecipientFilter());

                    //notify target too
                    targetDeathrunPlayer.SendChatMessage(
                        $"{{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits have been taken from you by {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}}!");
                }
            }
            else
            {
                callerDeathrunPlayer.SendChatMessage("Invalid amount of credits.");
            }
        }
    }
}