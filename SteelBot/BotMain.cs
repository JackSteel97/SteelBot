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
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.NonGroupedCommands;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Secret;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Stocks;
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
        private readonly UserTrackingService UserTrackingService;

        public BotMain(AppConfigurationService appConfigurationService, ILogger<BotMain> logger, DiscordClient client, IServiceProvider serviceProvider, DataHelpers dataHelpers, DataCache cache, UserTrackingService userTrackingService)
        {
            AppConfigurationService = appConfigurationService;
            Logger = logger;
            Client = client;
            ServiceProvider = serviceProvider;
            DataHelpers = dataHelpers;
            Cache = cache;
            UserTrackingService = userTrackingService;
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await ShutdownDiscordClient();
            await Cache.Users.DisconnectAllUsers();
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
            Commands.CommandExecuted += HandleCommandExecuted;
        }

        private void InitCommands()
        {
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = ServiceProvider,
                PrefixResolver = ResolvePrefix,
                EnableDms = false
            });

            Commands.RegisterCommands<ConfigCommands>();
            Commands.RegisterCommands<PollsCommands>();
            Commands.RegisterCommands<RolesCommands>();
            Commands.RegisterCommands<StatsCommands>();
            Commands.RegisterCommands<UtilityCommands>();
            Commands.RegisterCommands<PuzzleCommands>();
            Commands.RegisterCommands<RankRoleCommands>();
            Commands.RegisterCommands<TriggerCommands>();
            Commands.RegisterCommands<FeedbackCommands>();
            Commands.RegisterCommands<FunCommands>();
            Commands.RegisterCommands<StocksCommands>();
            Commands.RegisterCommands<PortfolioCommands>();

            Commands.SetHelpFormatter<CustomHelpFormatter>();
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
                    // Ignore bots and the current user.
                    if (!args.User.IsBot && args.User.Id != client.CurrentUser.Id)
                    {
                        await UserTrackingService.TrackUser(args.Guild.Id, args.User.Id, args.Guild);

                        _ = Task.Run(async () =>
                        {
                            await DataHelpers.Polls.HandleMessageReaction(args, Client.CurrentUser.Id);
                        });
                    }
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
                // Only pay attention to guild messages.
                if (args.Guild != null)
                {
                    // Ignore bots and the current user.
                    if (!args.Author.IsBot && args.Author.Id != client.CurrentUser.Id)
                    {
                        await UserTrackingService.TrackUser(args.Guild.Id, args.Author.Id, args.Guild);

                        _ = Task.Run(async () =>
                        {
                            bool levelUp = await DataHelpers.Stats.HandleNewMessage(args);
                            if (levelUp)
                            {
                                await DataHelpers.RankRoles.UserLevelledUp(args.Guild.Id, args.Author.Id, args.Guild);
                            }
                            await DataHelpers.Triggers.HandleNewMessage(args.Guild.Id, args.Channel, args.Message.Content);
                        });
                    }
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
                if (args.Guild != null)
                {
                    // Ignore bots and the current user.
                    if (!args.User.IsBot && args.User.Id != client.CurrentUser.Id)
                    {
                        await UserTrackingService.TrackUser(args.Guild.Id, args.User.Id, args.Guild);
                        _ = Task.Run(async () =>
                        {
                            bool levelUp = await DataHelpers.Stats.HandleVoiceStateChange(args);
                            if (levelUp)
                            {
                                await DataHelpers.RankRoles.UserLevelledUp(args.Guild.Id, args.User.Id, args.Guild);
                            }
                        });
                    }
                }
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
                foreach (var failedCheck in ex.FailedChecks)
                {
                    if (failedCheck is CooldownAttribute cooldown)
                    {
                        await args.Context.Member.SendMessageAsync(embed: EmbedGenerator
                            .Warning($"The `{args.Command.QualifiedName}` command can only be executed **{"time".ToQuantity(cooldown.MaxUses)}** every **{cooldown.Reset.Humanize()}**"));
                        return;
                    }
                    if (failedCheck is RequireUserPermissionsAttribute userPerms)
                    {
                        await args.Context.Member.SendMessageAsync(embed: EmbedGenerator
                            .Warning($"The `{args.Command.QualifiedName}` command can only be executed by users with **{userPerms.Permissions}** permission"));
                        return;
                    }
                }
            }
            else if (args.Exception.Message.Equals("Could not find a suitable overload for the command.", StringComparison.OrdinalIgnoreCase)
                || args.Exception.Message.Equals("No matching subcommands were found, and this group is not executable.", StringComparison.OrdinalIgnoreCase))
            {
                Command helpCmd = Commands.FindCommand("help", out string _);
                CommandContext helpCtx = Commands.CreateContext(args.Context.Message, args.Context.Prefix, helpCmd, args.Command.QualifiedName);
                _ = Commands.ExecuteCommandAsync(helpCtx);
            }
            else if (args.Exception.Message.Equals("Specified command was not found.", StringComparison.OrdinalIgnoreCase))
            {
                await args.Context.Channel.SendMessageAsync(embed: EmbedGenerator.Primary(AppConfigurationService.Application.UnknownCommandResponse, "Unknown Command"));
                return;
            }
            else
            {
                await Log(args.Exception, args.Context.Message.Content);
                await args.Context.Channel.SendMessageAsync(embed: EmbedGenerator.Error("Something went wrong.\nMy creator has been notified."));
            }
        }

        private async Task HandleCommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs args)
        {
            Interlocked.Increment(ref AppConfigurationService.HandledCommands);
            await Cache.CommandStatistics.IncrementCommandStatistic(args.Command.QualifiedName);
        }

        private async Task Log(Exception e, string source)
        {
            Logger.LogError(e, $"Source Method: [{source}]");
            await Cache.Exceptions.InsertException(new ExceptionLog(e, source));
            await SendMessageToJack(e, source);
        }

        private async Task SendMessageToJack(Exception e, string source)
        {
            ulong civlationId = AppConfigurationService.Application.CommonServerId;
            ulong jackId = AppConfigurationService.Application.CreatorUserId;

            DiscordGuild commonServer = await Client.GetGuildAsync(civlationId);
            DiscordMember jack = await commonServer.GetMemberAsync(jackId);

            await jack.SendMessageAsync(embed: EmbedGenerator.Info($"Error Message:\n{Formatter.BlockCode(e.Message)}\nAt:\n{Formatter.InlineCode(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"))}", "An Error Occured", $"Source: {source}"));
        }
    }
}