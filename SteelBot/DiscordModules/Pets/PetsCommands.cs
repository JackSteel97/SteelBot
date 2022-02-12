using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.Helpers;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public PetsCommands(ILogger<PetsCommands> logger, PetFactory petFactory, DataHelpers dataHelpers)
        {
            Logger = logger;
            PetFactory = petFactory;
            DataHelpers = dataHelpers;
        }

        [GroupCommand]
        [Description("Show all your pets")]
        public async Task GetPets(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] requested to view their pets in guild [{GuildId}]", context.User.Id, context.Guild.Id);

            var embed = DataHelpers.Pets.GetOwnedPetsDisplayEmbed(context.Guild.Id, context.User.Id);
            await context.RespondAsync(embed);
        }

        [Command("Search")]
        [Description("Search for a new pet")]
        public async Task Search(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            if (!DataHelpers.Pets.SearchSuccess(context.Member))
            {
                await context.RespondAsync(EmbedGenerator.Info($"You didn't find anything this time!{Environment.NewLine}Try again later", "Nothing Found"));
                return;
            }

            (bool befriendAttempt, Pet pet) = await DataHelpers.Pets.HandleInitialSearchSuccess(context);
            bool newPet = false;
            if (befriendAttempt)
            {
                newPet = await DataHelpers.Pets.HandleBefriendAttempt(context, pet);
            }

            if (newPet)
            {
                await context.RespondAsync(DataHelpers.Pets.GetPetOwnedSuccessMessage(context.Member, pet));
            }
        }

        // TODO: Remove
        [GroupCommand]
        [Description("WIP: Generate a new pet")]
        public Task Generate(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] generated a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);
            var pet = PetFactory.Generate();
            var embed = DataHelpers.Pets.GetPetDisplayEmbed(pet);
            return context.RespondAsync(embed);
        }

        [Command("DebugStats")]
        public Task GenerateLots(CommandContext context, double count)
        {
            var countByRarity = new Dictionary<Rarity, int>();
            
            var start = DateTime.UtcNow;
            for (int i =0; i < count; ++i)
            {
                var pet = PetFactory.Generate();
                if(!countByRarity.ContainsKey(pet.Rarity))
                {
                    countByRarity.Add(pet.Rarity, 1);
                }
                ++countByRarity[pet.Rarity];
            }
            var end = DateTime.UtcNow;

            var embed = new DiscordEmbedBuilder().WithTitle("Stats").WithColor(EmbedGenerator.InfoColour)
                .AddField("Generated", count.ToString(), true)
                .AddField("Took", (end - start).Humanize(), true)
                .AddField("Average Per Pet", $"{((end - start) / count).TotalMilliseconds*1000} μs", true)
                .AddField("Common", $"{countByRarity[Rarity.Common]} ({countByRarity[Rarity.Common] / count:P2})", true)
                .AddField("Uncommon", $"{countByRarity[Rarity.Uncommon]} ({countByRarity[Rarity.Uncommon] / count:P2})", true)
                .AddField("Rare", $"{countByRarity[Rarity.Rare]} ({countByRarity[Rarity.Rare] / count:P2})", true)
                .AddField("Epic", $"{countByRarity[Rarity.Epic]} ({countByRarity[Rarity.Epic] / count:P2})", true)
                .AddField("Legendary", $"{countByRarity[Rarity.Legendary]} ({countByRarity[Rarity.Legendary] / count:P2})", true);

            return context.RespondAsync(embed);
        }
    }
}
