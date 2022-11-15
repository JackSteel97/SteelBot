using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun;

[Group("Fun")]
[Aliases("f")]
[Description("Commands for fun.")]
[RequireGuild]
public class FunCommands : TypingCommandModule
{
    private readonly DataHelpers _dataHelpers;
    private readonly DataCache _cache;

    public FunCommands(DataHelpers dataHelpers, DataCache cache, IHub sentry, ILogger<FunCommands> logger) : base(logger, sentry)
    {
        _dataHelpers = dataHelpers;
        _cache = cache;
    }

    [Command("Joke")]
    [Aliases("j")]
    [Description("Gets a joke courtesy of [Jokes.One](https://jokes.one/)")]
    [Cooldown(3, 60, CooldownBucketType.Channel)]
    public async Task TellMeAJoke(CommandContext context)
    {
        var jokeWrapper = await _cache.Fun.GetJoke();
        var joke = jokeWrapper.Jokes[0];
        await context.RespondAsync(embed: EmbedGenerator.Info(joke.Joke.Text, "Joke of The Day", $"© {jokeWrapper.Copyright}"));
    }

    [Command("Inspo")]
    [Aliases("Inspiration", "Motivate", "Quote")]
    [Description("Gets an AI generated motivational quote")]
    [Cooldown(5, 60, CooldownBucketType.User)]
    public async Task GetInspiration(CommandContext context)
    {
        var imageStream = await FunDataHelper.GetMotivationalQuote();

        if (imageStream != null)
        {
            var msg = new DiscordMessageBuilder().WithFile("MotivationalQuote.jpg", imageStream);
            await context.RespondAsync(msg);
        }
        else
        {
            await context.RespondAsync(embed: EmbedGenerator.Error("Failed to generate a motivational quote, please try again later."));
            await _cache.Exceptions.InsertException(new Database.Models.ExceptionLog(new NullReferenceException("Motivational Quote stream cannot be null"), nameof(GetInspiration)));
        }
    }
}