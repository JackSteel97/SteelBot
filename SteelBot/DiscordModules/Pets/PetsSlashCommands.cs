using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.Channels.Pets;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
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
    private const double _hourSeconds = 60 * 60;

    /// <inheritdoc />
    public PetsSlashCommands(ErrorHandlingService errorHandlingService, PetCommandsChannel petCommandsChannel, CancellationService cancellationService, ILogger<PetsSlashCommands> logger) : base(logger)
    {
        _errorHandlingService = errorHandlingService;
        _petCommandsChannel = petCommandsChannel;
        _cancellationService = cancellationService;
        _logger = logger;
    }

    [SlashCommand("View", "Show all your owned pets")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task GetPets(InteractionContext context, [Option("OtherUser", "View the pets of another if provided")] DiscordUser otherUser = null)
    {
        var targetUser = otherUser != null ? (DiscordMember)otherUser : context.Member;
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to view the pets for user {TargetUserId} pets in guild [{GuildId}]", context.User.Id, targetUser.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.View, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, targetUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Manage", "Manage your owned pets")]
    [SlashCooldown(10, 60, SlashCooldownBucketType.User)]
    public async Task ManagePets(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to manage their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ManageAll, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Treat", "Give one of your pets a treat, boosting their XP instantly. Allows 2 treats per hour")]
    [SlashCooldown(2, _hourSeconds, SlashCooldownBucketType.User)]
    public async Task TreatPet(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to give one of their pets a treat in Guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Treat, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token); 
    }

    [SlashCommand("Search", "Search for a new pet. Allows 10 searches per hour")]
    [SlashCooldown(10, _hourSeconds, SlashCooldownBucketType.User)]
    public async Task Search(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Search, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [SlashCommand("Bonus", "View the bonuses from all your pets available in this server")]
    [SlashCooldown(3, 60, SlashCooldownBucketType.User)]
    public async Task Bonuses(InteractionContext context, [Option("TargetUser", "View the bonuses for another user")] DiscordUser otherUser = null)
    {
        var targetUser = otherUser != null ? (DiscordMember)otherUser : context.Member;
        _logger.LogInformation("[Slash Command] User [{UserId}] requested to view their applied bonuses in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ViewBonuses, new InteractionResponder(context, _errorHandlingService), context.Member, context.Guild, targetUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }
}