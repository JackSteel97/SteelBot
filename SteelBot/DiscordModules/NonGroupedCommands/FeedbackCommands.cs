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

namespace SteelBot.DiscordModules.NonGroupedCommands
{
    [Description("Commands for providing feedback about the bot.")]
    [RequireGuild]
    public class FeedbackCommands : TypingCommandModule
    {
        private readonly DataCache Cache;

        public FeedbackCommands(DataCache cache)
        {
            Cache = cache;
        }

        [Command("Good")]
        [Description("Adds a good bot vote.")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task GoodBot(CommandContext context, [RemainingText] string remainder)
        {
            if (remainder != null && remainder.Equals("bot", StringComparison.OrdinalIgnoreCase))
            {
                await Cache.Guilds.IncrementGoodVote(context.Guild.Id);
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
                await Cache.Guilds.IncrementBadVote(context.Guild.Id);
                await context.RespondAsync(embed: EmbedGenerator.Info("I'm sorry!"));
            }
        }
    }
}