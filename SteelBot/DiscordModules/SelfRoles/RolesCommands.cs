using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Sentry;
using SteelBot.Helpers.Extensions;
using System;
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

        public RolesCommands(DataHelpers dataHelper, IHub sentry) : base(sentry)
        {
            DataHelpers = dataHelper;
        }

        [GroupCommand]
        [Description("Displays the available self roles on this server.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public Task ViewSelfRoles(CommandContext context)
        {
            DataHelpers.Roles.DisplayRoles(context);
            return Task.CompletedTask;
        }

        [Command("Join")]
        [Aliases("j")]
        [Description("Joins the self role specified.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public async Task JoinRole(CommandContext context, [RemainingText] string roleName)
        {
            if (roleName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                await DataHelpers.Roles.JoinAllRoles(context);
            }
            else
            {
                await DataHelpers.Roles.JoinRole(context, roleName);
            }
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
            await DataHelpers.Roles.LeaveRole(context, roleName);
        }

        [Command("Leave")]
        [Priority(10)]
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