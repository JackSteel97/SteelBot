using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using SteelBot.Helpers.Extensions;
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
    public async Task RespondAsync(DiscordMessageBuilder messageBuilder)
    {
        await RespondCore(messageBuilder);
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