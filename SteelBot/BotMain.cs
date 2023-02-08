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
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteelBot.Channels;
using SteelBot.Channels.Message;
using SteelBot.Channels.Pets;
using SteelBot.Channels.Puzzle;
using SteelBot.Channels.RankRole;
using SteelBot.Channels.SelfRole;
using SteelBot.Channels.Stats;
using SteelBot.Channels.Voice;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.DiscordModules;
using SteelBot.DiscordModules.AuditLog;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.NonGroupedCommands;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Puzzle;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Triggers;
using SteelBot.DiscordModules.Utility;
using SteelBot.DSharpPlusOverrides;
using SteelBot.Exceptions;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot;

public class BotMain : IHostedService
{
#if DEBUG
    private readonly ulong? _testServerId = 782237087352356876;
#else
    private readonly ulong? _testServerId = null;
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
    private readonly AuditLogService _auditLogService;
    private readonly UserTrackingService _userTrackingService;

    // Channels
    private readonly VoiceStateChannel _voiceStateChannel;
    private readonly MessagesChannel _incomingMessageChannel;
    private readonly SelfRoleManagementChannel _selfRoleManagementChannel;
    private readonly RankRoleManagementChannel _rankRoleManagementChannel;
    private readonly PetCommandsChannel _petCommandsChannel;
    private readonly StatsCommandsChannel _statsCommandsChannel;
    private readonly PuzzleCommandsChannel _puzzleCommandsChannel;

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
        PetCommandsChannel petCommandsChannel,
        StatsCommandsChannel statsCommandsChannel,
        PuzzleCommandsChannel puzzleCommandsChannel,
        AuditLogService auditLogService,
        UserTrackingService userTrackingService)
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
        _petCommandsChannel = petCommandsChannel;
        _statsCommandsChannel = statsCommandsChannel;
        _puzzleCommandsChannel = puzzleCommandsChannel;
        _auditLogService = auditLogService;
        _userTrackingService = userTrackingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Bot Version {Version} ({Environment})", _appConfigurationService.Version, _appConfigurationService.Environment);

        Console.CancelKeyPress += async (s, a) => await ShutdownDiscordClient();

        _logger.LogInformation("Initialising Command Modules");
        SetupChannelsController();
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

    private void SetupChannelsController()
    {
        ChannelsController.MessagesChannel = _serviceProvider.GetRequiredService<MessagesChannel>();
        ChannelsController.PetCommandsChannel = _serviceProvider.GetRequiredService<PetCommandsChannel>();
        ChannelsController.RankRoleManagementChannel = _serviceProvider.GetRequiredService<RankRoleManagementChannel>();
        ChannelsController.SelfRoleManagementChannel = _serviceProvider.GetRequiredService<SelfRoleManagementChannel>();
        ChannelsController.VoiceStateChannel = _serviceProvider.GetRequiredService<VoiceStateChannel>();
        ChannelsController.PuzzleCommandsChannel = _serviceProvider.GetRequiredService<PuzzleCommandsChannel>();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await ShutdownDiscordClient();
        await _dataHelpers.Stats.DisconnectAllUsers();
        _cancellationService.Cancel();
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
        _petCommandsChannel.Start(_cancellationService.Token);
        _statsCommandsChannel.Start(_cancellationService.Token);
        _puzzleCommandsChannel.Start(_cancellationService.Token);
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
        _client.GuildMemberAdded += HandleGuildMemberAdded;
        _client.MessageReactionAdded += HandleMessageReactionAdded;
        
        _commands.CommandErrored += HandleCommandErrored;
        _commands.CommandExecuted += HandleCommandExecuted;

        _slashCommands.SlashCommandErrored += HandleSlashCommandErrored;
        _slashCommands.SlashCommandInvoked += HandleSlashCommandInvoked;
        _slashCommands.SlashCommandExecuted += HandleSlashCommandExecuted;
    }

    private async Task HandleSlashCommandExecuted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs e)
    {
        Interlocked.Increment(ref _appConfigurationService.HandledCommands);
        await _cache.CommandStatistics.IncrementCommandStatistic(e.Context.QualifiedName);
    }

    private async Task HandleMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
    {
        await _userTrackingService.TrackUser(e.Guild.Id, e.User, e.Guild, sender);
        await _auditLogService.MessageReactionAdded(e);
    }


    private async Task HandleSlashCommandInvoked(SlashCommandsExtension sender, SlashCommandInvokedEventArgs e)
    {
        if (e.Context.Guild != null)
        {
            await _userTrackingService.TrackUser(e.Context.Guild.Id, e.Context.User, e.Context.Guild, e.Context.Client);
        }
    }

    private Task HandleSlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs args)
    {
        // TODO: This is awful, refactor.
        Task.Run(async () =>
        {
            if (args.Exception is SlashExecutionChecksFailedException ex)
            {
                foreach (var failedCheck in ex.FailedChecks)
                {
                    if (failedCheck is SlashCooldownAttribute cooldown)
                    {
                        await args.Context.Member.SendMessageAsync(EmbedGenerator
                            .Warning(
                                $"This `{args.Context.QualifiedName}` command can only be executed **{"time".ToQuantity(cooldown.MaxUses)}** every **{cooldown.Reset.Humanize()}**{Environment.NewLine}{Environment.NewLine}**{cooldown.GetRemainingCooldown(args.Context).Humanize()}** remaining"));
                        return;
                    }

                    if (failedCheck is SlashRequireUserPermissionsAttribute userPerms)
                    {
                        await args.Context.Member.SendMessageAsync(EmbedGenerator
                            .Warning($"This `{args.Context.QualifiedName}` command can only be executed by users with **{userPerms.Permissions}** permission"));
                        return;
                    }
                }
            }
            else if (args.Exception is CommandRateLimitedException rateLimitedException)
            {
                await args.Context.Member.SendMessageAsync(EmbedGenerator.Warning(rateLimitedException.Message));
            }
            else if (args.Context.Guild == null)
            {
                await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("Sorry, you can't use commands outside of a server"))).AsEphemeral());
            }
            else
            {
                await _errorHandlingService.Log(args.Exception, args.Context.QualifiedName);
                await args.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("Something went wrong.\nMy creator has been notified."))).AsEphemeral());
            }
        }).FireAndForget(_errorHandlingService);
        return Task.CompletedTask;
    }

    private Task HandleModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
    {
        // Prevent the sentry context clashing with the existing one.
        using (ExecutionContext.SuppressFlow())
        {
            Task.Run(async () =>
            {
                await _auditLogService.ModalSubmitted(e);
                switch (e.Interaction.Data.CustomId)
                {
                    case InteractionIds.Modals.PetNameEntry:
                        await _dataHelpers.Pets.HandleNamingPet(e);
                        break;
                    case InteractionIds.Modals.PetMove:
                        await _dataHelpers.Pets.HandleMovingPet(e);
                        break;
                }
            }).FireAndForget(_errorHandlingService);
        }


        return Task.CompletedTask;
    }

    private void InitCommands()
    {
        _commands = _client.UseCommandsNext(new CommandsNextConfiguration { Services = _serviceProvider, PrefixResolver = ResolvePrefix, EnableDms = false, CommandExecutor = _commandExecutor });

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

        _commands.SetHelpFormatter<CustomHelpFormatter>();

        _slashCommands = _client.UseSlashCommands(new SlashCommandsConfiguration { Services = _serviceProvider });

        _slashCommands.RegisterCommands<MiscSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<StatsSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<PuzzleSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<PetsSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<ConfigSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<FunSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<RankRoleSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<RolesSlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<UtilitySlashCommands>(_testServerId);
        _slashCommands.RegisterCommands<AuditLogSlashCommands>(_testServerId);
    }

    private void InitInteractivity() =>
        // Enable interactivity and set default options.
        _client.UseInteractivity(new InteractivityConfiguration
        {
            // Default pagination behaviour to just ignore the reactions.
            PaginationBehaviour = PaginationBehaviour.WrapAround,
            AckPaginationButtons = true,
            ButtonBehavior = ButtonPaginationBehavior.DeleteButtons,

            // Default timeout for other actions to 2 minutes.
            Timeout = TimeSpan.FromMinutes(2)
        });

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
                // TODO: Atomic updates for user properties rather than updating the entire object.
                // Only non-commands count for message stats.
                await _incomingMessageChannel.Write(new IncomingMessage(args), _cancellationService.Token);
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
            if (args?.Guild != null && args.User.Id != client.CurrentUser.Id) await _voiceStateChannel.Write(new VoiceStateChange(args), _cancellationService.Token);
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
            var joinedGuild = new Guild(args.Guild.Id, args.Guild.Name);
            await _cache.Guilds.UpsertGuild(joinedGuild);
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
                var usersInGuild = _cache.Users.GetUsersInGuild(args.Guild.Id);
                foreach (var user in usersInGuild) await _cache.Users.RemoveUser(args.Guild.Id, user.DiscordId);
                await _cache.Guilds.RemoveGuild(args.Guild.Id);
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
                        await args.Context.Member.SendMessageAsync(EmbedGenerator
                            .Warning(
                                $"The `{args.Command.QualifiedName}` command can only be executed **{"time".ToQuantity(cooldown.MaxUses)}** every **{cooldown.Reset.Humanize()}**{Environment.NewLine}{Environment.NewLine}**{cooldown.GetRemainingCooldown(args.Context).Humanize()}** remaining"));
                        return;
                    }

                    if (failedCheck is RequireUserPermissionsAttribute userPerms)
                    {
                        await args.Context.Member.SendMessageAsync(EmbedGenerator
                            .Warning($"The `{args.Command.QualifiedName}` command can only be executed by users with **{userPerms.Permissions}** permission"));
                        return;
                    }
                }
            }
            else if (args.Exception is CommandRateLimitedException rateLimitedException)
            {
                await args.Context.Member.SendMessageAsync(EmbedGenerator.Warning(rateLimitedException.Message));
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
                await args.Context.Channel.SendMessageAsync(EmbedGenerator.Primary(_appConfigurationService.Application.UnknownCommandResponse, "Unknown Command"));
            }
            else
            {
                await _errorHandlingService.Log(args.Exception, args.Context.Message.Content);
                await args.Context.Channel.SendMessageAsync(EmbedGenerator.Error("Something went wrong.\nMy creator has been notified."));
            }
        }).FireAndForget(_errorHandlingService);
        return Task.CompletedTask;
    }

    private Task HandleCommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs args)
    {
        Task.Run(async () =>
        {
            Interlocked.Increment(ref _appConfigurationService.HandledCommands);
            await _cache.CommandStatistics.IncrementCommandStatistic(args.Command.QualifiedName);
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
            await _auditLogService.JoinedGuild(args);
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
                await _auditLogService.LeftGuild(args);
                using (await _userLockingService.WriterLockAsync(args.Guild.Id, args.Member.Id))
                {
                    // Delete user data.
                    await _cache.Users.RemoveUser(args.Guild.Id, args.Member.Id);
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.Log(ex, nameof(HandleGuildMemberRemoved));
            }
        }).FireAndForget(_errorHandlingService);

        return Task.CompletedTask;
    }
}