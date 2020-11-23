using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles
{
    [Description("Self Role commands.")]
    [RequireGuild]
    public class RolesCommands : BaseCommandModule
    {
        private readonly DataHelpers DataHelper;

        public RolesCommands(DataHelpers dataHelper)
        {
            DataHelper = dataHelper;
        }

        [Command("JoinRole")]
        [Aliases("jr")]
        [Description("Joins the self role specified.")]
        public async Task JoinRole(CommandContext context, string roleName)
        {
            // Check role is a self role.
            if (!DataHelper.Roles.IsSelfRole(context.Guild.Id, roleName))
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
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.Member.Mention} joined **{roleName}**"));
        }

        [Command("LeaveRole")]
        [Aliases("lr")]
        [Description("Leaves the self role specified.")]
        public async Task LeaveRole(CommandContext context, string roleName)
        {
            // Check role is a self role.
            if (!DataHelper.Roles.IsSelfRole(context.Guild.Id, roleName))
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
            await context.RespondAsync(embed: EmbedGenerator.Success($"{context.User.Mention} left **{roleName}**"));
        }

        [Command("SelfRoles")]
        [Aliases("ViewSelfRoles")]
        [Description("Displays the available self roles on this server.")]
        public Task ViewSelfRoles(CommandContext context)
        {
            List<SelfRole> allRoles = DataHelper.Roles.GetSelfRoles(context.Guild.Id);
            if (allRoles == null || allRoles.Count == 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Warning("There are no self roles available.\nAsk your administrator to create some!"));
            }
            string prefix = DataHelper.Config.GetPrefix(context.Guild.Id);

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle("Available Self Roles")
                .WithDescription($"Use {prefix}JoinRole \"RoleName\" to join one of these roles.");

            foreach (SelfRole role in allRoles)
            {
                builder.AddField($"Name: {role.RoleName}", role.Description);
            }

            return context.RespondAsync(embed: builder.Build());
        }
    }
}