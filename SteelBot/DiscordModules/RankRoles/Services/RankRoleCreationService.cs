using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.RankRole;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles.Services;
public class RankRoleCreationService
{
    private readonly RankRolesProvider _rankRolesProvider;
    private readonly GuildsProvider _guildsProvider;
    private readonly ILogger<RankRoleCreationService> _logger;
    private readonly UsersProvider _usersProvider;
    private readonly UserLockingService _userLockingService;
    private readonly LevelMessageSender _levelMessageSender;

    public RankRoleCreationService(RankRolesProvider rankRolesProvider,
        GuildsProvider guildsProvider,
        ILogger<RankRoleCreationService> logger,
        UsersProvider usersProvider,
        UserLockingService userLockingService,
        LevelMessageSender levelMessageSender)
    {
        _rankRolesProvider = rankRolesProvider;
        _guildsProvider = guildsProvider;
        _logger = logger;
        _usersProvider = usersProvider;
        _userLockingService = userLockingService;
        _levelMessageSender = levelMessageSender;
    }

    public async ValueTask Create(RankRoleManagementAction request)
    {
        _logger.LogInformation("Request to create Rank Role {RoleName} at {RequiredRank} in Guild {GuildId} received", request.GetRoleIdentifier(), request.RequiredRank, request.Guild.Id);
        if (Validate(request, out var discordRole))
        {
            var newRankRole = await CreateRole(request, discordRole);
            if (newRankRole != default)
            {
                var usersGainedRole = await AssignRoleToUsersAboveRequiredRank(request.Guild, discordRole, newRankRole);
                SendCreatedSuccessMessage(request, discordRole.Mention, newRankRole.LevelRequired, usersGainedRole);
            }
        }
    }

    private async Task<StringBuilder> AssignRoleToUsersAboveRequiredRank(DiscordGuild guild, DiscordRole newDiscordRole, RankRole newRankRole)
    {
        var usersGainedRole = new StringBuilder();
        using (await _userLockingService.WriteLockAllUsersAsync(guild.Id))
        {
            var allUsers = _usersProvider.GetUsersInGuild(guild.Id);
            foreach (var user in allUsers)
            {
                // Check the user is at or above the required level. And that their current rank role is a lower rank.
                if (user.CurrentLevel >= newRankRole.LevelRequired && (user.CurrentRankRole == default || newRankRole.LevelRequired > user.CurrentRankRole.LevelRequired))
                {
                    var member = await guild.GetMemberAsync(user.DiscordId);

                    if (user.CurrentRankRole != null)
                    {
                        // Remove any old rank role if one exists.
                        var discordRoleToRemove = guild.GetRole(user.CurrentRankRole.RoleDiscordId);
                        await member.RevokeRoleAsync(discordRoleToRemove, "A new higher level rank role was created");
                    }
                    await member.GrantRoleAsync(newDiscordRole, "New Rank Role created, this user already has the required rank");
                    await _usersProvider.UpdateRankRole(guild.Id, user.DiscordId, newRankRole);
                    _levelMessageSender.SendRankGrantedMessage(guild, member.Id, newRankRole, newDiscordRole.Mention);
                }
            }
        }
        return usersGainedRole;
    }

    private void SendCreatedSuccessMessage(RankRoleManagementAction request, string newRoleName, int newRoleRank, StringBuilder usersGainedRole)
    {
        string alreadyAchievedUsersSection = "";
        if (usersGainedRole.Length > 0)
        {
            alreadyAchievedUsersSection += "The following users have been awarded the new role:\n";
            alreadyAchievedUsersSection += usersGainedRole.ToString();
        }

        request.Responder.Respond(RankRoleMessages.RankRoleCreatedSuccess(newRoleName, newRoleRank, alreadyAchievedUsersSection));
    }

    private bool Validate(RankRoleManagementAction request, out DiscordRole discordRole)
    {
        discordRole = null;
        if (request.RoleId == default && string.IsNullOrWhiteSpace(request.RoleName))
        {
            request.Responder.Respond(RankRoleMessages.NoRoleNameProvided());
            return false;
        }

        discordRole = GetDiscordRole(request.Guild, request.RoleId, request.RoleName);
        if (discordRole == null)
        {
            request.Responder.Respond(RankRoleMessages.RoleDoesNotExistOnServer(request.RoleName));
            return false;
        }

        if (discordRole.Name.Length > 255)
        {
            request.Responder.Respond(RankRoleMessages.RoleNameTooLong());
            return false;
        }

        if (request.RequiredRank < 0)
        {
            request.Responder.Respond(RankRoleMessages.RequiredRankMustBePositive());
            return false;
        }

        if (_rankRolesProvider.BotKnowsRole(request.Guild.Id, discordRole.Id))
        {
            request.Responder.Respond(RankRoleMessages.RoleAlreadyExists());
            return false;
        }

        if (RoleExistsAtLevel(request.Guild.Id, request.RequiredRank, out string existingRoleName))
        {
            request.Responder.Respond(RankRoleMessages.RoleAlreadyExistsForLevel(request.RequiredRank, existingRoleName));
            return false;
        }

        return true;
    }

    private async ValueTask<RankRole> CreateRole(RankRoleManagementAction request, DiscordRole discordRole)
    {
        RankRole role = default;
        if (_guildsProvider.TryGetGuild(request.Guild.Id, out var guild))
        {
            role = new RankRole(discordRole.Id, discordRole.Name, guild.RowId, request.RequiredRank);
            await _rankRolesProvider.AddRole(request.Guild.Id, role);
        }
        return role;
    }

    private bool RoleExistsAtLevel(ulong guildId, int level, out string existingRoleName)
    {
        existingRoleName = null;
        if (_rankRolesProvider.TryGetGuildRankRoles(guildId, out var roles))
        {
            foreach (var role in roles)
            {
                if (role.LevelRequired == level)
                {
                    existingRoleName = role.RoleName;
                    return true;
                }
            }
        }
        return false;
    }

    private static DiscordRole GetDiscordRole(DiscordGuild guild, ulong roleId, string roleName)
    {
        return roleId != default
            ? guild.GetRole(roleId)
            : guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
}