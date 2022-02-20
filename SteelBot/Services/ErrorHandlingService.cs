using DSharpPlus;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Services.Configuration;
using System;
using System.Threading.Tasks;

namespace SteelBot.Services
{
    public class ErrorHandlingService
    {
        private readonly DiscordClient Client;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly DataCache Cache;
        private readonly ILogger<ErrorHandlingService> Logger;

        public ErrorHandlingService(DiscordClient client, AppConfigurationService appConfigurationService, DataCache cache, ILogger<ErrorHandlingService> logger)
        {
            Client = client;
            AppConfigurationService = appConfigurationService;
            Cache = cache;
            Logger = logger;
        }

        public async Task Log(Exception e, string source)
        {
            Logger.LogError(e, $"Source Method: [{source}]");
            await Cache.Exceptions.InsertException(new ExceptionLog(e, source));
            await SendMessageToJack(e, source);
        }

        private async Task SendMessageToJack(Exception e, string source)
        {
            ulong civlationId = AppConfigurationService.Application.CommonServerId;
            ulong jackId = AppConfigurationService.Application.CreatorUserId;

            var commonServer = await Client.GetGuildAsync(civlationId);
            var jack = await commonServer.GetMemberAsync(jackId);

            await jack.SendMessageAsync(embed: EmbedGenerator.Info($"Error Message:\n{Formatter.BlockCode(e.Message)}\nAt:\n{Formatter.InlineCode(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"))}\n\nStack Trace:\n{Formatter.BlockCode(e.StackTrace)}", "An Error Occured", $"Source: {source}"));
        }
    }
}
