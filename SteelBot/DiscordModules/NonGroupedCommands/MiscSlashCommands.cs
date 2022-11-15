using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.NonGroupedCommands;

public class MiscSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly AppConfigurationService _appConfigurationService;
    private readonly ErrorHandlingService _errorHandlingService;

    public MiscSlashCommands(AppConfigurationService appConfigurationService, ErrorHandlingService errorHandlingService, IHub sentry, ILogger<MiscSlashCommands> logger) : base(
        nameof(MiscSlashCommands), logger, sentry)
    {
        _appConfigurationService = appConfigurationService;
        _errorHandlingService = errorHandlingService;
    }

    [SlashCommand("invite", "Get a link to invite this bot to your own server")]
    public async Task Invite(InteractionContext context)
    {
        var response = new DiscordInteractionResponseBuilder()
            .AddComponents(Interactions.Links.ExternalLink(_appConfigurationService.Application.InviteLink, "Invite me to your own server!"));
        await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }

    [SlashCommand("link", "Display a link as a button")]
    public Task Link(InteractionContext context,
        [Option("longlink", "The link to display as a button link")]
        string longLink,
        [Option("linktext", "The text for the link button")]
        string linkText)
    {
        if (!longLink.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            longLink = $"https://{longLink}";
        }

        if (!Uri.IsWellFormedUriString(longLink, UriKind.Absolute))
        {
            context.SendWarning($"{Formatter.Bold(longLink)} is not a valid URL", _errorHandlingService);
            return Task.CompletedTask;
        }

        if (linkText.Length > 50)
        {
            context.SendWarning("Link Text cannot be longer than 50 characters", _errorHandlingService);
            return Task.CompletedTask;
        }

        var response = new DiscordInteractionResponseBuilder()
            .AddComponents(Interactions.Links.ExternalLink(longLink, linkText));
        context.SendMessage(response, _errorHandlingService);
        return Task.CompletedTask;
    }
}