using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Interactivity.Models;
using SteelBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Responders;

public class InteractionResponder : IResponder
{
    private readonly BaseContext _context;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly DiscordClient _client;
    private readonly DiscordUser _user;
    private readonly DiscordChannel _channel;
    private readonly DiscordInteraction _interaction;

    public InteractionResponder(BaseContext context, ErrorHandlingService errorHandlingService)
    {
        _context = context;
        _errorHandlingService = errorHandlingService;
        _client = _context.Client;
        _user = context.User;
        _channel = context.Channel;
        _interaction = context.Interaction;
    }

    /// <inheritdoc />
    public Task<DiscordMessage> RespondAsync(DiscordMessageBuilder messageBuilder, bool ephemeral = false)
    {
        return RespondCore(messageBuilder, ephemeral);
    }
    
    /// <inheritdoc />
    public void Respond(DiscordMessageBuilder messageBuilder, bool ephemeral = false)
    {
        RespondCore(messageBuilder, ephemeral).FireAndForget(_errorHandlingService);
    }

    /// <inheritdoc />
    public async Task RespondPaginatedAsync(List<Page> pages) => await RespondPaginatedCore(pages);

    /// <inheritdoc />
    public void RespondPaginated(List<Page> pages) => RespondPaginatedCore(pages).FireAndForget(_errorHandlingService);

    /// <inheritdoc />
    public Task<(string selectionId, DiscordInteraction interaction)> RespondPaginatedWithComponents(List<PageWithSelectionButtons> pages)
    {
        return InteractivityHelper.SendPaginatedMessageWithComponentsAsync(_channel, _user, pages);
    }

    private async Task<DiscordMessage> RespondCore(DiscordMessageBuilder messageBuilder, bool ephemeral)
    {
        var interactionResponse = new DiscordInteractionResponseBuilder(messageBuilder).AsEphemeral(ephemeral);
        foreach (var file in messageBuilder.Files)
        {
            interactionResponse.AddFile(file.FileName, file.Stream);
        }
        await _context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, interactionResponse);
        return await _context.GetOriginalResponseAsync();
    }

    private Task RespondPaginatedCore(List<Page> pages)
    {
        var interactivity = _client.GetInteractivity();
        return interactivity.SendPaginatedResponseAsync(_interaction, false, _user, pages);
    }
}