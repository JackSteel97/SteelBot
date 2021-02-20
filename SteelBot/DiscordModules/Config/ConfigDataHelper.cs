using Microsoft.Extensions.Logging;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config
{
    public class ConfigDataHelper
    {
        private readonly ILogger<ConfigDataHelper> Logger;
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;

        public ConfigDataHelper(ILogger<ConfigDataHelper> logger, DataCache cache, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            Cache = cache;
            AppConfigurationService = appConfigurationService;
        }

        public async Task SetPrefix(ulong guildId, string newPrefix)
        {
            Logger.LogInformation($"Setting bot prefix for Guild [{guildId}] to [{newPrefix}]");
            await Cache.Guilds.SetNewPrefix(guildId, newPrefix);
        }

        public async Task SetLevellingChannel(ulong guildId, ulong channelId)
        {
            Logger.LogInformation($"Setting Levelling Channel for Guild [{guildId}] to [{channelId}]");
            await Cache.Guilds.SetLevellingChannel(guildId, channelId);
        }

        public string GetPrefix(ulong guildId)
        {
            return Cache.Guilds.GetGuildPrefix(guildId);
        }

        public string GetEnvironment()
        {
            return AppConfigurationService.Environment;
        }

        public string GetVersion()
        {
            return AppConfigurationService.Version;
        }
    }
}