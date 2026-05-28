using Admins.Config;
using Admins.Interfaces;
using Admins.Services;
using DeathrunManager.Shared;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Commands.LivesSystem;

public class AddLivesCommand(
    IDeathrunManager deathrunManagerApi) : IAdminCommand
{
    public string CommandString => "addlives";
    public CommandInfo CommandInfo => new()
    {
        Aliases = [ "givelives" ],
        Permission = "addlives"
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
                callerDeathrunPlayer.SendChatMessage("Usage: /addlives <name> <amount>");
                return;
            }
            
            var livesAmount = command.GetArg(2);
            if (int.TryParse(livesAmount, out var parsedLivesAmount) && parsedLivesAmount > 0 )
            {
                foreach (var targetDeathrunPlayer in targetDeathrunPlayers)
                {
                    if (targetDeathrunPlayer == callerDeathrunPlayer)
                    {
                        targetDeathrunPlayer.LivesSystem?.AddLivesNum(parsedLivesAmount);
                        targetDeathrunPlayer.SendChatMessage($"You gave {{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives to yourself!");

                        continue;
                    }
                    
                    targetDeathrunPlayer.LivesSystem?.AddLivesNum(parsedLivesAmount);
                    targetDeathrunPlayer.SendChatMessage($"Admin {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}} "
                                                         + $"added {{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives to {{GREEN}}{targetDeathrunPlayer.Client.Name}{{DEFAULT}}!",
                        new RecipientFilter());
                
                    //notify target too
                    targetDeathrunPlayer.SendChatMessage($"{{GREEN}}{parsedLivesAmount} {{DEFAULT}}lives have been given to you by {{GREEN}}{callerDeathrunPlayer.Client.Name}{{DEFAULT}}!");
                }
            }
            else
            {
                callerDeathrunPlayer.SendChatMessage("Invalid amount of lives.");
            }
        }
    }
}