using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Utility;
using SteelBot.Helpers;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot
{
    public class BotMain : IHostedService
    {
        private readonly AppConfigurationService AppConfigurationService;
        private readonly ILogger<BotMain> Logger;
        private readonly DiscordClient Client;
        private readonly IServiceProvider ServiceProvider;
        private readonly DataHelpers DataHelpers;
        private readonly DataCache Cache;
        private CommandsNextExtension Commands;

        public BotMain(AppConfigurationService appConfigurationService, ILogger<BotMain> logger, DiscordClient client, IServiceProvider serviceProvider, DataHelpers dataHelpers, DataCache cache)
        {
            AppConfigurationService = appConfigurationService;
            Logger = logger;
            Client = client;
            ServiceProvider = serviceProvider;
            DataHelpers = dataHelpers;
            Cache = cache;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation($"Starting Bot Version {AppConfigurationService.Version} ({AppConfigurationService.Environment})");

            Console.CancelKeyPress += async (s, a) => await ShutdownDiscordClient();

            Logger.LogInformation("Initialising Command Modules");
            InitCommands();
            Logger.LogInformation("Initialising Interactivity");
            InitInteractivity();
            Logger.LogInformation("Initialising Event Handlers");
            InitHandlers();

            Logger.LogInformation("Starting Client");
            return Client.ConnectAsync(new DiscordActivity("+Help", ActivityType.ListeningTo));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return ShutdownDiscordClient();
        }

        private async Task ShutdownDiscordClient()
        {
            Logger.LogInformation("Disconnecting Client");
            await Client.DisconnectAsync();
        }

        private void InitHandlers()
        {
            Client.MessageReactionAdded += HandleReactionAdded;
            Client.MessageCreated += HandleMessageCreated;
            Client.VoiceStateUpdated += HandleVoiceStateChange;
            Client.GuildCreated += HandleJoiningGuild;

            Commands.CommandErrored += HandleCommandErrored;
        }

        private void InitCommands()
        {
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = ServiceProvider,
                PrefixResolver = ResolvePrefix
            });

            Commands.RegisterCommands<ConfigCommands>();
            Commands.RegisterCommands<PollsCommands>();
            Commands.RegisterCommands<RolesCommands>();
            Commands.RegisterCommands<StatsCommands>();
            Commands.RegisterCommands<UtilityCommands>();
        }

        private void InitInteractivity()
        {
            // Enable interactivity and set default options.
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // Default pagination behaviour to just ignore the reactions.
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                // Default timeout for other actions to 2 minutes.
                Timeout = TimeSpan.FromMinutes(2)
            });
        }

        private Task<int> ResolvePrefix(DiscordMessage msg)
        {
            string guildsPrefix = DataHelpers.Config.GetPrefix(msg.Channel.GuildId);

            int prefixFound = CommandsNextUtilities.GetStringPrefixLength(msg, guildsPrefix);
            if (prefixFound == -1)
            {
                prefixFound = CommandsNextUtilities.GetMentionPrefixLength(msg, Client.CurrentUser);
            }
            return Task.FromResult(prefixFound);
        }

        private async Task HandleReactionAdded(DiscordClient client, MessageReactionAddEventArgs args)
        {
            try
            {
                if (args.Guild != null)
                {
                    await DataHelpers.UserTracking.TrackUser(args.Guild.Id, args.User.Id);

                    // Polls are not supported outside of guilds.
                    await DataHelpers.Polls.HandleMessageReaction(args, Client.CurrentUser.Id);
                }
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleReactionAdded));
            }
        }

        private async Task HandleMessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            try
            {
                if (args.Guild != null)
                {
                    await DataHelpers.UserTracking.TrackUser(args.Guild.Id, args.Author.Id);
                    await DataHelpers.Stats.HandleNewMessage(args);
                }
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleMessageCreated));
            }
        }

        private async Task HandleVoiceStateChange(DiscordClient client, VoiceStateUpdateEventArgs args)
        {
            try
            {
                await DataHelpers.UserTracking.TrackUser(args.Guild.Id, args.User.Id);

                await DataHelpers.Stats.HandleVoiceStateChange(args);
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleVoiceStateChange));
            }
        }

        private async Task HandleJoiningGuild(DiscordClient client, GuildCreateEventArgs args)
        {
            try
            {
                Guild joinedGuild = new Guild(args.Guild.Id);
                await Cache.Guilds.UpsertGuild(joinedGuild);
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleJoiningGuild));
            }
        }

        private async Task HandleCommandErrored(CommandsNextExtension ext, CommandErrorEventArgs args)
        {
            if (args.Exception is ChecksFailedException ex)
            {
                if (ex.FailedChecks.Count > 0)
                {
                    CheckBaseAttribute failedCheck = ex.FailedChecks[0];
                    if (failedCheck != null)
                    {
                        if (failedCheck is CooldownAttribute cooldown)
                        {
                            await args.Context.Member.SendMessageAsync(embed: EmbedGenerator
                                .Warning($"The `{args.Command.QualifiedName}` command can only be executed **{"time".ToQuantity(cooldown.MaxUses)}** every **{cooldown.Reset.Humanize()}**"));
                            return;
                        }
                    }
                }
            }
        }

        private async Task Log(Exception e, string source)
        {
            Logger.LogError(e, $"Source Method: [{source}]");
            await Cache.Exceptions.InsertException(new ExceptionLog(e, source));
        }
    }
}