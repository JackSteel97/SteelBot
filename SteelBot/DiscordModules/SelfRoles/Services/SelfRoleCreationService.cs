using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.SelfRole;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Roles.Helpers;
using SteelBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles.Services;

public class SelfRoleCreationService
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly GuildsProvider _guildsProvider;
    private readonly ILogger<SelfRoleCreationService> _logger;
    private readonly SelfRolesProvider _selfRolesProvider;

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
        if (ValidateCreationRequest(request, out var discordRole))
            await CreateSelfRole(request, discordRole);
        else
            _logger.LogInformation("Request to create self role {RoleName} in Guild {GuildId} failed validation", request.RoleName, request.Member.Guild.Id);
    }

    public async ValueTask Remove(SelfRoleManagementAction request)
    {
        if (request.Action != SelfRoleActionType.Delete) throw new ArgumentException($"Unexpected management action send to {nameof(Remove)}");

        _logger.LogInformation("Request to remove self role {RoleName} in Guild {GuildId} received", request.RoleName, request.Member.Guild.Id);
        if (_selfRolesProvider.TryGetRole(request.Member.Guild.Id, request.RoleName, out var role))
        {
            await _selfRolesProvider.RemoveRole(request.Member.Guild.Id, role.DiscordRoleId);
            request.Responder.Respond(SelfRoleMessages.RoleRemovedSuccess(request.RoleName));
        }
        else
        {
            request.Responder.Respond(SelfRoleMessages.RoleDoesNotExist(request.RoleName));
        }
    }

    private async ValueTask CreateSelfRole(SelfRoleManagementAction request, DiscordRole discordRole)
    {
        if (_guildsProvider.TryGetGuild(request.Member.Guild.Id, out var guild))
        {
            var role = new SelfRole(discordRole.Id, request.RoleName, guild.RowId, request.Description);
            await _selfRolesProvider.AddRole(guild.DiscordId, role);

            request.Responder.Respond(SelfRoleMessages.RoleCreatedSuccess(discordRole.Mention));
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
            request.Responder.Respond(SelfRoleMessages.NoRoleNameProvided());
        else if (string.IsNullOrWhiteSpace(request.Description))
            request.Responder.Respond(SelfRoleMessages.NoRoleDescriptionProvided());
        else if (request.RoleName.Length > 255)
            request.Responder.Respond(SelfRoleMessages.RoleNameTooLong());
        else if (request.Description.Length > 255)
            request.Responder.Respond(SelfRoleMessages.RoleDescriptionTooLong());
        else
            valid = ValidateRoleAgainstServer(request, out discordRole);
        return valid;
    }

    private bool ValidateRoleAgainstServer(SelfRoleManagementAction request, out DiscordRole discordRole)
    {
        bool valid = false;
        discordRole = request.Member.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));
        if (discordRole == null)
            request.Responder.Respond(SelfRoleMessages.RoleNotCreatedYet());
        else if (_selfRolesProvider.BotKnowsRole(request.Member.Guild.Id, discordRole.Id))
            request.Responder.Respond(SelfRoleMessages.RoleAlreadyExists(discordRole.Mention));
        else
            valid = true;
        return valid;
    }
}