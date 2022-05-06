using SteelBot.Database.Models;
using System.Collections.Generic;
namespace SteelBot.DiscordModules.RankRoles.Helpers;
public static class RankRoleShared
{
    public static RankRole FindHighestRankRoleForLevel(IEnumerable<RankRole> roles, int currentUserLevel, RankRole currentRankRole, HashSet<ulong> excludedRoles = null, bool currentRoleIsBeingRemoved = false)
    {
        RankRole currentCandidate = default;
        foreach(var rankRole in roles)
        {
            if ((currentCandidate == default || rankRole.LevelRequired >= currentCandidate.LevelRequired) // Only bother checking if this is lower than the current candidate.
                && (currentRankRole == default || rankRole.LevelRequired > currentRankRole.LevelRequired || currentRoleIsBeingRemoved) // Only bother checking if this is higher than the user's current rank role (if they have one)
                && (currentUserLevel >= rankRole.LevelRequired && currentRankRole.RowId != rankRole.RowId) // Make sure they are above the level for this role. and they do not already have it.
                && (excludedRoles == null || !excludedRoles.Contains(rankRole.RoleDiscordId))) // Make sure it's not excluded.
            {
                currentCandidate = rankRole;
            }
        }
        return currentCandidate;
    }
}
