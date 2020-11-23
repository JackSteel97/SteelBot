using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
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

        public async Task CreateSelfRole(ulong guildId, string roleName, string description)
        {
            Logger.LogInformation($"Request to create self role [{roleName}] in Guild [{guildId}] received.");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                SelfRole role = new SelfRole(roleName, guild.RowId, description);
                await Cache.SelfRoles.AddRole(guildId, role);
            }
            else
            {
                Logger.LogWarning($"Could not create self role because Guild [{guild}] does not exist");
            }
        }

        public async Task DeleteSelfRole(ulong guildId, string roleName)
        {
            Logger.LogInformation($"Request to delete self role [{roleName}] in Guild [{guildId}] received.");
            await Cache.SelfRoles.RemoveRole(guildId, roleName);
        }
    }
}