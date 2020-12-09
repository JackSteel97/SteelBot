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
            Logger.LogInformation($"Request to create rank role [{roleName}] in Guild [{guildId}] received");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                RankRole role = new RankRole(roleName, guild.RowId, requiredRank);
                await Cache.RankRoles.AddRole(guildId, role);
            }
            else
            {
                Logger.LogWarning($"Could not create rank role because Guild [{guildId}] does not exist.");
            }
        }

        public async Task DeleteRankRole(ulong guildId, string roleName)
        {
            Logger.LogInformation($"Request to delete self role [{roleName}] in Guild [{guildId}] received.");
            await Cache.RankRoles.RemoveRole(guildId, roleName);
        }

        public List<User> GetAllUsersInGuild(ulong guildId)
        {
            return Cache.Users.GetUsersInGuild(guildId);
        }

        public bool RoleExists(ulong guildId, string roleName)
        {
            return Cache.RankRoles.BotKnowsRole(guildId, roleName);
        }

        public async Task UserLevelledUp(ulong guildId, ulong userId, DiscordGuild guild)
        {
            if (Cache.RankRoles.TryGetGuildRankRoles(guildId, out Dictionary<string, RankRole> roles)
                && Cache.Users.TryGetUser(guildId, userId, out User user))
            {
                foreach (var rankRole in roles.Values)
                {
                    if (user.CurrentLevel >= rankRole.LevelRequired)
                    {
                        var member = await guild.GetMemberAsync(userId);
                        var role = guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(rankRole.RoleName));
                        if (!member.Roles.Any(r => r.Id == role.Id))
                        {
                            await member.GrantRoleAsync(role, $"User achieved level {rankRole.LevelRequired}");
                            await SendRankGrantedMessage(guild, member, rankRole);
                        }
                    }
                }
            }
        }

        private async Task SendRankGrantedMessage(DiscordGuild discordGuild, DiscordMember discordUser, RankRole achievedRole)
        {
            if (Cache.Guilds.TryGetGuild(discordGuild.Id, out Guild guild))
            {
                if (guild.LevelAnnouncementChannelId.HasValue)
                {
                    DiscordChannel channel = discordGuild.GetChannel(guild.LevelAnnouncementChannelId.Value);
                    await channel.SendMessageAsync(embed: EmbedGenerator.Info($"{discordUser.Mention} has been granted the **{achievedRole.RoleName}** role for reaching rank **{achievedRole.LevelRequired}**!", "Rank Granted!"));
                }
            }
        }
    }
}