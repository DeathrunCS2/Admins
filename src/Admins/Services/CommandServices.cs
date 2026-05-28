using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Admins.Extensions;
using DeathrunManager.Shared.Objects;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Admins.Services;

internal static class CommandServices
{
    public static class CommandTargeting
    {
        private enum TargetType
        {
            GroupAll,
            GroupBots,
            GroupHumans,
            GroupAlive,
            GroupDead,
            GroupNotMe,

            PlayerMe,
            PlayerAim,

            TeamCt,
            TeamT,
            TeamSpec
        }
        
        private static readonly IReadOnlyDictionary<string, TargetType> TargetTypeMap =
            new Dictionary<string, TargetType>(StringComparer.OrdinalIgnoreCase)
            {
                { "@all", TargetType.GroupAll },
                { "@bots", TargetType.GroupBots },
                { "@human", TargetType.GroupHumans },
                { "@alive", TargetType.GroupAlive },
                { "@dead", TargetType.GroupDead },
                { "@!me", TargetType.GroupNotMe },
                { "@me", TargetType.PlayerMe },
                { "@aim", TargetType.PlayerAim },
                { "@ct", TargetType.TeamCt },
                { "@t", TargetType.TeamT },
                { "@spec", TargetType.TeamSpec }
            }.ToFrozenDictionary();

        public static IReadOnlyCollection<IDeathrunPlayer>? GetCommandTargets(IDeathrunPlayer callerDeathrunPlayer, StringCommand command)
        {
            if (command.ArgCount < 1)
            {
                callerDeathrunPlayer.SendChatMessage("Usage: <target>");
                return null;
            }

            var targetIdentifier = command.GetArg(1);

            return GetTargets(callerDeathrunPlayer, targetIdentifier);
        }

        private static IReadOnlyCollection<IDeathrunPlayer>? GetTargets(
            IDeathrunPlayer callerDeathrunPlayer,
            string targetIdentifier)
        {
            if (string.IsNullOrWhiteSpace(targetIdentifier))
            {
                callerDeathrunPlayer.SendChatMessage("Target cannot be empty.");
                return null;
            }

            var validDeathrunPlayers = Admins.Instance
                .DeathrunManager
                .Managers
                .PlayersManager
                .GetAllValidDeathrunPlayers();

            IReadOnlyCollection<IDeathrunPlayer> targetDeathrunPlayers;

            if (TargetTypeMap.TryGetValue(targetIdentifier, out var targetType))
            {
                targetDeathrunPlayers = GetTargetsByTargetType(
                    callerDeathrunPlayer,
                    validDeathrunPlayers,
                    targetType);
            }
            else
            {
                targetDeathrunPlayers = GetTargetsByIdentifier(
                    validDeathrunPlayers,
                    targetIdentifier);
            }

            if (targetDeathrunPlayers.Count <= 0)
            {
                callerDeathrunPlayer.SendChatMessage($"Target {{GREEN}}{targetIdentifier} {{DEFAULT}}not found.");
                return null;
            }

            return targetDeathrunPlayers;
        }

        private static IReadOnlyCollection<IDeathrunPlayer> GetTargetsByTargetType(
            IDeathrunPlayer callerDeathrunPlayer, 
            IReadOnlyCollection<IDeathrunPlayer> validDeathrunPlayers, TargetType targetType)
        {
            return targetType switch
            {
                TargetType.GroupAll => validDeathrunPlayers,

                TargetType.GroupBots => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.Controller?.SteamId == 0),

                TargetType.GroupHumans => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.Controller?.SteamId != 0),

                TargetType.GroupAlive => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.IsValidAndAlive),

                TargetType.GroupDead => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.IsValidAndAlive is not true),

                TargetType.GroupNotMe => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => IsSamePlayer(deathrunPlayer, callerDeathrunPlayer) is not true),

                TargetType.PlayerMe => [ callerDeathrunPlayer ],

                TargetType.TeamCt => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.Controller?.Team == CStrikeTeam.CT),

                TargetType.TeamT => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.Controller?.Team == CStrikeTeam.TE),

                TargetType.TeamSpec => validDeathrunPlayers
                    .FilterPlayers(deathrunPlayer => deathrunPlayer.Controller?.Team == CStrikeTeam.Spectator),

                TargetType.PlayerAim => GetAimTarget(callerDeathrunPlayer, validDeathrunPlayers),

                _ => []
            };
        }

        private static IReadOnlyCollection<IDeathrunPlayer> GetTargetsByIdentifier(
            IReadOnlyCollection<IDeathrunPlayer> validDeathrunPlayers,
            string identifier)
        {
            var exactTargets = GetExactIdentifierMatches(validDeathrunPlayers, identifier);

            if (exactTargets.Count > 0)
            {
                return exactTargets;
            }

            return validDeathrunPlayers
                .FilterPlayers(player =>
                    player.Client.Name.Contains(
                        identifier,
                        StringComparison.InvariantCultureIgnoreCase));
        }

        private static IReadOnlyCollection<IDeathrunPlayer> GetExactIdentifierMatches(
            IReadOnlyCollection<IDeathrunPlayer> validDeathrunPlayers,
            string identifier)
        {
            if (ulong.TryParse(identifier, out var steamId64))
            {
                var steamIdMatches = validDeathrunPlayers
                    .FilterPlayers(player => player.Client.SteamId == steamId64);

                if (steamIdMatches.Count > 0)
                {
                    return steamIdMatches;
                }
            }

            if (int.TryParse(identifier, out int userId))
            {
                var userIdMatches = validDeathrunPlayers
                    .FilterPlayers(player => player.Client.UserId == userId);

                if (userIdMatches.Count > 0)
                {
                    return userIdMatches;
                }

                var slotMatches = validDeathrunPlayers
                    .FilterPlayers(player => player.Client.Slot == userId);

                if (slotMatches.Count > 0)
                {
                    return slotMatches;
                }
            }

            return Array.Empty<IDeathrunPlayer>();
        }

        private static IReadOnlyCollection<IDeathrunPlayer> GetAimTarget(
            IDeathrunPlayer callerDeathrunPlayer,
            IReadOnlyCollection<IDeathrunPlayer> validDeathrunPlayers)
        {
            // Placeholder because exact aim-tracing depends on your current utility methods.
            // If your framework already has GetPlayerAimTarget / TraceAim / RayTrace helpers,
            // plug them in here and return either one player or Array.Empty<IDeathrunPlayer>().

            return [];
        }

        private static bool IsSamePlayer(IDeathrunPlayer first, IDeathrunPlayer second)
        {
            return first.Client.SteamId == second.Client.SteamId;
        }
    }
    
}
