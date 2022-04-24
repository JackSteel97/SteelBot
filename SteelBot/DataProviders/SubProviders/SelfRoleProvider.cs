using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class SelfRolesProvider
    {
        private readonly ILogger<SelfRolesProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;

        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, SelfRole>> SelfRolesByGuildAndId;

        public SelfRolesProvider(ILogger<SelfRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            SelfRolesByGuildAndId = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, SelfRole>>();
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
            var guildRoles = SelfRolesByGuildAndId.GetOrAdd(guildId, _ => new ConcurrentDictionary<ulong, SelfRole>());

            guildRoles.TryAdd(role.DiscordRoleId, role);
        }

        private void RemoveRoleFromInternalCache(ulong guildId, ulong roleId)
        {
            if (SelfRolesByGuildAndId.TryGetValue(guildId, out var roles))
            {
                roles.TryRemove(roleId, out _);
            }
        }

        public bool BotKnowsRole(ulong guildId, ulong roleId)
        {
            if (SelfRolesByGuildAndId.TryGetValue(guildId, out var roles))
            {
                return roles.ContainsKey(roleId);
            }
            return false;
        }

        public bool TryGetRole(ulong guildId, ulong roleId, out SelfRole role)
        {
            if (SelfRolesByGuildAndId.TryGetValue(guildId, out var roles))
            {
                return roles.TryGetValue(roleId, out role);
            }
            role = null;
            return false;
        }

        public bool TryGetRole(ulong guildId, string roleName, out SelfRole role)
        {
            if(SelfRolesByGuildAndId.TryGetValue(guildId, out var guildRoles))
            {
                role = guildRoles.Values.FirstOrDefault(x => x.RoleName.Equals(roleName, System.StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                role = null;
            }
            return role != null;
        }

        public bool TryGetGuildRoles(ulong guildId, out List<SelfRole> roles)
        {
            if(SelfRolesByGuildAndId.TryGetValue(guildId, out var indexedRoles))
            {
                roles = indexedRoles.Values.ToList();
                return true;
            }
            roles = new List<SelfRole>();
            return false;
        }

        public async Task AddRole(ulong guildId, SelfRole role)
        {
            if (!BotKnowsRole(guildId, role.DiscordRoleId))
            {
                await InsertSelfRole(guildId, role);
            }
        }

        public async Task RemoveRole(ulong guildId, ulong roleId)
        {
            if (TryGetRole(guildId, roleId, out SelfRole role))
            {
                await DeleteSelfRole(guildId, role);
            }
        }

        private async Task InsertSelfRole(ulong guildId, SelfRole role)
        {
            Logger.LogInformation("Writing a new Self Role {RoleName} for Guild {GuildId} to the database", role.RoleName, guildId);

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
                Logger.LogError("Writing Self Role {RoleName} for Guild {GuildId} to the database inserted no entities. The internal cache was not changed", role.RoleName, guildId);
            }
        }

        private async Task DeleteSelfRole(ulong guildId, SelfRole role)
        {
            Logger.LogInformation("Deleting Self Role {RoleName} for Guild {GuildId} from the database", role.RoleName, guildId);

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.SelfRoles.Remove(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveRoleFromInternalCache(guildId, role.DiscordRoleId);
            }
            else
            {
                Logger.LogWarning("Deleting Self Role {RoleName} for Guild {GuildId} from the database deleted no entities. The internal cache was not changed", role.RoleName, guildId);
            }
        }
    }
}