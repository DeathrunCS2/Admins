using Admins.Config;
using Admins.Interfaces;
using Admins.Services;
using DeathrunManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.LivesSystem;

public class TakeLivesCommand(
    IDeathrunManager deathrunManagerApi) : IAdminCommand
{
    public string CommandString => "takelives";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "removelives" ],
        Permission = "takelives"
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
                callerDeathrunPlayer.SendChatMessage("Usage: /takelives <name> <amount>");
                return;
            }
            
            var livesAmount = command.GetArg(2);
            if (int.TryParse(livesAmount, out var parsedLivesAmount) && parsedLivesAmount > 0 )
            {
                foreach (var targetDeathrunPlayer in targetDeathrunPlayers)
                {
                    if (targetDeathrunPlayer == callerDeathrunPlayer)
                    {
                        targetDeathrunPlayer.LivesSystem?.RemoveLives(parsedLivesAmount);
                        targetDeathrunPlayer.SendChatMessage($"You took {{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives from yourself!");
                        continue;
                    }
                    
                    targetDeathrunPlayer.LivesSystem?.RemoveLives(parsedLivesAmount);
                    targetDeathrunPlayer.SendChatMessage($"Admin {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}} "
                                                         + $"took {{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives from {{GREEN}}{targetDeathrunPlayer.Client.Name}{{DEFAULT}}!",
                        new RecipientFilter());

                    //notify target too
                    targetDeathrunPlayer.SendChatMessage(
                        $"{{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives have been taken from you by {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}}!");
                }
            }
            else
            {
                callerDeathrunPlayer.SendChatMessage("Invalid amount of lives.");
            }
        }
    }
}