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
using SteelBot.DiscordModules.Triggers;
using SteelBot.DiscordModules.Utility;
using SteelBot.Helpers;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
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
            await DataHelpers.Stats.DisconnectAllUsers();

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
            Client.GuildDeleted += HandleLeavingGuild;
            Client.GuildMemberRemoved += HandleGuildMemberRemoved;
            Client.GuildMemberAdded += HandleGuildMemberAdded;

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
            Commands.RegisterCommands<MiscCommands>();

            Commands.SetHelpFormatter<CustomHelpFormatter>();
        }

        private void InitInteractivity()
        {
            // Enable interactivity and set default options.
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // Default pagination behaviour to just ignore the reactions.
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                AckPaginationButtons = true,
                ButtonBehavior = ButtonPaginationBehavior.DeleteButtons,

                // Default timeout for other actions to 2 minutes.
                Timeout = TimeSpan.FromMinutes(2)
            });
        }

        private Task<int> ResolvePrefix(DiscordMessage msg)
        {
            string guildsPrefix = DataHelpers.Config.GetPrefix(msg.Channel.GuildId.Value);

            int prefixFound = CommandsNextUtilities.GetStringPrefixLength(msg, guildsPrefix);
            if (prefixFound == -1)
            {
                prefixFound = CommandsNextUtilities.GetMentionPrefixLength(msg, Client.CurrentUser);
            }
            return Task.FromResult(prefixFound);
        }

        private Task HandleReactionAdded(DiscordClient client, MessageReactionAddEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (args.Guild != null &&
                        await UserTrackingService.TrackUser(args.Guild.Id, args.User, args.Guild, client))
                    {
                        await DataHelpers.Polls.HandleMessageReaction(args, Client.CurrentUser.Id);
                    }
                }
                catch (Exception ex)
                {
                    await Log(ex, nameof(HandleReactionAdded));
                }
            });

            return Task.CompletedTask;
        }

        private Task HandleMessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Only pay attention to guild messages.
                    if (args.Guild != null && await UserTrackingService.TrackUser(args.Guild.Id, args.Author, args.Guild, client))
                    {
                        bool levelUp = await DataHelpers.Stats.HandleNewMessage(args);
                        if (levelUp)
                        {
                            await DataHelpers.RankRoles.UserLevelledUp(args.Guild.Id, args.Author.Id, args.Guild);
                        }
                        await DataHelpers.Triggers.HandleNewMessage(args.Guild.Id, args.Channel, args.Message.Content);
                    }
                }
                catch (Exception ex)
                {
                    await Log(ex, nameof(HandleMessageCreated));
                }
            });

            return Task.CompletedTask;

        }

        private Task HandleVoiceStateChange(DiscordClient client, VoiceStateUpdateEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (args.Guild != null && await UserTrackingService.TrackUser(args.Guild.Id, args.User, args.Guild, client))
                    {
                        bool levelUp = await DataHelpers.Stats.HandleVoiceStateChange(args);
                        if (levelUp)
                        {
                            await DataHelpers.RankRoles.UserLevelledUp(args.Guild.Id, args.User.Id, args.Guild);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Log(ex, nameof(HandleVoiceStateChange));
                }
            });

            return Task.CompletedTask;
        }

        private async Task HandleJoiningGuild(DiscordClient client, GuildCreateEventArgs args)
        {
            // Don't offload to a Task.run because this happens rarely and needs to happen before any commands from a new guild can be processed.
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

        private async Task HandleLeavingGuild(DiscordClient client, GuildDeleteEventArgs args)
        {
            try
            {
                var usersInGuild = Cache.Users.GetUsersInGuild(args.Guild.Id);
                foreach (var user in usersInGuild)
                {
                    await Cache.Users.RemoveUser(args.Guild.Id, user.DiscordId);
                }
                await Cache.Guilds.RemoveGuild(args.Guild.Id);
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleLeavingGuild));
            }
        }

        private Task HandleCommandErrored(CommandsNextExtension ext, CommandErrorEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                if (args.Exception is ChecksFailedException ex)
                {
                    foreach (CheckBaseAttribute failedCheck in ex.FailedChecks)
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
            });
            return Task.CompletedTask;
        }

        private Task HandleCommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs args)
        {
            _ = Task.Run(async () =>
            {
                Interlocked.Increment(ref AppConfigurationService.HandledCommands);
                await Cache.CommandStatistics.IncrementCommandStatistic(args.Command.QualifiedName);
            });

            return Task.CompletedTask;
        }

        private async Task HandleGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs args)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    var msg = new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Info("This is a tester welcome message", $"Welcome to {args.Guild.Name}!", "Thanks for joining."));
                    await args.Member.SendMessageAsync(msg);
                });
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleGuildMemberAdded));
            }
        }

        private async Task HandleGuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs args)
        {
            try
            {
                // Delete user data.
                await Cache.Users.RemoveUser(args.Guild.Id, args.Member.Id);
            }
            catch (Exception ex)
            {
                await Log(ex, nameof(HandleGuildMemberRemoved));
            }
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