using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Sentry;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions
{
    public static class CommandContextExtensions
    {
        public static Task<DiscordMessage> RespondAsync(this CommandContext context, DiscordEmbed embed, bool mention)
        {
            var message = new DiscordMessageBuilder().WithEmbed(embed);
            return context.RespondAsync(message, mention);
        }

        public static Task<DiscordMessage> RespondAsync(this CommandContext context, DiscordEmbedBuilder embed, bool mention)
        {
            var message = new DiscordMessageBuilder().WithEmbed(embed);
            return context.RespondAsync(message, mention);
        }

        public static Task<DiscordMessage> RespondAsync(this CommandContext context, DiscordMessageBuilder messageBuilder, bool mention)
        {
            return context.Channel.SendMessageAsync(messageBuilder.WithReply(context.Message.Id, mention));
        }

        public static User GetSentryUser(this CommandContext context)
        {
            return new User
            {
                Username = $"{context.User.Username}#{context.User.Discriminator}",
                Other = new Dictionary<string, string>
                {
                    ["UserId"] = context.User.Id.ToString(),
                    ["GuildId"] = context.Guild.Id.ToString(),
                    ["Guild"] = context.Guild.Name
                }
            };
        }
    }
}
