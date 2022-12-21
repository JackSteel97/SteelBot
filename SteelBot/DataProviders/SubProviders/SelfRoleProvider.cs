using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class SelfRolesProvider
{
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly ILogger<SelfRolesProvider> _logger;

    private readonly ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, SelfRole>> _selfRolesByGuildAndId;

    public SelfRolesProvider(ILogger<SelfRolesProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
    {
        _logger = logger;
        _dbContextFactory = contextFactory;

        _selfRolesByGuildAndId = new ConcurrentDictionary<ulong, ConcurrentDictionary<ulong, SelfRole>>();
        LoadSelfRoleData();
    }

    private void LoadSelfRoleData()
    {
        _logger.LogInformation("Loading data from database: SelfRoles");

        SelfRole[] allRoles;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            allRoles = db.SelfRoles.AsNoTracking().Include(sr => sr.Guild).ToArray();
        }

        foreach (var role in allRoles) AddRoleToInternalCache(role.Guild.DiscordId, role);
    }

    private void AddRoleToInternalCache(ulong guildId, SelfRole role)
    {
        var guildRoles = _selfRolesByGuildAndId.GetOrAdd(guildId, _ => new ConcurrentDictionary<ulong, SelfRole>());
        guildRoles.TryAdd(role.DiscordRoleId, role);
    }

    private void RemoveRoleFromInternalCache(ulong guildId, ulong roleId)
    {
        if (_selfRolesByGuildAndId.TryGetValue(guildId, out var roles)) roles.TryRemove(roleId, out _);
    }

    public bool BotKnowsRole(ulong guildId, ulong roleId) => _selfRolesByGuildAndId.TryGetValue(guildId, out var roles) ? roles.ContainsKey(roleId) : false;

    public bool TryGetRole(ulong guildId, ulong roleId, out SelfRole role)
    {
        if (_selfRolesByGuildAndId.TryGetValue(guildId, out var roles)) return roles.TryGetValue(roleId, out role);
        role = null;
        return false;
    }

    public bool TryGetRole(ulong guildId, string roleName, out SelfRole role)
    {
        role = _selfRolesByGuildAndId.TryGetValue(guildId, out var guildRoles)
            ? guildRoles.Values.FirstOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            : null;
        return role != null;
    }

    public bool TryGetGuildRoles(ulong guildId, out List<SelfRole> roles)
    {
        if (_selfRolesByGuildAndId.TryGetValue(guildId, out var indexedRoles))
        {
            roles = indexedRoles.Values.ToList();
            return true;
        }

        roles = new List<SelfRole>();
        return false;
    }

    public async Task AddRole(ulong guildId, SelfRole role)
    {
        if (!BotKnowsRole(guildId, role.DiscordRoleId)) await InsertSelfRole(guildId, role);
    }

    public async Task RemoveRole(ulong guildId, ulong roleId)
    {
        if (TryGetRole(guildId, roleId, out var role)) await DeleteSelfRole(guildId, role);
    }

    private async Task InsertSelfRole(ulong guildId, SelfRole role)
    {
        _logger.LogInformation("Writing a new Self Role {RoleName} for Guild {GuildId} to the database", role.RoleName, guildId);

        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SelfRoles.Add(role);
            writtenCount = await db.SaveChangesAsync();
        }

        if (writtenCount > 0)
            AddRoleToInternalCache(guildId, role);
        else
            _logger.LogError("Writing Self Role {RoleName} for Guild {GuildId} to the database inserted no entities. The internal cache was not changed", role.RoleName, guildId);
    }

    private async Task DeleteSelfRole(ulong guildId, SelfRole role)
    {
        _logger.LogInformation("Deleting Self Role {RoleName} for Guild {GuildId} from the database", role.RoleName, guildId);

        int writtenCount;
        using (var db = _dbContextFactory.CreateDbContext())
        {
            db.SelfRoles.Remove(role);
            writtenCount = await db.SaveChangesAsync();
        }

        if (writtenCount > 0)
            RemoveRoleFromInternalCache(guildId, role.DiscordRoleId);
        else
            _logger.LogWarning("Deleting Self Role {RoleName} for Guild {GuildId} from the database deleted no entities. The internal cache was not changed", role.RoleName, guildId);
    }
}