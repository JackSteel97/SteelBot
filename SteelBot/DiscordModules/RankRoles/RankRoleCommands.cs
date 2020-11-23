using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    [Description("Rank role management commands")]
    [RequireGuild]
    public class RankRoleCommands
    {
        private readonly DataHelpers DataHelpers;

        public RankRoleCommands(DataHelpers dataHelpers)
        {
            DataHelpers = dataHelpers;
        }

        [Command("SetRankRole")]
        [Aliases("CreateSelfRole", "srr")]
        [Description("Sets the given role as a rank role at the given level.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetRankRole(CommandContext context, string roleName, int requiredRank)
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
            if (requiredRank < 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The required rank must be postive."));
                return;
            }
            if (!context.Guild.Roles.Values.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("You must create the role in the server first."));
                return;
            }

            await DataHelpers.RankRoles.CreateSelfRole(context.Guild.Id, roleName, requiredRank);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Rank Role **{roleName}** created!"));

            // TODO: Assign the role to anyone already at or above this rank.
        }
    }
}