using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.DataProviders.SubProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Responders;
using SteelBot.Services;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun;

[SlashCommandGroup("Fun", "Commands for fun"), SlashRequireGuild]
public class FunSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly FunProvider _funProvider;
    private ExceptionProvider _exceptionProvider;
    private readonly ErrorHandlingService _errorHandlingService;
    private readonly ILogger<FunSlashCommands> _logger;

    /// <inheritdoc />
    public FunSlashCommands(FunProvider funProvider, ILogger<FunSlashCommands> logger, ErrorHandlingService errorHandlingService, ExceptionProvider exceptionProvider) : base(logger)
    {
        _funProvider = funProvider;
        _logger = logger;
        _errorHandlingService = errorHandlingService;
        _exceptionProvider = exceptionProvider;
    }

    [SlashCommand("Joke", "Gets a joke courtesy of [Jokes.One](https://jokes.one/)")]
    [SlashCooldown(3, 60, SlashCooldownBucketType.Channel)]
    public async Task TellMeAJoke(InteractionContext context)
    {
        _logger.LogInformation("[Slash Command] User {UserId} requested today's Joke", context.User.Id);
        var jokeWrapper = await _funProvider.GetJoke();
        var joke = jokeWrapper.Jokes[0];
        var responder = new InteractionResponder(context, _errorHandlingService);
        responder.Respond(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Info(joke.Joke.Text, "Joke of the Day", $"© {jokeWrapper.Copyright}")));
    }

    [SlashCommand("Inspiration", "Gets an AI generated motivational quote image")]
    [SlashCooldown(5, 60, SlashCooldownBucketType.User)]
    public async Task GetInspiration(InteractionContext context)
    {
        var responder = new InteractionResponder(context, _errorHandlingService);
        var imageStream = await FunDataHelper.GetMotivationalQuote();

        if (imageStream != null)
        {
            var msg = new DiscordMessageBuilder().AddFile("MotivationalQuote.jpg", imageStream);
            responder.Respond(msg);
        }
        else
        {
            _logger.LogWarning("Failed to generate a motivational quote");
            responder.Respond(new DiscordMessageBuilder().WithEmbed(EmbedGenerator.Error("Failed to generate a motivational quote, please try again later.")), true);
            await _exceptionProvider.InsertException(new Database.Models.ExceptionLog(new NullReferenceException("Motivational Quote stream cannot be null"), nameof(GetInspiration)));
        }
    }
}