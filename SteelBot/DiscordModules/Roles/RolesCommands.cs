using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles
{
    [Group("SelfRoles")]
    [Aliases("sr")]
    [Description("Self Role commands.")]
    [RequireGuild]
    public class RolesCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelpers;

        public RolesCommands(DataHelpers dataHelper)
        {
            DataHelpers = dataHelper;
        }

        [GroupCommand]
        [Description("Displays the available self roles on this server.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public Task ViewSelfRoles(CommandContext context)
        {
            List<SelfRole> allRoles = DataHelpers.Roles.GetSelfRoles(context.Guild.Id);
            if (allRoles == null || allRoles.Count == 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Warning("There are no self roles available.\nAsk your administrator to create some!"));
            }
            string prefix = DataHelpers.Config.GetPrefix(context.Guild.Id);

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle("Available Self Roles");

            StringBuilder rolesBuilder = new StringBuilder();
            rolesBuilder.AppendLine(Formatter.Bold("All")).AppendLine($" - Join all available Self Roles");
            foreach (SelfRole role in allRoles)
            {
                if (!role.Hidden)
                {
                    DiscordRole discordRole = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
                    string roleMention = role.RoleName;
                    if (discordRole != default && discordRole.IsMentionable)
                    {
                        roleMention = discordRole.Mention;
                    }
                    rolesBuilder.AppendLine(Formatter.Bold(roleMention));
                    rolesBuilder.AppendLine($" - {role.Description}");
                }
            }
            builder.WithDescription($"Use `{prefix}SelfRoles Join \"RoleName\"` to join one of these roles.\n\n{rolesBuilder}");

            return context.RespondAsync(embed: builder.Build());
        }

        [Command("Join")]
        [Aliases("j")]
        [Description("Joins the self role specified.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task JoinRole(CommandContext context, [RemainingText] string roleName)
        {
            if (roleName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                string joinedRoles = await DataHelpers.Roles.JoinAllAvailableRoles(context.Member, context.Guild);
                if (joinedRoles.Length > 0)
                {
                    await context.RespondAsync(embed: EmbedGenerator.Success(joinedRoles, "Joined Roles"));
                }
                else
                {
                    await context.RespondAsync(embed: EmbedGenerator.Warning("There are no roles that you don't already have."));
                }
                return;
            }

            // Get discord role variable.
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid role on this server. Make sure your administrator has added the role."));
                return;
            }

            await DataHelpers.Roles.JoinRole(context, role);
        }

        [Command("Join")]
        [Priority(10)]
        public async Task JoinRole(CommandContext context, DiscordRole role)
        {
            await DataHelpers.Roles.JoinRole(context, role);
        }

        [Command("Leave")]
        [Aliases("l")]
        [Description("Leaves the self role specified.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task LeaveRole(CommandContext context, [RemainingText] string roleName)
        {
            // Get discord role variable.
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid role on this server. Make sure your administrator has added the role."));
                return;
            }
            await DataHelpers.Roles.LeaveRole(context, role);
        }

        [Command("Leave")]
        public async Task LeaveRole(CommandContext context, DiscordRole role)
        {
            await DataHelpers.Roles.LeaveRole(context, role);
        }

        [Command("Set")]
        [Aliases("Create")]
        [Description("Sets the given role as a self role that users can join themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        [Cooldown(10, 60, CooldownBucketType.Guild)]
        public Task SetSelfRole(CommandContext context, string roleName, string description)
        {
            return DataHelpers.Roles.CreateRole(context, roleName, description);
        }

        [Command("Set")]
        [Priority(10)]
        public Task SetSelfRole(CommandContext context, DiscordRole role, string description)
        {
            return DataHelpers.Roles.CreateRole(context, role, description);
        }

        [Command("Remove")]
        [Aliases("Delete")]
        [Description("Removes the given role from the list of self roles, users will no longer be able to join the role themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        [Cooldown(10, 60, CooldownBucketType.Guild)]
        public Task RemoveSelfRole(CommandContext context, [RemainingText] string roleName)
        {
           return DataHelpers.Roles.RemoveRole(context, roleName);
        }

        [Command("Remove")]
        [Priority(10)]
        public Task RemoveSelfRole(CommandContext context, DiscordRole role)
        {
            return DataHelpers.Roles.RemoveRole(context, role);
        }
    }
}