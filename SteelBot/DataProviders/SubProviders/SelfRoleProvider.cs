using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class SelfRolesProvider
    {
        private readonly ILogger<SelfRolesProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;

        private readonly Dictionary<ulong, Dictionary<string, SelfRole>> SelfRolesByGuildAndRole;

        public SelfRolesProvider(ILogger<SelfRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            SelfRolesByGuildAndRole = new Dictionary<ulong, Dictionary<string, SelfRole>>();
            LoadSelfRoleData();
        }

        private void LoadSelfRoleData()
        {
            Logger.LogInformation("Loading data from database: SelfRoles");

            SelfRole[] allRoles;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                allRoles = db.SelfRoles.AsNoTracking().Include(sr => sr.Guild).ToArray();
            }

            foreach (SelfRole role in allRoles)
            {
                AddRoleToInternalCache(role.Guild.DiscordId, role);
            }
        }

        private void AddRoleToInternalCache(ulong guildId, SelfRole role)
        {
            if (!SelfRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, SelfRole> roles))
            {
                roles = new Dictionary<string, SelfRole>();
                SelfRolesByGuildAndRole.Add(guildId, roles);
            }
            if (!roles.ContainsKey(role.RoleName.ToLower()))
            {
                roles.Add(role.RoleName.ToLower(), role);
            }
        }

        private void RemoveRoleFromInternalCache(ulong guildId, string roleName)
        {
            if (SelfRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, SelfRole> roles))
            {
                if (roles.ContainsKey(roleName.ToLower()))
                {
                    roles.Remove(roleName.ToLower());
                }
            }
        }

        public bool BotKnowsRole(ulong guildId, string roleName)
        {
            if (SelfRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, SelfRole> roles))
            {
                return roles.ContainsKey(roleName.ToLower());
            }
            return false;
        }

        public bool TryGetRole(ulong guildId, string roleName, out SelfRole role)
        {
            if (SelfRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, SelfRole> roles))
            {
                return roles.TryGetValue(roleName.ToLower(), out role);
            }
            role = null;
            return false;
        }

        public bool TryGetGuildRoles(ulong guildId, out Dictionary<string, SelfRole> roles)
        {
            return SelfRolesByGuildAndRole.TryGetValue(guildId, out roles);
        }

        public async Task AddRole(ulong guildId, SelfRole role)
        {
            if (!BotKnowsRole(guildId, role.RoleName))
            {
                await InsertSelfRole(guildId, role);
            }
        }

        public async Task RemoveRole(ulong guildId, string roleName)
        {
            if (TryGetRole(guildId, roleName, out SelfRole role))
            {
                await DeleteSelfRole(guildId, role);
            }
        }

        private async Task InsertSelfRole(ulong guildId, SelfRole role)
        {
            Logger.LogInformation($"Writing a new Self Role [{role.RoleName}] for Guild [{guildId}] to the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.SelfRoles.Add(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                AddRoleToInternalCache(guildId, role);
            }
            else
            {
                Logger.LogError($"Writing Self Role [{role.RoleName}] for Guild [{guildId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task DeleteSelfRole(ulong guildId, SelfRole role)
        {
            Logger.LogInformation($"Deleting Self Role [{role.RoleName}] for Guild [{guildId}] from the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.SelfRoles.Remove(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveRoleFromInternalCache(guildId, role.RoleName);
            }
            else
            {
                Logger.LogWarning($"Deleting Self Role [{role.RoleName}] for Guild [{guildId}] from the database deleted no entities. The internal cache was not changed.");
            }
        }
    }
}