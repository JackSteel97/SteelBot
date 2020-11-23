using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    public class RankRoleDataHelper
    {
        private readonly ILogger<RankRoleDataHelper> Logger;
        private readonly DataCache Cache;

        public RankRoleDataHelper(DataCache cache, ILogger<RankRoleDataHelper> logger)
        {
            Cache = cache;
            Logger = logger;
        }

        public async Task CreateSelfRole(ulong guildId, string roleName, int requiredRank)
        {
            Logger.LogInformation($"Request to create rank role [{roleName}] in Guild [{guildId}] received");
            if (Cache.Guilds.TryGetGuild(guildId, out Guild guild))
            {
                RankRole role = new RankRole(roleName, guild.RowId, requiredRank);
                await Cache.RankRoles.AddRole(guildId, role);
            }
            else
            {
                Logger.LogWarning($"Could not create rank role because Guild [{guildId}] does not exist.");
            }
        }

        public async Task DeleteSelfRole(ulong guildId, string roleName)
        {
            Logger.LogInformation($"Request tot delete self role [{roleName}] in Guild [{guildId}] received.");
            await Cache.RankRoles.RemoveRole(guildId, roleName);
        }
    }
}