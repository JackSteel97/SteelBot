using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using SteelBot.Channels.SelfRole;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.Services;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Roles;

public class RolesDataHelper
{
    private readonly SelfRoleViewingService _selfRoleViewingService;

    public RolesDataHelper(SelfRoleViewingService selfRoleViewingService)
    {
        _selfRoleViewingService = selfRoleViewingService;
    }

    // TODO: Move to channel.
    public void DisplayRoles(CommandContext context) => _selfRoleViewingService.DisplaySelfRoles(context);
}