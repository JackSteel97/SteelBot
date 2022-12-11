using DSharpPlus.Entities;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.Responders;

namespace SteelBot.DiscordModules.Roles;

public class RolesDataHelper
{
    private readonly SelfRoleViewingService _selfRoleViewingService;

    public RolesDataHelper(SelfRoleViewingService selfRoleViewingService)
    {
        _selfRoleViewingService = selfRoleViewingService;
    }

    // TODO: Move to channel.
    public void DisplayRoles(DiscordGuild guild, IResponder responder) => _selfRoleViewingService.DisplaySelfRoles(guild, responder);
}