using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles
{
    public class RolesDataHelper
    {
        private readonly ILogger<RolesDataHelper> Logger;
        private readonly DataCache Cache;

        public RolesDataHelper(ILogger<RolesDataHelper> logger, DataCache cache)
        {
            Logger = logger;
            Cache = cache;
        }

        public async Task<string> JoinAllAvailableRoles(DiscordMember member, DiscordGuild guild)
        {
            StringBuilder builder = new StringBuilder();
            Dictionary<string, DiscordRole> discordRolesByName = guild.Roles.Values.ToDictionary(x => x.Name.ToLower());
            var allRoles = GetSelfRoles(guild.Id);
            foreach (var role in allRoles)
            {
                if (discordRolesByName.TryGetValue(role.RoleName.ToLower(), out DiscordRole discordRole))
                {
                    if (!member.Roles.Any(userRole => userRole.Id == discordRole.Id))
                    {
                        await member.GrantRoleAsync(discordRole, "Requested to join All Self Roles.");
                        string roleMention = discordRole.IsMentionable ? discordRole.Mention : discordRole.Name;
                        builder.AppendLine($"**{roleMention}**");
                    }
                }
                else
                {
                    builder.AppendLine($"**{role.RoleName}** - is not a valid role on this server, make sure the server role has not been deleted.");
                }
            }
            return builder.ToString();
        }

        public bool IsSelfRole(ulong guildId, string roleName)
        {
            return Cache.SelfRoles.BotKnowsRole(guildId, roleName);
        }

        public List<SelfRole> GetSelfRoles(ulong guildId)
        {
            List<SelfRole> result = null;
            if (Cache.SelfRoles.TryGetGuildRoles(guildId, out Dictionary<string, SelfRole> roles))
            {
                result = roles.Values.OrderBy(r => r.RoleName).ToList();
            }
            return result;
        }

        public async Task CreateSelfRole(ulong guildId, string roleName, string description, bool hidden)
        {
            Logger.LogInformation($"Request to create self role [{roleName}] in Guild [{guildId}] received.");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                SelfRole role = new SelfRole(roleName, guild.RowId, description, hidden);
                await Cache.SelfRoles.AddRole(guildId, role);
            }
            else
            {
                Logger.LogWarning($"Could not create self role because Guild [{guild}] does not exist");
            }
        }

        public async Task DeleteSelfRole(ulong guildId, string roleName)
        {
            Logger.LogInformation($"Request to delete self role [{roleName}] in Guild [{guildId}] received.");
            await Cache.SelfRoles.RemoveRole(guildId, roleName);
        }
    }
}