﻿using DSharpPlus;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Exceptions;
using SteelBot.Helpers;
using SteelBot.Helpers.Sentry;
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
        private readonly IHub _sentry;

        public ErrorHandlingService(DiscordClient client, AppConfigurationService appConfigurationService, DataCache cache, ILogger<ErrorHandlingService> logger, IHub sentry)
        {
            Client = client;
            AppConfigurationService = appConfigurationService;
            Cache = cache;
            Logger = logger;
            _sentry = sentry;
        }

        public async Task Log(Exception e, string source)
        {
            try
            {
                var transaction = _sentry.GetCurrentTransaction();

                Logger.LogError(e, "Source Method: {Source}", source);
                if (e.InnerException != null)
                {
                    await Cache.Exceptions.InsertException(new ExceptionLog(e.InnerException, source));
                    await SendMessageToJack(e.InnerException, source);
                }

                await Cache.Exceptions.InsertException(new ExceptionLog(e, source));

                if (e is not FireAndForgetTaskException)
                {
                    await SendMessageToJack(e, source);
                }

                if (transaction != null)
                {
                    transaction.Finish(e);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while attempting to log an error");
            }
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
