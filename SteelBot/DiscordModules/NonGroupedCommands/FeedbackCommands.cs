using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.NonGroupedCommands;

[Description("Commands for providing feedback about the bot.")]
[RequireGuild]
public class FeedbackCommands : TypingCommandModule
{
    private readonly DataCache _cache;

    public FeedbackCommands(DataCache cache, IHub sentry, ILogger<FeedbackCommands> logger, AuditLogService auditLogService) : base(logger, auditLogService, sentry)
    {
        _cache = cache;
    }

    [Command("Good")]
    [Description("Adds a good bot vote.")]
    [Cooldown(1, 60, CooldownBucketType.User)]
    public async Task GoodBot(CommandContext context, [RemainingText] string remainder)
    {
        if (remainder != null && remainder.Equals("bot", StringComparison.OrdinalIgnoreCase))
        {
            await _cache.Guilds.IncrementGoodVote(context.Guild.Id);
            await context.RespondAsync(embed: EmbedGenerator.Info("Thank you!"));
        }
    }

    [Command("Bad")]
    [Description("Adds a bad bot vote.")]
    [Cooldown(1, 60, CooldownBucketType.User)]
    public async Task BadBot(CommandContext context, [RemainingText] string remainder)
    {
        if (remainder != null && remainder.Equals("bot", StringComparison.OrdinalIgnoreCase))
        {
            await _cache.Guilds.IncrementBadVote(context.Guild.Id);
            await context.RespondAsync(embed: EmbedGenerator.Info("I'm sorry!"));
        }
    }
}