using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.SelfRole;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Roles.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles.Services
{
    public class SelfRoleMembershipService
    {
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly SelfRolesProvider _selfRolesProvider;
        private readonly ILogger<SelfRoleMembershipService> _logger;

        public SelfRoleMembershipService(ErrorHandlingService errorHandlingService, SelfRolesProvider selfRolesProvider, ILogger<SelfRoleMembershipService> logger)
        {
            _errorHandlingService = errorHandlingService;
            _selfRolesProvider = selfRolesProvider;
            _logger = logger;
        }

        public async Task Join(SelfRoleManagementAction request)
        {
            if (request.Action != SelfRoleActionType.Join) throw new ArgumentException($"Unexpected management action send to {nameof(Join)}");

            _logger.LogInformation("Request for user {UserId} to join self role {RoleName} in Guild {GuildId} received", request.Member.Id, request.RoleName, request.Member.Guild.Id);

            if (ValidateRequest(request, out var discordRole)
                && ValidateUserDoesNotAlreadyHaveRole(request.Member, discordRole.Id, discordRole.Mention, request.RespondAsync))
            {
                await JoinRole(request.Member, discordRole);
                request.RespondAsync(SelfRoleMessages.JoinedRoleSuccess(request.Member.Mention, discordRole.Mention)).FireAndForget(_errorHandlingService);
            }
        }

        public async Task JoinAll(SelfRoleManagementAction request)
        {
            if (request.Action != SelfRoleActionType.JoinAll) throw new ArgumentException($"Unexpected management action send to {nameof(JoinAll)}");

            _logger.LogInformation("Request for user {UserId} to join All self roles in Guild {GuildId} received", request.Member.Id, request.Member.Guild.Id);

            if (_selfRolesProvider.TryGetGuildRoles(request.Member.Guild.Id, out var allSelfRoles))
            {
                var joinedRolesBuilder = await JoinRoles(request, allSelfRoles);
                if (joinedRolesBuilder.Length > 0)
                {
                    request.RespondAsync(SelfRoleMessages.JoinedRolesSuccess(joinedRolesBuilder)).FireAndForget(_errorHandlingService);
                }
                else
                {
                    request.RespondAsync(SelfRoleMessages.NoSelfRolesLeftToJoin()).FireAndForget(_errorHandlingService);
                }
            }
        }

        public async Task Leave(SelfRoleManagementAction request)
        {
            if (request.Action != SelfRoleActionType.Leave) throw new ArgumentException($"Unexpected management action send to {nameof(Leave)}");

            if(ValidateRequest(request, out var discordRole))
            {
                await LeaveRole(request.Member, discordRole);
                request.RespondAsync(SelfRoleMessages.LeftRoleSuccess(request.Member.Mention, discordRole.Mention)).FireAndForget(_errorHandlingService);
            }
        }

        private bool ValidateRequest(SelfRoleManagementAction request, out DiscordRole discordRole)
        {
            bool valid = true;
            discordRole = request.Member.Guild.Roles.Values.FirstOrDefault(role => role.Name.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));
            if (discordRole == default)
            {
                request.RespondAsync(SelfRoleMessages.RoleDoesNotExist(request.RoleName)).FireAndForget(_errorHandlingService);
                valid = false;
            }
            else if (!_selfRolesProvider.BotKnowsRole(request.Member.Guild.Id, discordRole.Id))
            {
                request.RespondAsync(SelfRoleMessages.InvalidRole(discordRole.Mention)).FireAndForget(_errorHandlingService);
                valid = false;
            }
            return valid;
        }

        private bool ValidateUserDoesNotAlreadyHaveRole(DiscordMember member, ulong discordRoleId, string roleMention, Func<DiscordMessageBuilder, Task> respondAsync)
        {
            if (member.Roles.Any(r => r.Id == discordRoleId))
            {
                respondAsync(SelfRoleMessages.AlreadyHasRole(roleMention)).FireAndForget(_errorHandlingService);
                return false;
            }

            return true;
        }

        private async Task JoinRole(DiscordMember member, DiscordRole role, string reason = "Requested to join Self Role")
        {
            _logger.LogInformation("User {UserId} joining role {RoleName} in Guild {GuildId}", member.Id, role.Name, member.Guild.Id);
            await member.GrantRoleAsync(role, reason);
        }

        private async Task<StringBuilder> JoinRoles(SelfRoleManagementAction request, List<SelfRole> allSelfRoles)
        {
            var builder = new StringBuilder();
            foreach (var selfRole in allSelfRoles)
            {
                var discordRole = request.Member.Guild.GetRole(selfRole.DiscordRoleId);
                if (discordRole != default)
                {
                    await JoinRole(request.Member, discordRole, "Requested to join All Self Roles");
                    string roleMention = discordRole.IsMentionable ? discordRole.Mention : discordRole.Name;
                    builder.AppendLine(Formatter.Bold(roleMention));
                }
                else
                {
                    builder.Append(Formatter.Bold(selfRole.RoleName)).AppendLine(" - is not a valid role on this server make sure the server role has not been deleted");
                }
            }

            return builder;
        }

        private async Task LeaveRole(DiscordMember member, DiscordRole role, string reason = "Requested to leave Self Role")
        {
            _logger.LogInformation("User {UserId} leaving role {RoleName} in Guild {GuildId}", member.Id, role.Name, member.Guild.Id);
            await member.RevokeRoleAsync(role, reason);
        }
    }
}
