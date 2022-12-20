using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers.Extensions;
using SteelBot.RateLimiting;
using SteelBot.Responders;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets;

[SlashCommandGroup("Pets", "Commands for interacting with user pets")]
[SlashRequireGuild]
public class PetsSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly PetCommandsChannel _petCommandsChannel;
    private readonly CancellationService _cancellationService;
    private readonly ILogger<PetsSlashCommands> _logger;
    
    private readonly RateLimit _viewPetsRateLimit;
    private readonly RateLimit _manageRateLimit;
    private readonly RateLimit _treatRateLimit;
    private readonly RateLimit _searchRateLimit;
    private readonly RateLimit _bonusesRateLimit;

    /// <inheritdoc />
    public PetsSlashCommands(ErrorHandlingService errorHandlingService, PetCommandsChannel petCommandsChannel, CancellationService cancellationService, ILogger<PetsSlashCommands> logger, RateLimitFactory rateLimitFactory, AuditLogService auditLogService) : base(logger, auditLogService)
    {
        _errorHandlingService = errorHandlingService;
        _petCommandsChannel = petCommandsChannel;
        _cancellationService = cancellationService;
        _logger = logger;

        _viewPetsRateLimit = rateLimitFactory.Get(nameof(ViewPets), 10, TimeSpan.FromMinutes(1));
        _manageRateLimit = rateLimitFactory.Get(nameof(ManagePets), 10, TimeSpan.FromMinutes(1));
        _treatRateLimit = rateLimitFactory.Get(nameof(TreatPet), 2, TimeSpan.FromHours(1));
        _searchRateLimit = rateLimitFactory.Get(nameof(Search), 10, TimeSpan.FromHours(1));
        _bonusesRateLimit = rateLimitFactory.Get(nameof(Bonuses), 3, TimeSpan.FromMinutes(1));
    }

    [SlashCommand("View", "Show all your owned pets")]
    public async Task ViewPets(InteractionContext context, [Option("OtherUser", "View the pets of another if provided")] DiscordUser otherUser = null)
    {
        _viewPetsRateLimit.ThrowIfExceeded(context.User.Id);
        var targetUser = otherUser != null ? (DiscordMember)otherUser : context.Member;
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to view the pets for user {TargetUserId} pets in guild [{GuildId}]", context.User.Id, targetUser.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.View, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, targetUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Manage", "Manage your owned pets")]
    public async Task ManagePets(InteractionContext context)
    {
        _manageRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to manage their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ManageAll, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Treat", "Give one of your pets a treat, boosting their XP instantly. Allows 2 treats per hour")]
    public async Task TreatPet(InteractionContext context)
    {
        _treatRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to give one of their pets a treat in Guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Treat, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token); 
    }

    [SlashCommand("Search", "Search for a new pet. Allows 10 searches per hour")]
    public async Task Search(InteractionContext context)
    {
        _searchRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("[Slash Command] User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Search, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Bonus", "View the bonuses from all your pets available in this server")]
    public async Task Bonuses(InteractionContext context, [Option("TargetUser", "View the bonuses for another user")] DiscordUser otherUser = null)
    {
        _bonusesRateLimit.ThrowIfExceeded(context.User.Id);
        var targetUser = otherUser != null ? (DiscordMember)otherUser : context.Member;
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to view their applied bonuses in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ViewBonuses, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, targetUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }
}