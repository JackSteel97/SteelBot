using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.SelfRole;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.Helpers;
using SteelBot.Services;
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

        private readonly SelfRoleManagementChannel SelfRoleManagementChannel;
        private readonly CancellationService CancellationService;

        public RolesDataHelper(ILogger<RolesDataHelper> logger, DataCache cache, SelfRoleManagementChannel selfRoleManagementChannel, CancellationService cancellationService)
        {
            Logger = logger;
            Cache = cache;
            SelfRoleManagementChannel = selfRoleManagementChannel;
            CancellationService = cancellationService;
        }

        public Task CreateRole(CommandContext context, DiscordRole role, string description)
        {
            return CreateRole(context, role.Name, description);
        }

        public async Task CreateRole(CommandContext context, string roleName, string description)
        {
            var change = new SelfRoleManagementAction(SelfRoleActionType.Create, context.Member, context.Message, roleName, description);
            await SelfRoleManagementChannel.WriteChange(change, CancellationService.Token);
        }

        public Task RemoveRole(CommandContext context, DiscordRole role)
        {
            return RemoveRole(context, role.Name);
        }

        public async Task RemoveRole(CommandContext context, string roleName)
        {
            var change = new SelfRoleManagementAction(SelfRoleActionType.Delete, context.Member, context.Message, roleName);
            await SelfRoleManagementChannel.WriteChange(change, CancellationService.Token);
        }

        // <=================> OLD <========================>

        public async Task JoinRole(CommandContext context, DiscordRole role)
        {
            string roleMention = role.IsMentionable ? role.Mention : role.Name;

            // Check role is a self role.
            if (!IsSelfRole(context.Guild.Id, role.Name))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleMention}** is not a valid self role"));
                return;
            }

            if (context.Member.Roles.Any(userRole => userRole.Id == role.Id))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"You already have the {roleMention} role."));
                return;
            }

            // Add to user.
            await context.Member.GrantRoleAsync(role, "Requested to join Self Role.");
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.Member.Mention} joined {roleMention}"));
        }

        public async Task LeaveRole(CommandContext context, DiscordRole role)
        {
            string roleMention = role.IsMentionable ? role.Mention : role.Name;

            // Check role is a self role.
            if (!IsSelfRole(context.Guild.Id, role.Name))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleMention}** is not a valid self role"));
                return;
            }

            // Remove from user.
            await context.Member.RevokeRoleAsync(role, "Requested to leave Self Role.");
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.User.Mention} left {roleMention}"));
        }

        public async Task<string> JoinAllAvailableRoles(DiscordMember member, DiscordGuild guild)
        {
            StringBuilder builder = new StringBuilder();
            // TODO: Use discord ids to index these instead of names.
            Dictionary<string, DiscordRole> discordRolesByName = new Dictionary<string, DiscordRole>(guild.Roles.Count);
            foreach(var role in guild.Roles.Values)
            {
                string name = role.Name.ToLower();
                if (!discordRolesByName.ContainsKey(name))
                {
                    discordRolesByName.Add(name, role);
                }
            }

            List<SelfRole> allRoles = GetSelfRoles(guild.Id);
            foreach (SelfRole role in allRoles)
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