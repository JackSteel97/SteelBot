using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.RankRole;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.RankRoles.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;

namespace SteelBot.DiscordModules.RankRoles.Services;

public class RankRoleViewingService
{
    private readonly DiscordClient _discordClient;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<RankRoleViewingService> _logger;
    private readonly RankRolesProvider _rankRolesProvider;

    public RankRoleViewingService(ILogger<RankRoleViewingService> logger, RankRolesProvider rankRolesProvider, ErrorHandlingService errorHandlingService, DiscordClient discordClient)
    {
        _rankRolesProvider = rankRolesProvider;
        _errorHandlingService = errorHandlingService;
        _discordClient = discordClient;
        _logger = logger;
    }

    public void View(RankRoleManagementAction request)
    {
        _logger.LogInformation("User {UserId} requested to view the Rank Roles in Guild {GuildId}", request.Member.Id, request.Guild.Id);
        if (!_rankRolesProvider.TryGetGuildRankRoles(request.Guild.Id, out var guildRoles) || guildRoles.Count == 0)
        {
            request.Responder.Respond(RankRoleMessages.NoRankRolesForThisServer());
            return;
        }

        // Sort ascending.
        guildRoles.Sort((r1, r2) => r1.LevelRequired.CompareTo(r2.LevelRequired));

        var embedBuilder = new DiscordEmbedBuilder()
            .WithColor(EmbedGenerator.InfoColour)
            .WithTitle($"{request.Guild.Name} Rank Roles")
            .WithTimestamp(DateTime.UtcNow);

        var interactivity = _discordClient.GetInteractivity();

        var rolesPages = PaginationHelper.GenerateEmbedPages(embedBuilder,
            guildRoles,
            20,
            (builder, item, _) => builder.Append("Level ").Append(Formatter.InlineCode(item.LevelRequired.ToString())).Append(" - ").AppendLine(item.RoleDiscordId.ToRoleMention()));

        request.Responder.RespondPaginated(rolesPages);
    }
}