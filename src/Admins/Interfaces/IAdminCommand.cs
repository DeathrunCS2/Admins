using Admins.Config;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Admins.Interfaces;

public interface IAdminCommand
{
    public string CommandString { get; }
    public CommandInfo CommandInfo { get; }

    public void OnCommandExecute(IGameClient? caller, StringCommand command);
}