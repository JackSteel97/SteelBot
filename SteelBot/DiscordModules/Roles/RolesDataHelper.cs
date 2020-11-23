using Microsoft.Extensions.Logging;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteelBot.DiscordModules.Roles
{
    public class RolesDataHelper
    {
        private readonly ILogger<RolesDataHelper> Logger;
        private readonly DataCache Cache;
        private readonly AppConfigurationService AppConfigurationService;

        public RolesDataHelper(ILogger<RolesDataHelper> logger, DataCache cache, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            Cache = cache;
            AppConfigurationService = appConfigurationService;
        }

        public bool IsSelfRole(ulong guildId, string roleName)
        {
            return Cache.SelfRoles.BotKnowsRole(guildId, roleName);
        }

        public List<SelfRole> GetSelfRoles(ulong guildId)
        {
            List<SelfRole> result = null;
            if (Cache.SelfRoles.TryGetGuildRoles(guildId, out Dictionary<string, SelfRole> roles))
            {
                result = roles.Values.OrderBy(r=>r.RoleName).ToList();
            }
            return result;
        }
    }
}
