using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
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

        [Command("Search")]
        public async Task Search(CommandContext context)
        {
            Logger.LogInformation("User [{UserId}] started searching for a new pet in Guild [{GuildId}]", context.Member.Id, context.Guild.Id);

            if (!DataHelpers.Pets.SearchSuccess(context.Member))
            {
                await context.RespondAsync(EmbedGenerator.Info("You didn't find anything this time!\nTry again later", "Nothing Found"));
                return;
            }

            var hasSpace = DataHelpers.Pets.HasSpaceForAnotherPet(context.Member);
            string noSpaceMessage = "";
            if (!hasSpace)
            {
                noSpaceMessage = " But you don't have enough room for another pet!";
            }

            var foundPet = PetFactory.Generate();
            var initialPetDisplay = DataHelpers.Pets.GetPetDisplayEmbed(foundPet, includeName: false);

            var initialResponseBuilder = new DiscordMessageBuilder()
                .WithContent($"You found a new potential friend!{noSpaceMessage}")
                .WithEmbed(initialPetDisplay)
                .AddComponents(new DiscordComponent[] {
                    Interactions.Pets.Befriend.Disable(!hasSpace),
                    Interactions.Pets.Leave
                });

            var message = await context.RespondAsync(initialResponseBuilder);
            var result = await message.WaitForButtonAsync(context.Member);

            if (!result.TimedOut)
            {
                initialResponseBuilder.ClearComponents();
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(initialResponseBuilder));
                // TODO: Send next message in chain based on choice
            }
            else
            {
                await message.DeleteAsync();
                await context.RespondAsync(DataHelpers.Pets.GetPetRanAwayMessage(foundPet));
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
