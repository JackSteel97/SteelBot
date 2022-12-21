using DSharpPlus;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Exceptions;
using SteelBot.Helpers;
using SteelBot.Services.Configuration;
using System;
using System.Threading.Tasks;

namespace SteelBot.Services;

public class ErrorHandlingService
{
    private readonly DiscordClient _client;
    private readonly AppConfigurationService _appConfigurationService;
    private readonly DataCache _cache;
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(DiscordClient client, AppConfigurationService appConfigurationService, DataCache cache, ILogger<ErrorHandlingService> logger)
    {
        _client = client;
        _appConfigurationService = appConfigurationService;
        _cache = cache;
        _logger = logger;
    }

    public async Task Log(Exception e, string source)
    {
        try
        {
            _logger.LogError(e, "Source Method: {Source}", source);
            if (e.InnerException != null)
            {
                await _cache.Exceptions.InsertException(new ExceptionLog(e.InnerException, source));
                await SendMessageToJack(e.InnerException, source);
            }

            await _cache.Exceptions.InsertException(new ExceptionLog(e, source));

            if (e is not FireAndForgetTaskException)
            {
                await SendMessageToJack(e, source);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to log an error");
        }
    }

    private async Task SendMessageToJack(Exception e, string source)
    {
        ulong civlationId = _appConfigurationService.Application.CommonServerId;
        ulong jackId = _appConfigurationService.Application.CreatorUserId;

        var commonServer = await _client.GetGuildAsync(civlationId);
        var jack = await commonServer.GetMemberAsync(jackId);

        await jack.SendMessageAsync(embed: EmbedGenerator.Info($"Error Message:\n{Formatter.BlockCode(e.Message)}\nAt:\n{Formatter.InlineCode(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"))}\n\nStack Trace:\n{Formatter.BlockCode(e.StackTrace)}", "An Error Occured", $"Source: {source}"));
    }
}