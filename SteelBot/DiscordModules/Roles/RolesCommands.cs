using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
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
            foreach (SelfRole role in allRoles)
            {
                if (!role.Hidden)
                {
                    var discordRole = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
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
            // Check role is a self role.
            if (!DataHelpers.Roles.IsSelfRole(context.Guild.Id, roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid self role"));
                return;
            }

            // Get discord role variable.
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid role on this server. Make sure your administrator has added the role."));
            }

            // Add to user.
            await context.Member.GrantRoleAsync(role, "Requested to join Self Role.");
            string roleMention = role.IsMentionable ? role.Mention : role.Name;
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.Member.Mention} joined {roleMention}"));
        }

        [Command("Leave")]
        [Aliases("l")]
        [Description("Leaves the self role specified.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task LeaveRole(CommandContext context, [RemainingText] string roleName)
        {
            // Check role is a self role.
            if (!DataHelpers.Roles.IsSelfRole(context.Guild.Id, roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid self role"));
                return;
            }

            // Get discord role variable.
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"**{roleName}** is not a valid role on this server. Make sure your administrator has added the role."));
            }

            // Remove from user.
            await context.Member.RevokeRoleAsync(role, "Requested to leave Self Role.");
            string roleMention = role.IsMentionable ? role.Mention : role.Name;
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.User.Mention} left {roleMention}"));
        }

        [Command("Set")]
        [Aliases("Create")]
        [Description("Sets the given role as a self role that users can join themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        [Cooldown(10, 60, CooldownBucketType.Guild)]
        public async Task SetSelfRole(CommandContext context, string roleName, string description, bool hidden = false)
        {
            if (roleName.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The role name must be 255 characters or less."));
                return;
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid role name provided."));
                return;
            }
            if (description.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The role description must be 255 characters or less."));
                return;
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid description provided."));
                return;
            }
            DiscordRole discordRole = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (discordRole == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("You must create the role in the server first."));
                return;
            }
            if (DataHelpers.Roles.IsSelfRole(context.Guild.Id, roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"The self role **{roleName}** already exists. Delete it first if you want to change it."));
                return;
            }

            string roleMention = discordRole.IsMentionable ? discordRole.Mention : discordRole.Name;
            await DataHelpers.Roles.CreateSelfRole(context.Guild.Id, roleName, description, hidden);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Self Role {roleMention} created!"));
        }

        [Command("Remove")]
        [Aliases("Delete")]
        [Description("Removes the given role from the list of self roles, users will no longer be able to join the role themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        [Cooldown(10, 60, CooldownBucketType.Guild)]
        public async Task RemoveSelfRole(CommandContext context, [RemainingText] string roleName)
        {
            await DataHelpers.Roles.DeleteSelfRole(context.Guild.Id, roleName);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Self Role **{roleName}** deleted!"));
        }
    }
}