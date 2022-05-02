using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Roles.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteelBot.DiscordModules.Roles.Services;

public class SelfRoleViewingService
{
    private readonly ConfigDataHelper _configDataHelper;
    private readonly SelfRolesProvider _selfRolesProvider;
    private readonly ErrorHandlingService _errorHandlingService;

    public SelfRoleViewingService(ConfigDataHelper configDataHelper,
        SelfRolesProvider selfRolesProvider,
        ErrorHandlingService errorHandlingService)
    {
        _configDataHelper = configDataHelper;
        _selfRolesProvider = selfRolesProvider;
        _errorHandlingService = errorHandlingService;
    }

    public void DisplaySelfRoles(CommandContext context)
    {
        if (!_selfRolesProvider.TryGetGuildRoles(context.Guild.Id, out var allRoles))
        {
            context.RespondAsync(SelfRoleMessages.NoSelfRolesAvailable()).FireAndForget(_errorHandlingService);
            return;
        }

        string prefix = _configDataHelper.GetPrefix(context.Guild.Id);

        var builder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle("Available Self Roles");

        var rolesBuilder = new StringBuilder();
        rolesBuilder.AppendLine(Formatter.Bold("All")).AppendLine(" - Join all available Self Roles");

        AppendSelfRoles(context, allRoles, rolesBuilder);

        builder.WithDescription($"Use `{prefix}SelfRoles Join \"RoleName\"` to join one of these roles.\n\n{rolesBuilder}");
        context.RespondAsync(embed: builder.Build()).FireAndForget(_errorHandlingService);
    }

    private static void AppendSelfRoles(CommandContext context, List<Database.Models.SelfRole> allRoles, StringBuilder rolesBuilder)
    {
        foreach (var role in allRoles)
        {
            var discordRole = context.Guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
            string roleMention = role.RoleName;
            if (discordRole != default)
            {
                roleMention = discordRole.Mention;
            }
            rolesBuilder.AppendLine(Formatter.Bold(roleMention));
            rolesBuilder.Append(" - ").AppendLine(role.Description);
        }
    }
}
