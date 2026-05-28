using System.Collections.Generic;

namespace Admins.Config;

internal sealed class CommandsConfig
{
    public Dictionary<string, CommandInfo> Commands { get; init; } = [];
}

public class CommandInfo
{
    public string Permission { get; init; } = "";
    public string[] Aliases { get; init; } = [];
}
