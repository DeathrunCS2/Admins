using Admins.Config;
using Admins.Interfaces;
using Admins.Services;
using DeathrunManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.EconomySystem;

public class AddCreditsCommand(
    IDeathrunManager deathrunManagerApi) : IAdminCommand
{
    public string CommandString => "addcredits";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "givecredits" ],
        Permission = "addcredits"
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
                callerDeathrunPlayer.SendChatMessage("Usage: /addcredits <name> <amount>");
                return;
            }
            
            var creditsAmount = command.GetArg(2);
            if (int.TryParse(creditsAmount, out var parsedCreditsNum) && parsedCreditsNum > 0 )
            {
                foreach (var targetDeathrunPlayer in targetDeathrunPlayers)
                {
                    if (targetDeathrunPlayer == callerDeathrunPlayer)
                    {
                        targetDeathrunPlayer.EconomySystem?.AddCreditsNum(parsedCreditsNum);
                        targetDeathrunPlayer.SendChatMessage($"You gave {{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits to yourself!");
                        continue;
                    }
                    
                    targetDeathrunPlayer.EconomySystem?.AddCreditsNum(parsedCreditsNum);
                    targetDeathrunPlayer.SendChatMessage($"Admin {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}} "
                                                         + $"added {{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits to {{GREEN}}{targetDeathrunPlayer.Client.Name}{{DEFAULT}}!",
                        new RecipientFilter());

                    //notify target too
                    targetDeathrunPlayer.SendChatMessage(
                        $"{{GREEN}}{parsedCreditsNum} {{DEFAULT}}credits have been given to you by {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}}!");
                }
            }
            else
            {
                callerDeathrunPlayer.SendChatMessage("Invalid amount of credits.");
            }
        }
    }
}