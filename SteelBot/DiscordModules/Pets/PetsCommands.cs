using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Pets
{
    [Group("Pet")]
    [Aliases("Pets")]
    [Description("Commands for interacting with user pets")]
    [RequireGuild]
    public class PetsCommands : TypingCommandModule
    {
        private readonly ILogger<PetsCommands> Logger;
        private readonly PetFactory PetFactory;
        private readonly DataHelpers DataHelpers;
        private const double TwelveHoursSeconds = 12 * 60 * 60;
        private const double HourSeconds = 60 * 60;

        public PetsCommands(ILogger<PetsCommands> logger, PetFactory petFactory, DataHelpers dataHelpers)
        {
            Logger = logger;
            PetFactory = petFactory;
            DataHelpers = dataHelpers;
        }

        [GroupCommand]
        [Description("Show all your owned pets")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task GetPets(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to view their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);

            await DataHelpers.Pets.SendOwnedPetsDisplay(context);
        }

        [Command("manage")]
        [Description("Manage your owned pets")]
        [Cooldown(3, 60, CooldownBucketType.User)]
        public async Task ManagePets(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to manage their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);

            await DataHelpers.Pets.HandleManage(context);
        }

        [Command("treat")]
        [Aliases("reward", "gift")]
        [Description("Give one of your pets a treat, boosting their XP instantly. Allows 2 treats per twelve hours")]
        [Cooldown(2, TwelveHoursSeconds, CooldownBucketType.User)]
        public async Task TreatPet(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to give one of their pets a treat in Guild [{GuildId}]", context.User.Id, context.Guild.Id);

            await DataHelpers.Pets.HandleTreat(context);
        }

        [Command("Search")]
        [Description("Search for a new pet. Allows 5 searches per hour.")]
        [Cooldown(5, HourSeconds, CooldownBucketType.User)]
        public async Task Search(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            await DataHelpers.Pets.HandleSearch(context);
        }

        [Command("Bonus")]
        [Aliases("Bonuses", "b")]
        [Description("View the bonuses from all your pets available in this server")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task Bonuses(CommandContext context, DiscordMember otherUser = null)
        {
            Logger.LogInformation("User [{UserId}] requested to view their applied bonuses in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            otherUser ??= context.Member;
            await DataHelpers.Pets.SendPetBonusesDisplay(context, otherUser);
        }

        [Command("DebugStats")]
        [RequireOwner]
        public async Task GenerateLots(CommandContext context, double count)
        {
            var countByRarity = new Dictionary<Rarity, int>();

            var start = DateTime.UtcNow;
            for (int i = 0; i < count; ++i)
            {
                var pet = PetFactory.Generate();
                if (!countByRarity.ContainsKey(pet.Rarity))
                {
                    countByRarity.Add(pet.Rarity, 1);
                }
                ++countByRarity[pet.Rarity];
            }
            var end = DateTime.UtcNow;

            var embed = new DiscordEmbedBuilder().WithTitle("Stats").WithColor(EmbedGenerator.InfoColour)
                .AddField("Generated", count.ToString(), true)
                .AddField("Took", (end - start).Humanize(), true)
                .AddField("Average Per Pet", $"{((end - start) / count).TotalMilliseconds * 1000} μs", true)
                .AddField("Common", $"{countByRarity[Rarity.Common]} ({countByRarity[Rarity.Common] / count:P2})", true)
                .AddField("Uncommon", $"{countByRarity[Rarity.Uncommon]} ({countByRarity[Rarity.Uncommon] / count:P2})", true)
                .AddField("Rare", $"{countByRarity[Rarity.Rare]} ({countByRarity[Rarity.Rare] / count:P2})", true)
                .AddField("Epic", $"{countByRarity[Rarity.Epic]} ({countByRarity[Rarity.Epic] / count:P2})", true)
                .AddField("Legendary", $"{countByRarity[Rarity.Legendary]} ({countByRarity[Rarity.Legendary] / count:P2})", true);

            await context.RespondAsync(embed);
        }

        [Command("Combos")]
        [RequireOwner]
        public async Task CalculateCombinations(CommandContext context)
        {
            var count = PetFactory.CountPossibilities();

            await context.RespondAsync(EmbedGenerator.Info($"There are a possible `{count:N0}` unique pet combinations", "Calculated"));
        }
    }
}
