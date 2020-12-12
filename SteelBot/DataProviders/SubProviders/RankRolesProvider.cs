using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class RankRolesProvider
    {
        private readonly ILogger<RankRolesProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly Dictionary<ulong, Dictionary<string, RankRole>> RankRolesByGuildAndRole;

        public RankRolesProvider(ILogger<RankRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            RankRolesByGuildAndRole = new Dictionary<ulong, Dictionary<string, RankRole>>();
            LoadRankRoleData();
        }

        public void LoadRankRoleData()
        {
            Logger.LogInformation("Loading data from database: RankRoles");
            RankRole[] allRoles;
            using (var db = DbContextFactory.CreateDbContext())
            {
                allRoles = db.RankRoles.AsNoTracking().Include(rr => rr.Guild).ToArray();
            }
            foreach (var role in allRoles)
            {
                AddRoleToInternalCache(role.Guild.DiscordId, role);
            }
        }

        private void AddRoleToInternalCache(ulong guildId, RankRole role)
        {
            if (!RankRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, RankRole> roles))
            {
                roles = new Dictionary<string, RankRole>();
                RankRolesByGuildAndRole.Add(guildId, roles);
            }
            if (!roles.ContainsKey(role.RoleName.ToLower()))
            {
                roles.Add(role.RoleName.ToLower(), role);
            }
        }

        private void RemoveRoleFromInternalCache(ulong guildId, string roleName)
        {
            if (RankRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, RankRole> roles))
            {
                if (roles.ContainsKey(roleName.ToLower()))
                {
                    roles.Remove(roleName.ToLower());
                }
            }
        }

        public bool BotKnowsRole(ulong guildId, string roleName)
        {
            if (RankRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, RankRole> roles))
            {
                return roles.ContainsKey(roleName.ToLower());
            }
            return false;
        }

        public bool TryGetRole(ulong guildId, string roleName, out RankRole role)
        {
            if (RankRolesByGuildAndRole.TryGetValue(guildId, out Dictionary<string, RankRole> roles))
            {
                return roles.TryGetValue(roleName.ToLower(), out role);
            }
            role = null;
            return false;
        }

        public bool TryGetGuildRankRoles(ulong guildId, out Dictionary<string, RankRole> roles)
        {
            return RankRolesByGuildAndRole.TryGetValue(guildId, out roles);
        }

        public async Task AddRole(ulong guildId, RankRole role)
        {
            if (!BotKnowsRole(guildId, role.RoleName))
            {
                await InsertRankRole(guildId, role);
            }
        }

        public async Task RemoveRole(ulong guildId, string roleName)
        {
            if (TryGetRole(guildId, roleName, out RankRole role))
            {
                await DeleteRankRole(guildId, role);
            }
        }

        private async Task InsertRankRole(ulong guildId, RankRole role)
        {
            Logger.LogInformation($"Writing a new Rank Role [{role.RoleName}] for Guild [{guildId}] to the database.");
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.RankRoles.Add(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                AddRoleToInternalCache(guildId, role);
            }
            else
            {
                Logger.LogError($"Writing Rank Role [{role.RoleName}] for Guild [{guildId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task DeleteRankRole(ulong guildId, RankRole role)
        {
            Logger.LogInformation($"Deleting Rank Role [{role.RoleName}] for Guild [{guildId}] from the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.RankRoles.Remove(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveRoleFromInternalCache(guildId, role.RoleName);
            }
            else
            {
                Logger.LogWarning($"Deleting Rank Role [{role.RoleName}] for Guild [{guildId}] from the database deleted no entities. The internal cache was not changed.");
            }
        }
    }
}