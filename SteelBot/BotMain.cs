/*
 * Colin the Coding Cat blesses this file.
                    .               ,.
                  T."-._..---.._,-"/|
                  l|"-.  _.v._   (" |
                  [l /.'_ \; _~"-.`-t
                  Y " _(o} _{o)._ ^.|
                  j  T  ,-<v>-.  T  ]
                  \  l ( /-^-\ ) !  !
                   \. \.  "~"  ./  /c-..,__
                     ^r- .._ .- .-"  `- .  ~"--.
                      > \.                      \
                      ]   ^.                     \
                      3  .  ">            .       Y
         ,.__.--._   _j   \ ~   .         ;       |
        (    ~"-._~"^._\   ^.    ^._      I     . l
         "-._ ___ ~"-,_7    .Z-._   7"   Y      ;  \        _
            /"   "~-(r r  _/_--._~-/    /      /,.--^-._   / Y
            "-._    '"~~~>-._~]>--^---./____,.^~        ^.^  !
                ~--._    '   Y---.                        \./
                     ~~--._  l_   )                        \
                           ~-._~~~---._,____..---           \
                               ~----"~       \
                                              \
*/
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
using Sentry;
using SteelBot.Channels.Message;
using SteelBot.Channels.RankRole;
using SteelBot.Channels.SelfRole;
using SteelBot.Channels.Voice;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.NonGroupedCommands;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Secret;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Triggers;
using SteelBot.DiscordModules.Utility;
using SteelBot.DiscordModules.WordGuesser;
using SteelBot.DSharpPlusOverrides;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
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
        private readonly ErrorHandlingService ErrorHandlingService;
        private readonly CancellationService CancellationService;
        private readonly UserLockingService UserLockingService;
        private readonly ErrorHandlingAsynchronousCommandExecutor _commandExecutor;

        // Channels
        private readonly VoiceStateChannel VoiceStateChannel;
        private readonly MessagesChannel IncomingMessageChannel;
        private readonly SelfRoleManagementChannel SelfRoleManagementChannel;
        private readonly RankRoleManagementChannel RankRoleManagementChannel;

        public BotMain(AppConfigurationService appConfigurationService,
            ILogger<BotMain> logger,
            DiscordClient client,
            IServiceProvider serviceProvider,
            DataHelpers dataHelpers,
            DataCache cache,
            ErrorHandlingService errorHandlingService,
            VoiceStateChannel voiceStateChannel,
            CancellationService cancellationService,
            MessagesChannel incomingMessageChannel,
            UserLockingService userLockingService,
            SelfRoleManagementChannel selfRoleManagementChannel,
            RankRoleManagementChannel rankRoleManagementChannel,
            ErrorHandlingAsynchronousCommandExecutor commandExecutor)
        {
            AppConfigurationService = appConfigurationService;
            Logger = logger;
            Client = client;
            ServiceProvider = serviceProvider;
            DataHelpers = dataHelpers;
            Cache = cache;
            ErrorHandlingService = errorHandlingService;
            VoiceStateChannel = voiceStateChannel;
            CancellationService = cancellationService;
            IncomingMessageChannel = incomingMessageChannel;
            UserLockingService = userLockingService;
            SelfRoleManagementChannel = selfRoleManagementChannel;
            RankRoleManagementChannel = rankRoleManagementChannel;
            _commandExecutor = commandExecutor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Starting Bot Version {Version} ({Environment})", AppConfigurationService.Version, AppConfigurationService.Environment);

            Console.CancelKeyPress += async (s, a) => await ShutdownDiscordClient();

            Logger.LogInformation("Initialising Command Modules");
            InitCommands();
            Logger.LogInformation("Initialising Interactivity");
            InitInteractivity();
            Logger.LogInformation("Initialising Event Handlers");
            InitHandlers();
            Logger.LogInformation("Starting Event Handler Channels");
            StartChannels();

            Logger.LogInformation("Starting Client");
            return Client.ConnectAsync(new DiscordActivity("+Help", ActivityType.ListeningTo));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await ShutdownDiscordClient();
            await DataHelpers.Stats.DisconnectAllUsers();
            CancellationService.Cancel();
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
        }

        private async Task ShutdownDiscordClient()
        {
            Logger.LogInformation("Disconnecting Client");
            await Client.DisconnectAsync();
        }

        private void StartChannels()
        {
            VoiceStateChannel.Start(CancellationService.Token);
            IncomingMessageChannel.Start(CancellationService.Token);
            SelfRoleManagementChannel.Start(CancellationService.Token);
            RankRoleManagementChannel.Start(CancellationService.Token);
        }

        private void InitHandlers()
        {
            Client.MessageCreated += HandleMessageCreated;
            Client.VoiceStateUpdated += HandleVoiceStateChange;
            Client.GuildCreated += HandleJoiningGuild;
            Client.GuildDeleted += HandleLeavingGuild;
            Client.GuildMemberRemoved += HandleGuildMemberRemoved;
            Client.ModalSubmitted += HandleModalSubmitted;
            //Client.GuildMemberAdded += HandleGuildMemberAdded; // TODO: Implement properly

            Commands.CommandErrored += HandleCommandErrored;
            Commands.CommandExecuted += HandleCommandExecuted;
        }

        private Task HandleModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
        {
            switch (e.Interaction.Data.CustomId)
            {
                case InteractionIds.Modals.PetNameEntry:
                    _ = DataHelpers.Pets.HandleNamingPet(e);
                    break;
                case InteractionIds.Modals.PetMove:
                    _ = DataHelpers.Pets.HandleMovingPet(e);
                    break;
            }
            return Task.CompletedTask;
        }

        private void InitCommands()
        {
            Commands = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = ServiceProvider,
                PrefixResolver = ResolvePrefix,
                EnableDms = false,
                CommandExecutor = _commandExecutor
            });

            Commands.RegisterCommands<ConfigCommands>();
            Commands.RegisterCommands<RolesCommands>();
            Commands.RegisterCommands<StatsCommands>();
            Commands.RegisterCommands<UtilityCommands>();
            Commands.RegisterCommands<PuzzleCommands>();
            Commands.RegisterCommands<RankRoleCommands>();
            Commands.RegisterCommands<TriggerCommands>();
            Commands.RegisterCommands<FeedbackCommands>();
            Commands.RegisterCommands<FunCommands>();
            Commands.RegisterCommands<MiscCommands>();
            Commands.RegisterCommands<PetsCommands>();
            Commands.RegisterCommands<WordGuesserCommands>();

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
                Timeout = TimeSpan.FromMinutes(2),
            });
        }

        private Task<int> ResolvePrefix(DiscordMessage msg)
        {
            var prefixFound = PrefixResolver.Resolve(msg, Client.CurrentUser, DataHelpers.Config);
            return Task.FromResult(prefixFound);
        }

        private async Task HandleMessageCreated(DiscordClient client, MessageCreateEventArgs args)
        {
            try
            {
                if (args?.Guild != null && args.Author.Id != client.CurrentUser.Id && !PrefixResolver.IsPrefixedCommand(args.Message, Client.CurrentUser, DataHelpers.Config))
                {
                    // TODO: Atomic updates for user properties rather than updating the entire object.
                    // Only non-commands count for message stats.
                    await IncomingMessageChannel.Write(new IncomingMessage(args), CancellationService.Token);
                }
            }
            catch (Exception ex)
            {
                await ErrorHandlingService.Log(ex, nameof(HandleMessageCreated));
            }
        }

        private async Task HandleVoiceStateChange(DiscordClient client, VoiceStateUpdateEventArgs args)
        {
            try
            {
                if (args?.Guild != null && args.User.Id != client.CurrentUser.Id)
                {
                    await VoiceStateChannel.Write(new VoiceStateChange(args), CancellationService.Token);
                }
            }
            catch (Exception ex)
            {
                await ErrorHandlingService.Log(ex, nameof(HandleVoiceStateChange));
            }
        }

        private async Task HandleJoiningGuild(DiscordClient client, GuildCreateEventArgs args)
        {
            try
            {
                var joinedGuild = new Guild(args.Guild.Id);
                await Cache.Guilds.UpsertGuild(joinedGuild);
            }
            catch (Exception ex)
            {
                await ErrorHandlingService.Log(ex, nameof(HandleJoiningGuild));
            }
        }

        private Task HandleLeavingGuild(DiscordClient client, GuildDeleteEventArgs args)
        {
            Task.Run(async () =>
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
                    await ErrorHandlingService.Log(ex, nameof(HandleLeavingGuild));
                }
            }).FireAndForget(ErrorHandlingService);
            return Task.CompletedTask;
        }

        private Task HandleCommandErrored(CommandsNextExtension ext, CommandErrorEventArgs args)
        {
            // TODO: This is awful, refactor.
            Task.Run(async () =>
            {
                if (args.Exception is ChecksFailedException ex)
                {
                    foreach (CheckBaseAttribute failedCheck in ex.FailedChecks)
                    {
                        if (failedCheck is CooldownAttribute cooldown)
                        {
                            await args.Context.Member.SendMessageAsync(embed: EmbedGenerator
                                .Warning($"The `{args.Command.QualifiedName}` command can only be executed **{"time".ToQuantity(cooldown.MaxUses)}** every **{cooldown.Reset.Humanize()}**{Environment.NewLine}{Environment.NewLine}**{cooldown.GetRemainingCooldown(args.Context).Humanize()}** remaining"));
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
                    await ErrorHandlingService.Log(args.Exception, args.Context.Message.Content);
                    await args.Context.Channel.SendMessageAsync(embed: EmbedGenerator.Error("Something went wrong.\nMy creator has been notified."));
                }
            }).FireAndForget(ErrorHandlingService);
            return Task.CompletedTask;
        }

        private Task HandleCommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs args)
        {
            Task.Run(async () =>
            {
                Interlocked.Increment(ref AppConfigurationService.HandledCommands);
                await Cache.CommandStatistics.IncrementCommandStatistic(args.Command.QualifiedName);
            }).FireAndForget(ErrorHandlingService);

            return Task.CompletedTask;
        }

        private async Task HandleGuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs args)
        {
            try
            {
                var msg = new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Info("This is a tester welcome message", $"Welcome to {args.Guild.Name}!", "Thanks for joining."));
                await args.Member.SendMessageAsync(msg);
            }
            catch (Exception ex)
            {
                await ErrorHandlingService.Log(ex, nameof(HandleGuildMemberAdded));
            }
        }

        private Task HandleGuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    using (await UserLockingService.WriterLockAsync(args.Guild.Id, args.Member.Id))
                    {
                        // Delete user data.
                        await Cache.Users.RemoveUser(args.Guild.Id, args.Member.Id);
                    }
                }
                catch (Exception ex)
                {
                    await ErrorHandlingService.Log(ex, nameof(HandleGuildMemberRemoved));
                }
            }).FireAndForget(ErrorHandlingService);

            return Task.CompletedTask;
        }
    }
}