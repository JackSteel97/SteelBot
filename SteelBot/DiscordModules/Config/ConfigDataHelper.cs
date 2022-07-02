using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config;

public class ConfigDataHelper
{
    private readonly ILogger<ConfigDataHelper> _logger;
    private readonly DataCache _cache;
    private readonly AppConfigurationService _appConfigurationService;

    public ConfigDataHelper(ILogger<ConfigDataHelper> logger, DataCache cache, AppConfigurationService appConfigurationService)
    {
        _logger = logger;
        _cache = cache;
        _appConfigurationService = appConfigurationService;
    }

    public async Task SetPrefix(ulong guildId, string newPrefix)
    {
        _logger.LogInformation($"Setting bot prefix for Guild [{guildId}] to [{newPrefix}]");
        await _cache.Guilds.SetNewPrefix(guildId, newPrefix);
    }

    public async Task SetLevellingChannel(ulong guildId, ulong channelId)
    {
        _logger.LogInformation($"Setting Levelling Channel for Guild [{guildId}] to [{channelId}]");
        await _cache.Guilds.SetLevellingChannel(guildId, channelId);
    }

    public string GetPrefix(ulong guildId) => _cache.Guilds.GetGuildPrefix(guildId);

    public string GetEnvironment() => _appConfigurationService.Environment;

    public string GetVersion() => _appConfigurationService.Version;
}