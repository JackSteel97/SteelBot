﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
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
        [Cooldown(10, 60, CooldownBucketType.User)]
        public Task GetPets(CommandContext context, DiscordMember otherUser = null)
        {
            Logger.LogInformation("User [{UserId}] requested to view their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);

            otherUser ??= context.Member;
            _ = DataHelpers.Pets.SendOwnedPetsDisplay(context, otherUser);
            return Task.CompletedTask;
        }

        [Command("manage")]
        [Description("Manage your owned pets")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public Task ManagePets(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to manage their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);

            _ = DataHelpers.Pets.HandleManage(context);
            return Task.CompletedTask;
        }

        [Command("treat")]
        [Aliases("reward", "gift")]
        [Description("Give one of your pets a treat, boosting their XP instantly. Allows 2 treats per twelve hours")]
        [Cooldown(2, TwelveHoursSeconds, CooldownBucketType.User)]
        public Task TreatPet(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to give one of their pets a treat in Guild [{GuildId}]", context.User.Id, context.Guild.Id);

            _ = DataHelpers.Pets.HandleTreat(context);
            return Task.CompletedTask;
        }

        [Command("Search")]
        [Description("Search for a new pet. Allows 10 searches per hour.")]
        [Cooldown(10, HourSeconds, CooldownBucketType.User)]
        public Task Search(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            _ = DataHelpers.Pets.HandleSearch(context);
            return Task.CompletedTask;
        }

        [Command("Bonus")]
        [Aliases("Bonuses", "b")]
        [Description("View the bonuses from all your pets available in this server")]
        [Cooldown(3, 60, CooldownBucketType.User)]
        public Task Bonuses(CommandContext context, DiscordMember otherUser = null)
        {
            Logger.LogInformation("User [{UserId}] requested to view their applied bonuses in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            otherUser ??= context.Member;
            _ = DataHelpers.Pets.SendPetBonusesDisplay(context, otherUser);
            return Task.CompletedTask;
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
                .AddField("Legendary", $"{countByRarity[Rarity.Legendary]} ({countByRarity[Rarity.Legendary] / count:P2})", true)
                .AddField("Mythical", $"{countByRarity[Rarity.Mythical]} ({countByRarity[Rarity.Mythical] / count:P2})", true);

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
