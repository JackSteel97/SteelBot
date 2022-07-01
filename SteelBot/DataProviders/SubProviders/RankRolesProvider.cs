using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Helpers.Sentry;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class RankRolesProvider
    {
        private readonly ILogger<RankRolesProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, RankRole>> RankRolesByGuildAndRole;
        private readonly IHub _sentry;

        public RankRolesProvider(ILogger<RankRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory, IHub sentry)
        {
            Logger = logger;
            DbContextFactory = contextFactory;
            _sentry = sentry;

            RankRolesByGuildAndRole = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, RankRole>>();
            LoadRankRoleData();
        }

        private void LoadRankRoleData()
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(LoadRankRoleData));

            Logger.LogInformation("Loading data from database: RankRoles");
            RankRole[] allRoles;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                allRoles = db.RankRoles.AsNoTracking().Include(rr => rr.Guild).ToArray();
            }
            foreach (RankRole role in allRoles)
            {
                AddRoleToInternalCache(role.Guild.DiscordId, role);
            }

            transaction.Finish();
        }

        private void AddRoleToInternalCache(ulong guildId, RankRole role)
        {
            var guildRoles = RankRolesByGuildAndRole.GetOrAdd(guildId, _ => new ConcurrentDictionary<ulong, RankRole>());
            guildRoles.TryAdd(role.RoleDiscordId, role);
        }

        private void RemoveRoleFromInternalCache(ulong guildId, ulong roleId)
        {
            if(RankRolesByGuildAndRole.TryGetValue(guildId, out var guildRoles))
            {
                guildRoles.TryRemove(roleId, out _);
            }
        }

        public bool BotKnowsRole(ulong guildId, ulong roleId)
        {
            if (RankRolesByGuildAndRole.TryGetValue(guildId, out var roles))
            {
                return roles.ContainsKey(roleId);
            }
            return false;
        }

        public bool TryGetRole(ulong guildId, ulong roleId, out RankRole role)
        {
            if (RankRolesByGuildAndRole.TryGetValue(guildId, out var roles))
            {
                return roles.TryGetValue(roleId, out role);
            }
            role = null;
            return false;
        }

        public bool TryGetGuildRankRoles(ulong guildId, out List<RankRole> roles)
        {
            if(RankRolesByGuildAndRole.TryGetValue(guildId, out var guildRoles))
            {
                roles = guildRoles.Values.ToList();
                return true;
            }
            roles = new List<RankRole>();
            return false;
        }

        public async Task AddRole(ulong guildId, RankRole role)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(AddRole));

            if (!BotKnowsRole(guildId, role.RoleDiscordId))
            {
                await InsertRankRole(guildId, role);
            }

            transaction.Finish();
        }

        public async Task RemoveRole(ulong guildId, ulong roleId)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RemoveRole));

            if (TryGetRole(guildId, roleId, out var role))
            {
                await DeleteRankRole(guildId, role);
            }

            transaction.Finish();
        }

        private async Task InsertRankRole(ulong guildId, RankRole role)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(InsertRankRole));

            Logger.LogInformation("Writing a new Rank Role {RoleName} for Guild {GuildId} to the database", role.RoleName, guildId);
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
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
                Logger.LogError("Writing Rank Role {RoleName} for Guild {GuildId} to the database inserted no entities - The internal cache was not changed", role.RoleName, guildId);
            }

            transaction.Finish();
        }

        private async Task DeleteRankRole(ulong guildId, RankRole role)
        {
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(DeleteRankRole));

            Logger.LogInformation("Deleting Rank Role {RoleName} for Guild {GuildId} from the database", role.RoleName, guildId);

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.RankRoles.Remove(role);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveRoleFromInternalCache(guildId, role.RoleDiscordId);
            }
            else
            {
                Logger.LogWarning("Deleting Rank Role {RoleName} for Guild {GuildId} from the database deleted no entities. The internal cache was not changed", role.RoleName, guildId);
            }

            transaction.Finish();
        }
    }
}