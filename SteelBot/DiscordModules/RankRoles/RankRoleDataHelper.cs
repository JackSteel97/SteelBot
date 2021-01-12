using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    public class RankRoleDataHelper
    {
        private readonly ILogger<RankRoleDataHelper> Logger;
        private readonly DataCache Cache;

        public RankRoleDataHelper(DataCache cache, ILogger<RankRoleDataHelper> logger)
        {
            Cache = cache;
            Logger = logger;
        }

        public async Task CreateRankRole(ulong guildId, string roleName, int requiredRank)
        {
            Logger.LogInformation($"Request to create Rank Role [{roleName}] in Guild [{guildId}] received");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                RankRole role = new RankRole(roleName, guild.RowId, requiredRank);
                await Cache.RankRoles.AddRole(guildId, role);
            }
            else
            {
                Logger.LogWarning($"Could not create Rank Role because Guild [{guildId}] does not exist.");
            }
        }

        public async Task DeleteRankRole(ulong guildId, string roleName, DiscordGuild guild)
        {
            Logger.LogInformation($"Request to delete Rank Role [{roleName}] in Guild [{guildId}] received.");

            if (TryGetRankRole(guildId, roleName, out RankRole roleToDelete) && TryGetAllRankRolesInGuild(guildId, out List<RankRole> allRoles))
            {
                // Get all users in this guild.
                List<User> users = Cache.Users.GetUsersInGuild(guildId);
                foreach (User user in users)
                {
                    // Do we need to remove this role from this user?
                    if (user.CurrentRankRoleRowId == roleToDelete.RowId)
                    {
                        // Yes, find a role to replace it with.
                        RankRole roleToGrant = FindHighestRankRoleForLevel(allRoles.ToList(), user, new HashSet<string>() { roleToDelete.RoleName }, true);

                        // Get the user and the role to remove.
                        DiscordMember member = await guild.GetMemberAsync(user.DiscordId);
                        DiscordRole discordRoleToDelete = guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleToDelete.RoleName, StringComparison.OrdinalIgnoreCase));
                        if (discordRoleToDelete != null)
                        {
                            // Remove their old, about to be deleted role.
                            await member.RevokeRoleAsync(discordRoleToDelete, "This rank role was deleted by an admin.");
                        }

                        string roleToGrantMention = null;
                        if (roleToGrant != default)
                        {
                            // If their role can be replaced, replace it.
                            DiscordRole discordRoleToGrant = guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleToGrant.RoleName, StringComparison.OrdinalIgnoreCase));
                            if (discordRoleToDelete == null)
                            {
                                Logger.LogWarning($"While deleting the role [{roleToDelete.RoleName}] in Guild [{guild.Id}] the next best Role [{roleToGrant.RoleName}] to grant to User [{user.DiscordId}] could not be found in the Discord server. The user was skipped.");
                                continue;
                            }
                            roleToGrantMention = discordRoleToGrant.IsMentionable ? discordRoleToGrant.Mention : discordRoleToGrant.Name;

                            await member.GrantRoleAsync(discordRoleToGrant, "Previous rank role deleted.");
                        }

                        // Update their role in the cache and notify the user.
                        await Cache.Users.UpdateRankRole(guildId, user.DiscordId, roleToGrant);
                        await SendRankChangeDueToDeletionMessage(guild, member, roleToDelete, roleToGrantMention);
                    }
                }
                // Remove role from the cache and database once all user roles are sorted.
                await Cache.RankRoles.RemoveRole(guildId, roleName);
            }
        }

        public List<User> GetAllUsersInGuild(ulong guildId)
        {
            return Cache.Users.GetUsersInGuild(guildId);
        }

        public bool TryGetRankRole(ulong guildId, string roleName, out RankRole role)
        {
            return Cache.RankRoles.TryGetRole(guildId, roleName, out role);
        }

        public bool TryGetAllRankRolesInGuild(ulong guildId, out List<RankRole> allRoles)
        {
            if (Cache.RankRoles.TryGetGuildRankRoles(guildId, out Dictionary<string, RankRole> roles) && roles.Count > 0)
            {
                allRoles = roles.Values.ToList();
                return true;
            }
            allRoles = default;
            return false;
        }

        public bool RoleExists(ulong guildId, string roleName)
        {
            return Cache.RankRoles.BotKnowsRole(guildId, roleName);
        }

        public bool RoleExistsAtLevel(ulong guildId, int level, out string existingRoleName)
        {
            bool exists = false;
            existingRoleName = null;
            if (Cache.RankRoles.TryGetGuildRankRoles(guildId, out Dictionary<string, RankRole> roles))
            {
                foreach (RankRole role in roles.Values)
                {
                    if (role.LevelRequired == level)
                    {
                        exists = true;
                        existingRoleName = role.RoleName;
                        break;
                    }
                }
            }
            return exists;
        }

        public async Task UpdateRankRole(ulong guildId, ulong userId, RankRole role)
        {
            await Cache.Users.UpdateRankRole(guildId, userId, role);
        }

        public async Task UserLevelledUp(ulong guildId, ulong userId, DiscordGuild guild)
        {
            if (Cache.RankRoles.TryGetGuildRankRoles(guildId, out Dictionary<string, RankRole> roles)
                && Cache.Users.TryGetUser(guildId, userId, out User user))
            {
                RankRole roleToGrant = FindHighestRankRoleForLevel(roles.Values.ToList(), user);
                if (roleToGrant != default)
                {
                    DiscordMember member = await guild.GetMemberAsync(userId);
                    DiscordRole discordRoleToGrant = guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleToGrant.RoleName, StringComparison.OrdinalIgnoreCase));
                    if (user.CurrentRankRole != default)
                    {
                        // Remove any old rank role if one exists.
                        DiscordRole discordRoleToRemove = guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(user.CurrentRankRole.RoleName, StringComparison.OrdinalIgnoreCase));
                        await member.RevokeRoleAsync(discordRoleToRemove, "User achieved a new rank role that overwrites this one.");
                    }

                    await Cache.Users.UpdateRankRole(guildId, userId, roleToGrant);
                    await member.GrantRoleAsync(discordRoleToGrant, $"User achieved level {roleToGrant.LevelRequired}");
                    string roleMention = discordRoleToGrant.IsMentionable ? discordRoleToGrant.Mention : discordRoleToGrant.Name;
                    await SendRankGrantedMessage(guild, member, roleToGrant, roleMention);
                }
            }
        }

        private async Task SendRankGrantedMessage(DiscordGuild discordGuild, DiscordMember discordUser, RankRole achievedRole, string roleMention)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild))
            {
                if (guild.LevelAnnouncementChannelId.HasValue)
                {
                    DiscordChannel channel = discordGuild.GetChannel(guild.LevelAnnouncementChannelId.Value);
                    await channel.SendMessageAsync(discordUser.Mention, embed: EmbedGenerator.Info($"You have been granted the **{roleMention}** role for reaching rank **{achievedRole.LevelRequired}**!", "Rank Role Granted!"));
                }
            }
        }

        private async Task SendRankChangeDueToDeletionMessage(DiscordGuild discordGuild, DiscordMember discordUser, RankRole previousRole, string newRoleMention)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild))
            {
                if (guild.LevelAnnouncementChannelId.HasValue)
                {
                    DiscordChannel channel = discordGuild.GetChannel(guild.LevelAnnouncementChannelId.Value);
                    string newRoleText = string.IsNullOrWhiteSpace(newRoleMention)
                        ? "there are no rank roles eligible to replace it."
                        : $"your new role is **{newRoleMention}**";

                    await channel.SendMessageAsync(discordUser.Mention, embed: EmbedGenerator.Info($"Your previous rank role **{previousRole.RoleName}** has been deleted by an admin, {newRoleText}", "Rank Role Changed"));
                }
            }
        }

        private RankRole FindHighestRankRoleForLevel(List<RankRole> roles, User user, HashSet<string> excludedRoles = null, bool currentRoleIsBeingRemoved = false)
        {
            roles.Sort((r1, r2) => r2.LevelRequired.CompareTo(r1.LevelRequired));
            foreach (RankRole rankRole in roles)
            {
                // Only bother checking if this is higher than the user's current rank role (if they have one)
                if ((user.CurrentRankRole == default || rankRole.LevelRequired > user.CurrentRankRole.LevelRequired || currentRoleIsBeingRemoved)
                    // Make sure they are above the level for this role. and they do not already have it.
                    && user.CurrentLevel >= rankRole.LevelRequired && user.CurrentRankRoleRowId != rankRole.RowId)
                {
                    if (excludedRoles == null || !excludedRoles.Contains(rankRole.RoleName))
                    {
                        return rankRole;
                    }
                }
            }
            return default;
        }
    }
}