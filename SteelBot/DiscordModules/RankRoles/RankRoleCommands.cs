using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
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

        private readonly ILogger<RankRoleCommands> Logger;

        public RankRoleCommands(DataHelpers dataHelpers, ILogger<RankRoleCommands> logger)
        {
            DataHelpers = dataHelpers;
            Logger = logger;
        }

        [GroupCommand]
        [Description("View the rank roles set up in this server.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public async Task ViewRankRoles(CommandContext context)
        {
            if (!DataHelpers.RankRoles.TryGetAllRankRolesInGuild(context.Guild.Id, out List<RankRole> allRoles))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("There are no Rank Roles currently set up for this server."));
                return;
            }

            // Sort ascending.
            allRoles.Sort((r1, r2) => r1.LevelRequired.CompareTo(r2.LevelRequired));
            var serverRoles = context.Guild.Roles.Values;
            StringBuilder allRolesBuilder = new StringBuilder();
            foreach (var role in allRoles)
            {
                var serverRole = serverRoles.FirstOrDefault(serverRole => serverRole.Name.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
                if (serverRole != default)
                {
                    string roleMention = serverRole.IsMentionable ? serverRole.Mention : serverRole.Name;
                    allRolesBuilder.Append("Level ").Append(Formatter.InlineCode(role.LevelRequired.ToString())).Append(" - ").AppendLine(roleMention);
                }
                else
                {
                    Logger.LogWarning($"The rank role [{role.RoleName}] in Guild [{context.Guild.Id}] does not have a corresponding role created on the server.");
                }
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
               .WithColor(EmbedGenerator.InfoColour)
               .WithTitle($"{context.Guild.Name} Rank Roles")
               .WithTimestamp(DateTime.UtcNow);

            var interactivity = context.Client.GetInteractivity();
            var rolesPages = interactivity.GeneratePagesInEmbed(allRolesBuilder.ToString(), SplitType.Line, embedBuilder);

            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, rolesPages);
        }

        [Command("Set")]
        [Aliases("Create", "srr")]
        [Description("Sets the given role as a rank role at the given level.")]
        [Cooldown(5, 60, CooldownBucketType.Guild)]
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
            if (DataHelpers.RankRoles.RoleExistsAtLevel(context.Guild.Id, requiredRank, out string existingRoleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"A rank role already exists for level {Formatter.InlineCode(requiredRank.ToString())} - {Formatter.Bold(existingRoleName)}, please delete the existing role."));
                return;
            }
            DiscordRole role = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == default)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"The Role **{roleName}** does not exist. You must create the role in the server first."));
                return;
            }

            await DataHelpers.RankRoles.CreateRankRole(context.Guild.Id, roleName, requiredRank);
            DataHelpers.RankRoles.TryGetRankRole(context.Guild.Id, roleName, out RankRole createdRole);

            // Assign the role to anyone already at or above this rank.
            var allUsers = DataHelpers.RankRoles.GetAllUsersInGuild(context.Guild.Id);
            StringBuilder usersGainedRole = new StringBuilder();
            foreach (var user in allUsers)
            {
                // Check the user is at or above the required level. And that their current rank role is a lower rank.
                if (user.CurrentLevel >= requiredRank && (user.CurrentRankRole == default || requiredRank > user.CurrentRankRole.LevelRequired))
                {
                    var member = await context.Guild.GetMemberAsync(user.DiscordId);

                    if (user.CurrentRankRole != default)
                    {
                        // Remove any old rank role if one exists.
                        DiscordRole discordRoleToRemove = context.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(user.CurrentRankRole.RoleName, StringComparison.OrdinalIgnoreCase));
                        await member.RevokeRoleAsync(discordRoleToRemove, "A new rank role was created that overwrites this one for this user.");
                    }

                    await member.GrantRoleAsync(role, "New Rank Role created - This user already has the required rank");
                    await DataHelpers.RankRoles.UpdateRankRole(context.Guild.Id, user.DiscordId, createdRole);
                    usersGainedRole.AppendLine(member.Mention);
                }
            }

            string alreadyAchievedUsersSection = "";
            if (usersGainedRole.Length > 0)
            {
                alreadyAchievedUsersSection += "The following users have been awarded the new role:\n";
                alreadyAchievedUsersSection += usersGainedRole.ToString();
            }

            await context.RespondAsync(embed: EmbedGenerator.Success($"**{role.Name}** Set as a Rank Role for Rank **{requiredRank}**\n\n{alreadyAchievedUsersSection}", "Rank Role Created!"));
        }

        [Command("Remove")]
        [Aliases("Delete", "rrr")]
        [Description("Removes the given role from the list of rank roles, users will no longer be granted the role when they reach the required level.")]
        [Cooldown(5, 60, CooldownBucketType.Guild)]
        public async Task RemoveSelfRole(CommandContext context, [RemainingText] string roleName)
        {
            await DataHelpers.RankRoles.DeleteRankRole(context.Guild.Id, roleName, context.Guild);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Rank Role **{roleName}** deleted!"));
        }
    }
}