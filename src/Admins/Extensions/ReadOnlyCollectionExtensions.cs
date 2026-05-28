using System;
using System.Collections.Generic;
using DeathrunManager.Shared.Objects;

namespace Admins.Extensions;

public static class ReadOnlyCollectionExtensions
{
    public static IReadOnlyCollection<IDeathrunPlayer> FilterPlayers(
        this IReadOnlyCollection<IDeathrunPlayer> collection, 
        Func<IDeathrunPlayer, bool> predicate)
    {
        var deathrunPlayers = new List<IDeathrunPlayer>(collection.Count);

        foreach (var deathrunPlayer in collection)
            if (predicate(deathrunPlayer))
                deathrunPlayers.Add(deathrunPlayer);
        
        return deathrunPlayers;
    }
}