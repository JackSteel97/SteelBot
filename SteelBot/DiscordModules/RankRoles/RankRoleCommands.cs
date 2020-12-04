using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    [Group("RankRoles")]
    [Aliases("rr")]
    [Description("Rank role management commands")]
    [RequireGuild]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public class RankRoleCommands : BaseCommandModule
    {
        private readonly DataHelpers DataHelpers;

        public RankRoleCommands(DataHelpers dataHelpers)
        {
            DataHelpers = dataHelpers;
        }

        [Command("SetRankRole")]
        [Aliases("CreateRankRole", "srr")]
        [Description("Sets the given role as a rank role at the given level.")]
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
            if (DataHelpers.RankRoles.RoleExists(context.Guild.Id, roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("This rank role already exists, please delete the existing role first."));
                return;
            }
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"The Role **{roleName}** does not exist. You must create the role in the server first."));
                return;
            }

            await DataHelpers.RankRoles.CreateRankRole(context.Guild.Id, roleName, requiredRank);

            // Assign the role to anyone already at or above this rank.
            var allUsers = DataHelpers.RankRoles.GetAllUsersInGuild(context.Guild.Id);
            StringBuilder usersGainedRole = new StringBuilder();
            foreach (var user in allUsers)
            {
                if (user.CurrentLevel >= requiredRank)
                {
                    var member = await context.Guild.GetMemberAsync(user.DiscordId);
                    await member.GrantRoleAsync(role, "New Rank Role created - This user already has the required rank");
                    usersGainedRole.AppendLine(member.Mention);
                }
            }

            string alreadyAchievedUsersSection = "";
            if (usersGainedRole.Length > 0)
            {
                alreadyAchievedUsersSection += "The following users have been awarded the new role:\n";
                alreadyAchievedUsersSection += usersGainedRole.ToString();
            }

            await context.RespondAsync(embed: EmbedGenerator.Info($"**{role.Name}** Set as a Rank Role for Rank **{requiredRank}**\n\n{alreadyAchievedUsersSection}", "Rank Role Created!"));
        }

        [Command("RemoveRankRole")]
        [Aliases("DeleteRankRole", "rrr")]
        [Description("Removes the given role from the list of rank roles, users will no longer be granted the role when they reach the required level.")]
        public async Task RemoveSelfRole(CommandContext context, string roleName)
        {
            await DataHelpers.RankRoles.DeleteRankRole(context.Guild.Id, roleName);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Rank Role **{roleName}** deleted!"));
        }
    }
}