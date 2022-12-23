using DSharpPlus;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Roles.Helpers;
using SteelBot.Helpers;
using SteelBot.Responders;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteelBot.DiscordModules.Roles.Services;

public class SelfRoleViewingService
{
    private readonly ConfigDataHelper _configDataHelper;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly SelfRolesProvider _selfRolesProvider;

    public SelfRoleViewingService(ConfigDataHelper configDataHelper,
        SelfRolesProvider selfRolesProvider,
        ErrorHandlingService errorHandlingService)
    {
        _configDataHelper = configDataHelper;
        _selfRolesProvider = selfRolesProvider;
        _errorHandlingService = errorHandlingService;
    }

    public void DisplaySelfRoles(DiscordGuild guild, IResponder responder)
    {
        if (!_selfRolesProvider.TryGetGuildRoles(guild.Id, out var allRoles))
        {
            responder.Respond(SelfRoleMessages.NoSelfRolesAvailable());
            return;
        }

        string prefix = _configDataHelper.GetPrefix(guild.Id);

        var builder = new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle("Available Self Roles");

        var rolesBuilder = new StringBuilder();
        rolesBuilder.Append(Formatter.Bold("All")).AppendLine(" - Join all available Self Roles");

        AppendSelfRoles(guild, allRoles, rolesBuilder);

        builder.WithDescription($"Use `{prefix}SelfRoles Join \"RoleName\"` to join one of these roles.\n\n{rolesBuilder}");
        responder.Respond(new DiscordMessageBuilder().AddEmbed(builder.Build()));
    }

    private static void AppendSelfRoles(DiscordGuild guild, List<SelfRole> allRoles, StringBuilder rolesBuilder)
    {
        foreach (var role in allRoles)
        {
            var discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name.Equals(role.RoleName, StringComparison.OrdinalIgnoreCase));
            string roleMention = role.RoleName;
            if (discordRole != default) roleMention = discordRole.Mention;
            rolesBuilder.Append(Formatter.Bold(roleMention));
            rolesBuilder.Append(" - ").AppendLine(role.Description);
        }
    }
}