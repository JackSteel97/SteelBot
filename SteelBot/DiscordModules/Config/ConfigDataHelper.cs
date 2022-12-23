using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config;

public class ConfigDataHelper
{
    private readonly AppConfigurationService _appConfigurationService;
    private readonly DataCache _cache;
    private readonly ILogger<ConfigDataHelper> _logger;

    public ConfigDataHelper(ILogger<ConfigDataHelper> logger, DataCache cache, AppConfigurationService appConfigurationService)
    {
        _logger = logger;
        _cache = cache;
        _appConfigurationService = appConfigurationService;
    }

    public async Task SetPrefix(ulong guildId, string newPrefix)
    {
        _logger.LogInformation("Setting bot prefix for Guild {GuildId} to {NewPrefix}", guildId, newPrefix);
        await _cache.Guilds.SetNewPrefix(guildId, newPrefix);
    }

    public async Task SetLevellingChannel(ulong guildId, ulong channelId)
    {
        _logger.LogInformation("Setting Levelling Channel for Guild {GuildId} to {ChannelId}", guildId, channelId);
        await _cache.Guilds.SetLevellingChannel(guildId, channelId);
    }

    public Task<bool> ToggleDadJoke(ulong guildId)
    {
        _logger.LogInformation("Toggling Dad Joke Detection for Guild {GuildId}", guildId);
        return _cache.Guilds.ToggleDadJoke(guildId);
    }

    public Task<bool> ToggleLevelMentions(ulong guildId, ulong userId)
    {
        _logger.LogInformation("Toggling Level Mentions for User {UserId} in Guild {GuildId}", userId, guildId);
        return _cache.Users.ToggleLevelMention(guildId, userId);
    }

    public string GetPrefix(ulong guildId) => _cache.Guilds.GetGuildPrefix(guildId);

    public string GetEnvironment() => _appConfigurationService.Environment;

    public string GetVersion() => _appConfigurationService.Version;
}