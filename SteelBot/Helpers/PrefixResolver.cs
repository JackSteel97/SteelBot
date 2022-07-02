using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using SteelBot.DiscordModules.Config;

namespace SteelBot.Helpers;

public static class PrefixResolver
{
    public static int Resolve(DiscordMessage msg, DiscordUser currentUser, ConfigDataHelper configDataHelper)
    {
        string guildsPrefix = configDataHelper.GetPrefix(msg.Channel.GuildId.Value);

        int prefixFound = msg.GetStringPrefixLength(guildsPrefix);
        if (prefixFound == -1)
        {
            prefixFound = msg.GetMentionPrefixLength(currentUser);
        }

        return prefixFound;
    }

    public static bool IsPrefixedCommand(DiscordMessage msg, DiscordUser currentUser, ConfigDataHelper configDataHelper)
    {
        int prefixFound = Resolve(msg, currentUser, configDataHelper);
        return prefixFound != -1;
    }
}