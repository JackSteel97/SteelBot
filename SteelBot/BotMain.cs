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
using DSharpPlus.SlashCommands;
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
using SteelBot.Helpers.Sentry;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot;

public class BotMain : IHostedService
{
    #if DEBUG
    private static readonly ulong? _testServerId = 782237087352356876;
    #else
    private static readonly ulong? _testServerId = null;
    #endif
    
    private readonly AppConfigurationService _appConfigurationService;
    private readonly ILogger<BotMain> _logger;
    private readonly DiscordClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataHelpers _dataHelpers;
    private readonly DataCache _cache;
    private CommandsNextExtension _commands;
    private SlashCommandsExtension _slashCommands;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly CancellationService _cancellationService;
    private readonly UserLockingService _userLockingService;
    private readonly ErrorHandlingAsynchronousCommandExecutor _commandExecutor;
    private readonly IHub _sentry;

    // Channels
    private readonly VoiceStateChannel _voiceStateChannel;
    private readonly MessagesChannel _incomingMessageChannel;
    private readonly SelfRoleManagementChannel _selfRoleManagementChannel;
    private readonly RankRoleManagementChannel _rankRoleManagementChannel;

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
        ErrorHandlingAsynchronousCommandExecutor commandExecutor,
        IHub sentry)
    {
        _appConfigurationService = appConfigurationService;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _dataHelpers = dataHelpers;
        _cache = cache;
        _errorHandlingService = errorHandlingService;
        _voiceStateChannel = voiceStateChannel;
        _cancellationService = cancellationService;
        _incomingMessageChannel = incomingMessageChannel;
        _userLockingService = userLockingService;
        _selfRoleManagementChannel = selfRoleManagementChannel;
        _rankRoleManagementChannel = rankRoleManagementChannel;
        _commandExecutor = commandExecutor;
        _sentry = sentry;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Bot Version {Version} ({Environment})", _appConfigurationService.Version, _appConfigurationService.Environment);

        Console.CancelKeyPress += async (s, a) => await ShutdownDiscordClient();

        _logger.LogInformation("Initialising Command Modules");
        InitCommands();
        _logger.LogInformation("Initialising Interactivity");
        InitInteractivity();
        _logger.LogInformation("Initialising Event Handlers");
        InitHandlers();
        _logger.LogInformation("Starting Event Handler Channels");
        StartChannels();

        _logger.LogInformation("Starting Client");
        return _client.ConnectAsync(new DiscordActivity("+Help", ActivityType.ListeningTo));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var transaction = _sentry.StartNewConfiguredTransaction("Shutdown", nameof(StopAsync));
        await ShutdownDiscordClient();
        await _dataHelpers.Stats.DisconnectAllUsers();
        _cancellationService.Cancel();
        transaction.Finish();
        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
    }

    private async Task ShutdownDiscordClient()
    {
        _logger.LogInformation("Disconnecting Client");
        await _client.DisconnectAsync();
    }

    private void StartChannels()
    {
        _voiceStateChannel.Start(_cancellationService.Token);
        _incomingMessageChannel.Start(_cancellationService.Token);
        _selfRoleManagementChannel.Start(_cancellationService.Token);
        _rankRoleManagementChannel.Start(_cancellationService.Token);
    }

    private void InitHandlers()
    {
        _client.MessageCreated += HandleMessageCreated;
        _client.VoiceStateUpdated += HandleVoiceStateChange;
        _client.GuildCreated += HandleJoiningGuild;
        _client.GuildDeleted += HandleLeavingGuild;
        _client.GuildMemberRemoved += HandleGuildMemberRemoved;
        _client.ModalSubmitted += HandleModalSubmitted;
        _client.GuildAvailable += HandleGuildAvailable;
        //Client.GuildMemberAdded += HandleGuildMemberAdded; // TODO: Implement properly

        _commands.CommandErrored += HandleCommandErrored;
        _commands.CommandExecuted += HandleCommandExecuted;
    }



    private Task HandleModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
    {
        Task.Run(async () =>
        {
            var transaction = _sentry.StartNewConfiguredTransaction(nameof(HandleModalSubmitted), e.Interaction.Data.CustomId, e.Interaction.User, e.Interaction.Guild);
            switch (e.Interaction.Data.CustomId)
            {
                case InteractionIds.Modals.PetNameEntry:
                    await _dataHelpers.Pets.HandleNamingPet(e);
                    break;
                case InteractionIds.Modals.PetMove:
                    await _dataHelpers.Pets.HandleMovingPet(e);
                    break;
            }
            transaction.Finish();
        }).FireAndForget(_errorHandlingService);

        return Task.CompletedTask;
    }

    private void InitCommands()
    {
        _commands = _client.UseCommandsNext(new CommandsNextConfiguration()
        {
            Services = _serviceProvider,
            PrefixResolver = ResolvePrefix,
            EnableDms = false,
            CommandExecutor = _commandExecutor
        });

        _commands.RegisterCommands<ConfigCommands>();
        _commands.RegisterCommands<RolesCommands>();
        _commands.RegisterCommands<StatsCommands>();
        _commands.RegisterCommands<UtilityCommands>();
        _commands.RegisterCommands<PuzzleCommands>();
        _commands.RegisterCommands<RankRoleCommands>();
        _commands.RegisterCommands<TriggerCommands>();
        _commands.RegisterCommands<FeedbackCommands>();
        _commands.RegisterCommands<FunCommands>();
        _commands.RegisterCommands<MiscCommands>();
        _commands.RegisterCommands<PetsCommands>();
        _commands.RegisterCommands<WordGuesserCommands>();

        _commands.SetHelpFormatter<CustomHelpFormatter>();

        _slashCommands = _client.UseSlashCommands(new SlashCommandsConfiguration()
        {
            Services = _serviceProvider,
        });

        _slashCommands.RegisterCommands<MiscSlashCommands>(_testServerId);
    }

    private void InitInteractivity()
    {
        // Enable interactivity and set default options.
        _client.UseInteractivity(new InteractivityConfiguration
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
        int prefixFound = PrefixResolver.Resolve(msg, _client.CurrentUser, _dataHelpers.Config);
        return Task.FromResult(prefixFound);
    }

    private async Task HandleMessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        try
        {
            if (args?.Guild != null && args.Author.Id != client.CurrentUser.Id && !PrefixResolver.IsPrefixedCommand(args.Message, _client.CurrentUser, _dataHelpers.Config))
            {
                // TODO: Atomic updates for user properties rather than updating the entire object.
                // Only non-commands count for message stats.
                await _incomingMessageChannel.Write(new IncomingMessage(args), _cancellationService.Token);
            }
        }
        catch (Exception ex)
        {
            await _errorHandlingService.Log(ex, nameof(HandleMessageCreated));
        }
    }

    private async Task HandleVoiceStateChange(DiscordClient client, VoiceStateUpdateEventArgs args)
    {
        try
        {
            if (args?.Guild != null && args.User.Id != client.CurrentUser.Id)
            {
                await _voiceStateChannel.Write(new VoiceStateChange(args), _cancellationService.Token);
            }
        }
        catch (Exception ex)
        {
            await _errorHandlingService.Log(ex, nameof(HandleVoiceStateChange));
        }
    }

    private async Task HandleJoiningGuild(DiscordClient client, GuildCreateEventArgs args)
    {
        try
        {
            var transaction = _sentry.StartNewConfiguredTransaction(nameof(HandleJoiningGuild), "Create Guild");

            var joinedGuild = new Guild(args.Guild.Id, args.Guild.Name);
            await _cache.Guilds.UpsertGuild(joinedGuild);
            transaction.Finish();
        }
        catch (Exception ex)
        {
            await _errorHandlingService.Log(ex, nameof(HandleJoiningGuild));
        }
    }

    private Task HandleLeavingGuild(DiscordClient client, GuildDeleteEventArgs args)
    {
        Task.Run(async () =>
        {
            try
            {
                var transaction = _sentry.StartNewConfiguredTransaction(nameof(HandleLeavingGuild), "Delete Guild");

                var usersInGuild = _cache.Users.GetUsersInGuild(args.Guild.Id);
                foreach (var user in usersInGuild)
                {
                    await _cache.Users.RemoveUser(args.Guild.Id, user.DiscordId);
                }
                await _cache.Guilds.RemoveGuild(args.Guild.Id);
                transaction.Finish();
            }
            catch (Exception ex)
            {
                await _errorHandlingService.Log(ex, nameof(HandleLeavingGuild));
            }
        }).FireAndForget(_errorHandlingService);
        return Task.CompletedTask;
    }

    private Task HandleCommandErrored(CommandsNextExtension ext, CommandErrorEventArgs args)
    {
        // TODO: This is awful, refactor.
        Task.Run(async () =>
        {
            if (args.Exception is ChecksFailedException ex)
            {
                foreach (var failedCheck in ex.FailedChecks)
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
                var helpCmd = _commands.FindCommand("help", out string _);
                var helpCtx = _commands.CreateContext(args.Context.Message, args.Context.Prefix, helpCmd, args.Command.QualifiedName);
                _ = _commands.ExecuteCommandAsync(helpCtx);
            }
            else if (args.Exception.Message.Equals("Specified command was not found.", StringComparison.OrdinalIgnoreCase))
            {
                await args.Context.Channel.SendMessageAsync(embed: EmbedGenerator.Primary(_appConfigurationService.Application.UnknownCommandResponse, "Unknown Command"));
                return;
            }
            else
            {
                await _errorHandlingService.Log(args.Exception, args.Context.Message.Content);
                await args.Context.Channel.SendMessageAsync(embed: EmbedGenerator.Error("Something went wrong.\nMy creator has been notified."));
            }
        }).FireAndForget(_errorHandlingService);
        return Task.CompletedTask;
    }

    private Task HandleCommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs args)
    {
        Task.Run(async () =>
        {
            var transaction = _sentry.StartNewConfiguredTransaction(nameof(HandleCommandExecuted), "Increment Statistics");

            Interlocked.Increment(ref _appConfigurationService.HandledCommands);
            await _cache.CommandStatistics.IncrementCommandStatistic(args.Command.QualifiedName);

            transaction.Finish();
        }).FireAndForget(_errorHandlingService);

        return Task.CompletedTask;
    }

    private Task HandleGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Task.Run(async () =>
        {
            await _cache.Guilds.UpdateGuildName(e.Guild.Id, e.Guild.Name);
        }).FireAndForget(_errorHandlingService);
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
            await _errorHandlingService.Log(ex, nameof(HandleGuildMemberAdded));
        }
    }

    private Task HandleGuildMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs args)
    {
        Task.Run(async () =>
        {
            try
            {
                var transaction = _sentry.StartNewConfiguredTransaction(nameof(HandleGuildMemberRemoved), "Remove Member");

                using (await _userLockingService.WriterLockAsync(args.Guild.Id, args.Member.Id))
                {
                    var span = transaction.StartChild("Delete User", args.Member.Username);
                    // Delete user data.
                    await _cache.Users.RemoveUser(args.Guild.Id, args.Member.Id);
                    span.Finish();
                }

                transaction.Finish();
            }
            catch (Exception ex)
            {
                await _errorHandlingService.Log(ex, nameof(HandleGuildMemberRemoved));
            }
        }).FireAndForget(_errorHandlingService);

        return Task.CompletedTask;
    }
}