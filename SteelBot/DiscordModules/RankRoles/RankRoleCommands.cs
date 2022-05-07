﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers.Extensions;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    [Group("RankRoles")]
    [Aliases("rr")]
    [Description("Rank role management commands")]
    [RequireGuild]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public class RankRoleCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelpers;

        public RankRoleCommands(DataHelpers dataHelpers)
        {
            DataHelpers = dataHelpers;
        }

        [GroupCommand]
        [Description("View the rank roles set up in this server.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task ViewRankRoles(CommandContext context)
        {
            await DataHelpers.RankRoles.ViewRankRoles(context);
        }

        [Command("Set")]
        [Aliases("Create", "srr")]
        [Description("Sets the given role as a rank role at the given level.")]
        [Cooldown(5, 60, CooldownBucketType.Guild)]
        public async Task SetRankRole(CommandContext context, int requiredRank, [RemainingText] string roleName)
        {
            await DataHelpers.RankRoles.CreateRankRole(context, roleName, requiredRank);
        }

        [Command("Set")]
        [Priority(10)]
        public async Task SetRankRole(CommandContext context, int requiredRank, DiscordRole role)
        {
            await DataHelpers.RankRoles.CreateRankRole(context, role, requiredRank);
        }

        [Command("Remove")]
        [Aliases("Delete", "rrr")]
        [Description("Removes the given role from the list of rank roles, users will no longer be granted the role when they reach the required level.")]
        [Cooldown(5, 60, CooldownBucketType.Guild)]
        public async Task RemoveSelfRole(CommandContext context, [RemainingText] string roleName)
        {
            await DataHelpers.RankRoles.DeleteRankRole(context, roleName);
        }

        [Command("Remove")]
        [Priority(10)]
        public async Task RemoveSelfRole(CommandContext context, DiscordRole role)
        {
            await DataHelpers.RankRoles.DeleteRankRole(context, role);
        }
    }
}