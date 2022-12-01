using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Interactivity.Models;
using SteelBot.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Responders;

public class MessageResponder : IResponder
{
    private readonly DiscordClient _client;
    private readonly DiscordUser _user;
    private readonly DiscordChannel _channel;
    private readonly DiscordMessage _sourceMessage;
    private readonly ErrorHandlingService _errorHandlingService;

    public MessageResponder(CommandContext context, ErrorHandlingService errorHandlingService)
    {
        _client = context.Client;
        _user = context.User;
        _channel = context.Channel;
        _sourceMessage = context.Message;
        _errorHandlingService = errorHandlingService;
    }
    
    /// <inheritdoc />
    public async Task<DiscordMessage> RespondAsync(DiscordMessageBuilder messageBuilder, bool ephemeral = false)
    {
        return await RespondCore(messageBuilder);
    }

    /// <inheritdoc />
    public void Respond(DiscordMessageBuilder messageBuilder, bool ephemeral = false)
    {
        RespondCore(messageBuilder).FireAndForget(_errorHandlingService);
    }

    /// <inheritdoc />
    public async Task RespondPaginatedAsync(List<Page> pages)
    {
        await RespondPaginatedCore(pages);
    }

    /// <inheritdoc />
    public void RespondPaginated(List<Page> pages)
    {
        RespondPaginatedCore(pages).FireAndForget(_errorHandlingService);
    }

    /// <inheritdoc />
    public Task<(string selectionId, DiscordInteraction interaction)> RespondPaginatedWithComponents(List<PageWithSelectionButtons> pages)
    {
        return InteractivityHelper.SendPaginatedMessageWithComponentsAsync(_channel, _user, pages);
    }

    private Task<DiscordMessage> RespondCore(DiscordMessageBuilder messageBuilder)
    {
        return _sourceMessage.RespondAsync(messageBuilder);
    }

    private Task RespondPaginatedCore(List<Page> pages)
    {
        var interactivity = _client.GetInteractivity();
        return interactivity.SendPaginatedMessageAsync(_channel, _user, pages);
    }
}