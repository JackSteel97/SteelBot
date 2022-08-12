using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Interactivity.Models;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Responders;

public class InteractionResponder : IResponder
{
    private readonly InteractionContext _interactionContext;
    private readonly ErrorHandlingService _errorHandlingService;

    public InteractionResponder(InteractionContext interactionContext, ErrorHandlingService errorHandlingService)
    {
        _interactionContext = interactionContext;
        _errorHandlingService = errorHandlingService;
    }

    /// <inheritdoc />
    public async Task<DiscordMessage> RespondAsync(DiscordMessageBuilder messageBuilder)
    {
        await RespondCore(messageBuilder);
        
        // TODO: Solve this - the interface wants a message but we don't deal with messages like this in interactions.
        return null;
    }
    
    /// <inheritdoc />
    public void Respond(DiscordMessageBuilder messageBuilder)
    {
        RespondCore(messageBuilder).FireAndForget(_errorHandlingService);
    }

    /// <inheritdoc />
    public async Task RespondPaginatedAsync(List<Page> pages) => await RespondPaginatedCore(pages);

    /// <inheritdoc />
    public void RespondPaginated(List<Page> pages) => RespondPaginatedCore(pages).FireAndForget(_errorHandlingService);

    /// <inheritdoc />
    public Task<(string selectionId, DiscordInteraction interaction)> RespondPaginatedWithComponents(List<PageWithSelectionButtons> pages) => throw new NotImplementedException();

    private Task RespondCore(DiscordMessageBuilder messageBuilder)
    {
        var interactionResponse = new DiscordInteractionResponseBuilder(messageBuilder);
        return _interactionContext.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, interactionResponse);
    }

    private Task RespondPaginatedCore(List<Page> pages)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
}