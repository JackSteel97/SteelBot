using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun
{
    [Group("Fun")]
    [Aliases("f")]
    [Description("Commands for fun.")]
    [RequireGuild]
    public class FunCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelpers;
        private readonly DataCache Cache;

        public FunCommands(DataHelpers dataHelpers, DataCache cache)
        {
            DataHelpers = dataHelpers;
            Cache = cache;
        }

        [Command("Joke")]
        [Aliases("j")]
        [Description("Gets a joke courtesy of [Jokes.One](https://jokes.one/)")]
        public async Task TellMeAJoke(CommandContext context)
        {
            var jokeWrapper = await Cache.Fun.GetJoke();
            var joke = jokeWrapper.Jokes[0];
            await context.RespondAsync(embed: EmbedGenerator.Info(joke.Joke.Text, "Joke of The Day", $"© {jokeWrapper.Copyright}"));
        }
    }
}