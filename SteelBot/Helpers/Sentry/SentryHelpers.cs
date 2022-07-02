using DSharpPlus.Entities;
using Sentry;
using System.Collections.Generic;

namespace SteelBot.Helpers.Sentry;
public static class SentryHelpers
{
    public static User GetSentryUser(DiscordUser user, DiscordGuild guild)
    {
        return new User
        {
            Username = $"{user.Username}#{user.Discriminator}",
            Other = new Dictionary<string, string>
            {
                ["UserId"] = user.Id.ToString(),
                ["GuildId"] = guild.Id.ToString(),
                ["Guild"] = guild.Name
            }
        };
    }
}