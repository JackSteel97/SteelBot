using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles.Helpers;

public static class RankRoleShared
{
    public static RankRole FindHighestRankRoleForLevel(IEnumerable<RankRole> roles,
        int currentUserLevel,
        RankRole currentRankRole,
        HashSet<ulong> excludedRoles = null,
        bool currentRoleIsBeingRemoved = false)
    {
        RankRole currentCandidate = default;
        foreach (var rankRole in roles)
            if ((currentCandidate == default || rankRole?.LevelRequired >= currentCandidate?.LevelRequired) // Only bother checking if this is lower than the current candidate.
                && (currentRankRole == default || rankRole?.LevelRequired > currentRankRole?.LevelRequired ||
                    currentRoleIsBeingRemoved) // Only bother checking if this is higher than the user's current rank role (if they have one)
                && currentUserLevel >= rankRole.LevelRequired && currentRankRole?.RowId != rankRole.RowId // Make sure they are above the level for this role. and they do not already have it.
                && (excludedRoles == null || !excludedRoles.Contains(rankRole.RoleDiscordId))) // Make sure it's not excluded.
                currentCandidate = rankRole;
        return currentCandidate;
    }

    public static async ValueTask UserLevelledUp(ulong guildId,
        ulong userId,
        DiscordGuild guild,
        RankRolesProvider rankRolesProvider,
        UsersProvider usersProvider,
        LevelMessageSender levelMessageSender)
    {
        if (rankRolesProvider.TryGetGuildRankRoles(guildId, out var roles)
            && usersProvider.TryGetUser(guildId, userId, out var user))
        {
            var roleToGrant = FindHighestRankRoleForLevel(roles, user.CurrentLevel, user.CurrentRankRole);
            if (roleToGrant != default)
            {
                var member = await guild.GetMemberAsync(userId);
                var discordRoleToGrant = guild.GetRole(roleToGrant.RoleDiscordId);
                if (user.CurrentRankRole != default)
                {
                    // Remove any old rank role if one exists.
                    var discordRoleToRemove = guild.GetRole(user.CurrentRankRole.RoleDiscordId);
                    if (discordRoleToGrant != default) await member.RevokeRoleAsync(discordRoleToRemove, "User achieved a new rank role that overwrites this one.");
                }

                await usersProvider.UpdateRankRole(guildId, userId, roleToGrant);
                await member.GrantRoleAsync(discordRoleToGrant, $"User achieved level {roleToGrant.LevelRequired}");

                levelMessageSender.SendRankGrantedMessage(guild, member.Id, roleToGrant, discordRoleToGrant.Mention);
            }
        }
    }
}