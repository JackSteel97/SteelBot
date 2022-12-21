using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class RankRolesProvider
{
    private readonly ILogger<RankRolesProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, RankRole>> _rankRolesByGuildAndRole;

    public RankRolesProvider(ILogger<RankRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;

        _rankRolesByGuildAndRole = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, RankRole>>();
        LoadRankRoleData();
    }

    private void LoadRankRoleData()
    {
        _logger.LogInformation("Loading data from database: RankRoles");
        RankRole[] allRoles;
        using (var db = _dbContextFactory.CreateDbContext())
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
        var guildRoles = _rankRolesByGuildAndRole.GetOrAdd(guildId, _ => new ConcurrentDictionary<ulong, RankRole>());
        guildRoles.TryAdd(role.RoleDiscordId, role);
    }

    private void RemoveRoleFromInternalCache(ulong guildId, ulong roleId)
    {
        if (_rankRolesByGuildAndRole.TryGetValue(guildId, out var guildRoles))
        {
            guildRoles.TryRemove(roleId, out _);
        }
    }

    public bool BotKnowsRole(ulong guildId, ulong roleId)
    {
        return _rankRolesByGuildAndRole.TryGetValue(guildId, out var roles) ? roles.ContainsKey(roleId) : false;
    }

    public bool TryGetRole(ulong guildId, ulong roleId, out RankRole role)
    {
        if (_rankRolesByGuildAndRole.TryGetValue(guildId, out var roles))
        {
            return roles.TryGetValue(roleId, out role);
        }
        role = null;
        return false;
    }

    public bool TryGetGuildRankRoles(ulong guildId, out List<RankRole> roles)
    {
        if (_rankRolesByGuildAndRole.TryGetValue(guildId, out var guildRoles))
        {
            roles = guildRoles.Values.ToList();
            return true;
        }
        roles = new List<RankRole>();
        return false;
    }

    public async Task AddRole(ulong guildId, RankRole role)
    {
        if (!BotKnowsRole(guildId, role.RoleDiscordId))
        {
            await InsertRankRole(guildId, role);
        }
    }

    public async Task RemoveRole(ulong guildId, ulong roleId)
    {
        if (TryGetRole(guildId, roleId, out var role))
        {
            await DeleteRankRole(guildId, role);
        }
    }

    private async Task InsertRankRole(ulong guildId, RankRole role)
    {
        _logger.LogInformation("Writing a new Rank Role {RoleName} for Guild {GuildId} to the database", role.RoleName, guildId);
        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
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
            _logger.LogError("Writing Rank Role {RoleName} for Guild {GuildId} to the database inserted no entities - The internal cache was not changed", role.RoleName, guildId);
        }
    }

    private async Task DeleteRankRole(ulong guildId, RankRole role)
    {
        _logger.LogInformation("Deleting Rank Role {RoleName} for Guild {GuildId} from the database", role.RoleName, guildId);

        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
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
            _logger.LogWarning("Deleting Rank Role {RoleName} for Guild {GuildId} from the database deleted no entities. The internal cache was not changed", role.RoleName, guildId);
        }
    }
}