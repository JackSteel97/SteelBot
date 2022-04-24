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
    public class SelfRoleCreationService
    {
        private readonly ErrorHandlingService _errorHandlingService;
        private readonly SelfRolesProvider _selfRolesProvider;
        private readonly GuildsProvider _guildsProvider;
        private readonly ILogger<SelfRoleCreationService> _logger;

        public SelfRoleCreationService(ErrorHandlingService errorHandlingService,
            SelfRolesProvider selfRolesProvider,
            GuildsProvider guildsProvider,
            ILogger<SelfRoleCreationService> logger)
        {
            _errorHandlingService = errorHandlingService;
            _selfRolesProvider = selfRolesProvider;
            _guildsProvider = guildsProvider;
            _logger = logger;
        }

        public async ValueTask Create(SelfRoleManagementAction request)
        {
            if (request.Action != SelfRoleActionType.Create) throw new ArgumentException($"Unexpected management action send to {nameof(Create)}");

            _logger.LogInformation("Request to create self role {RoleName} in Guild {GuildId} received", request.RoleName, request.Member.Guild.Id);
            if (ValidateCreationRequest(request, out var discordRoleMention))
            {
                await CreateSelfRole(request, discordRoleMention);
            }
            else
            {
                _logger.LogInformation("Request to create self role {RoleName} in Guild {GuildId} failed validation", request.RoleName, request.Member.Guild.Id);
            }
        }

        public async ValueTask Remove(SelfRoleManagementAction request)
        {
            if (request.Action != SelfRoleActionType.Delete) throw new ArgumentException($"Unexpected management action send to {nameof(Create)}");

            _logger.LogInformation("Request to remove self role {RoleName} in Guild {GuildId} received", request.RoleName, request.Member.Guild.Id);
            if(_selfRolesProvider.TryGetRole(request.Member.Guild.Id, request.RoleName, out var role))
            {
                await _selfRolesProvider.RemoveRole(request.Member.Guild.Id, role.DiscordRoleId);
                request.RespondAsync(SelfRoleMessages.RoleRemovedSuccess(request.RoleName)).FireAndForget(_errorHandlingService);
            }
            else
            {
                request.RespondAsync(SelfRoleMessages.RoleDoesNotExist(request.RoleName)).FireAndForget(_errorHandlingService);
            }
        }

        private async ValueTask CreateSelfRole(SelfRoleManagementAction request, DiscordRole discordRole)
        {
            if (_guildsProvider.TryGetGuild(request.Member.Guild.Id, out var guild))
            {
                var role = new SelfRole(discordRole.Id, request.RoleName, guild.RowId, request.Description);
                await _selfRolesProvider.AddRole(guild.DiscordId, role);

                var roleMention = GetMention(discordRole);
                request.RespondAsync(SelfRoleMessages.RoleCreatedSuccess(roleMention)).FireAndForget(_errorHandlingService);
            }
            else
            {
                _logger.LogWarning("Could not create self role {RoleName} because Guild {GuildId} does not exist", request.RoleName, request.Member.Guild.Id);
            }
        }

        private bool ValidateCreationRequest(SelfRoleManagementAction request, out DiscordRole discordRole)
        {
            discordRole = null;
            bool valid = false;
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                request.RespondAsync(SelfRoleMessages.NoRoleNameProvided()).FireAndForget(_errorHandlingService);
            }
            else if (string.IsNullOrWhiteSpace(request.Description))
            {
                request.RespondAsync(SelfRoleMessages.NoRoleDescriptionProvided()).FireAndForget(_errorHandlingService);
            }
            else if (request.RoleName.Length > 255)
            {
                request.RespondAsync(SelfRoleMessages.RoleNameTooLong()).FireAndForget(_errorHandlingService);
            }
            else if (request.Description.Length > 255)
            {
                request.RespondAsync(SelfRoleMessages.RoleDescriptionTooLong()).FireAndForget(_errorHandlingService);
            }
            else
            {
                valid = ValidateRoleAgainstServer(request, out discordRole);
            }
            return valid;
        }

        private bool ValidateRoleAgainstServer(SelfRoleManagementAction request, out DiscordRole discordRole)
        {
            bool valid = false;
            discordRole = request.Member.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));
            if (discordRole == null)
            {
                request.RespondAsync(SelfRoleMessages.RoleNotCreatedYet()).FireAndForget(_errorHandlingService);
            }
            else if (_selfRolesProvider.BotKnowsRole(request.Member.Guild.Id, discordRole.Id))
            {
                var discordRoleMention = GetMention(discordRole);
                request.RespondAsync(SelfRoleMessages.RoleAlreadyExists(discordRoleMention));
            }
            else
            {
                valid = true;
            }
            return valid;
        }

        private static string GetMention(DiscordRole role)
        {
            return role.IsMentionable? role.Mention: role.Name;
        }
    }
}
