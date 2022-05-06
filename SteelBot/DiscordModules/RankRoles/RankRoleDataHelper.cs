using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.RankRole;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    public class RankRoleDataHelper
    {
        private readonly ILogger<RankRoleDataHelper> Logger;
        private readonly DataCache Cache;
        private readonly RankRoleManagementChannel _rankRoleManagementChannel;
        private readonly CancellationService _cancellationService;

        public RankRoleDataHelper(DataCache cache, ILogger<RankRoleDataHelper> logger, RankRoleManagementChannel rankRoleManagementChannel, CancellationService cancellationService)
        {
            Cache = cache;
            Logger = logger;
            _rankRoleManagementChannel = rankRoleManagementChannel;
            _cancellationService = cancellationService;
        }

        public ValueTask CreateRankRole(CommandContext context, string roleName, int requiredLevel)
        {
            var message = new RankRoleManagementAction(RankRoleManagementActionType.Create, context.Message, roleName, requiredLevel);
            return WriteAction(message);
        }

        public ValueTask CreateRankRole(CommandContext context, DiscordRole role, int requiredLevel)
        {
            var message = new RankRoleManagementAction(RankRoleManagementActionType.Create, context.Message, role.Id, requiredLevel);
            return WriteAction(message);
        }

        public ValueTask DeleteRankRole(CommandContext context, string roleName)
        {
            var message = new RankRoleManagementAction(RankRoleManagementActionType.Delete, context.Message, roleName);
            return WriteAction(message);
        }

        public ValueTask DeleteRankRole(CommandContext context, DiscordRole role)
        {
            var message = new RankRoleManagementAction(RankRoleManagementActionType.Delete, context.Message, role.Id);
            return WriteAction(message);
        }

        public ValueTask ViewRankRoles(CommandContext context)
        {
            var message = new RankRoleManagementAction(RankRoleManagementActionType.View, context.Message);
            return WriteAction(message);
        }

        private ValueTask WriteAction(RankRoleManagementAction action) => _rankRoleManagementChannel.Write(action, _cancellationService.Token);

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
    }
}