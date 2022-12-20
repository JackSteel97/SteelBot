using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Channels.Pets;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.RateLimiting;
using SteelBot.Responders;
using SteelBot.Services;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets;

[Group("Pet")]
[Aliases("Pets")]
[Description("Commands for interacting with user pets")]
[RequireGuild]
public class PetsCommands : TypingCommandModule
{
    private readonly ILogger<PetsCommands> _logger;
    private readonly PetFactory _petFactory;
    private readonly DataHelpers _dataHelpers;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly PetCommandsChannel _petCommandsChannel;
    private readonly CancellationService _cancellationService;
    
    private readonly RateLimit _viewPetsRateLimit;
    private readonly RateLimit _manageRateLimit;
    private readonly RateLimit _treatRateLimit;
    private readonly RateLimit _searchRateLimit;
    private readonly RateLimit _bonusesRateLimit;
    public PetsCommands(ILogger<PetsCommands> logger, PetFactory petFactory, DataHelpers dataHelpers, IHub sentry, ErrorHandlingService errorHandlingService, PetCommandsChannel petCommandsChannel, CancellationService cancellationService, RateLimitFactory rateLimitFactory, AuditLogService auditLogService)
        : base(logger, auditLogService, sentry)
    {
        _logger = logger;
        _petFactory = petFactory;
        _dataHelpers = dataHelpers;
        _errorHandlingService = errorHandlingService;
        _petCommandsChannel = petCommandsChannel;
        _cancellationService = cancellationService;
        
        _viewPetsRateLimit = rateLimitFactory.Get(nameof(ViewPets), 10, TimeSpan.FromMinutes(1));
        _manageRateLimit = rateLimitFactory.Get(nameof(ManagePets), 10, TimeSpan.FromMinutes(1));
        _treatRateLimit = rateLimitFactory.Get(nameof(TreatPet), 2, TimeSpan.FromHours(1));
        _searchRateLimit = rateLimitFactory.Get(nameof(Search), 10, TimeSpan.FromHours(1));
        _bonusesRateLimit = rateLimitFactory.Get(nameof(Bonuses), 3, TimeSpan.FromMinutes(1));
    }

    [GroupCommand]
    [Description("Show all your owned pets")]
    public async Task ViewPets(CommandContext context, DiscordMember otherUser = null)
    {
        _viewPetsRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("User [{UserId}] requested to view their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.View, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, otherUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("manage")]
    [Description("Manage your owned pets")]
    public async Task ManagePets(CommandContext context)
    {
        _manageRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("User [{UserId}] requested to manage their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ManageAll, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("treat")]
    [Aliases("reward", "gift")]
    [Description("Give one of your pets a treat, boosting their XP instantly. Allows 2 treats per hour")]
    public async Task TreatPet(CommandContext context)
    {
        _treatRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("User [{UserId}] requested to give one of their pets a treat in Guild [{GuildId}]", context.User.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Treat, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Search")]
    [Description("Search for a new pet. Allows 10 searches per hour.")]
    public async Task Search(CommandContext context)
    {
        _searchRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.Search, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("Bonus")]
    [Aliases("Bonuses", "b")]
    [Description("View the bonuses from all your pets available in this server")]
    public async Task Bonuses(CommandContext context, DiscordMember otherUser = null)
    {
        _bonusesRateLimit.ThrowIfExceeded(context.User.Id);
        _logger.LogInformation("User [{UserId}] requested to view their applied bonuses in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
        var message = new PetCommandAction(PetCommandActionType.ViewBonuses, new MessageResponder(context, _errorHandlingService), context.Member, context.Guild, otherUser);
        await _petCommandsChannel.Write(message, _cancellationService.Token);
    }

    [Command("DebugStats")]
    [RequireOwner]
    public async Task GenerateLots(CommandContext context, double count)
    {
        var countByRarity = new ConcurrentDictionary<Rarity, int>();

        var start = DateTime.UtcNow;
        Parallel.For(0, (int)count, _ =>
        {
            var pet = _petFactory.Generate(1);
            countByRarity.AddOrUpdate(pet.Rarity, 1, (_, v) => ++v);
        });
        var end = DateTime.UtcNow;

        var embed = new DiscordEmbedBuilder().WithTitle("Stats").WithColor(EmbedGenerator.InfoColour)
            .AddField("Generated", count.ToString(), true)
            .AddField("Took", (end - start).Humanize(), true)
            .AddField("Average Per Pet", $"{((end - start) / count).TotalMilliseconds * 1000} μs", true)
            .AddField("Common", $"{countByRarity[Rarity.Common]} ({countByRarity[Rarity.Common] / count:P2})", true)
            .AddField("Uncommon", $"{countByRarity[Rarity.Uncommon]} ({countByRarity[Rarity.Uncommon] / count:P2})", true)
            .AddField("Rare", $"{countByRarity[Rarity.Rare]} ({countByRarity[Rarity.Rare] / count:P2})", true)
            .AddField("Epic", $"{countByRarity[Rarity.Epic]} ({countByRarity[Rarity.Epic] / count:P2})", true)
            .AddField("Legendary", $"{countByRarity[Rarity.Legendary]} ({countByRarity[Rarity.Legendary] / count:P2})", true)
            .AddField("Mythical", $"{countByRarity[Rarity.Mythical]} ({countByRarity[Rarity.Mythical] / count:P2})", true);

        await context.RespondAsync(embed);
    }
}