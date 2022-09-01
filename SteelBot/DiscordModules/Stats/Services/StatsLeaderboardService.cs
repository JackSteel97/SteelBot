using DSharpPlus.Interactivity;
using SteelBot.Channels.Stats;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Stats.Helpers;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats.Services;

public class StatsLeaderboardService
{
    private readonly UserLockingService _userLockingService;
    private readonly UsersProvider _usersProvider;

    public StatsLeaderboardService(UserLockingService userLockingService, UsersProvider usersProvider)
    {
        _userLockingService = userLockingService;
        _usersProvider = usersProvider;
    }

    public async Task MetricLeaderboard(StatsCommandAction request)
    {
        // TODO: Implement.
    }

    public async Task LevelsLeaderboard(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewLevelsLeaderboard) throw new ArgumentException($"Unexpected action type sent to {nameof(LevelsLeaderboard)}");

        int top = request.Top;
        if (top <= 0)
        {
            request.Responder.Respond(StatsMessages.NoEntriesLeaderboard());
            return;
        }

        List<Page> pages;
        using (await _userLockingService.ReadLockAllUsersAsync(request.Guild.Id))
        {
            var guildUsers = _usersProvider.GetUsersInGuild(request.Guild.Id);
            if (guildUsers.Count == 0)
            {
                request.Responder.Respond(StatsMessages.NoUsersWithStatistics());
                return;
            }
            
            var orderedByXp = guildUsers
                .Where(u => u.TotalXp > 0)
                .OrderByDescending(u => u.TotalXp)
                .Take(top)
                .ToArray();

            if (orderedByXp.Length == 0)
            {
                request.Responder.Respond(StatsMessages.NoEntriesToShow());
                return;
            }

            var baseEmbed = StatsMessages.LeaderboardBase(request.Guild.Name);
            pages = PaginationHelper.GenerateEmbedPages(baseEmbed, orderedByXp, 5, StatsMessages.AppendLeaderboardEntry);
        }
        
        request.Responder.RespondPaginated(pages);
    }

    public async Task AllStats(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewAll) throw new ArgumentException($"Unexpected action type sent to {nameof(AllStats)}");

        int top = request.Top;
        if (top <= 0)
        {
            request.Responder.Respond(StatsMessages.NoEntriesLeaderboard());
            return;
        }

        List<Page> pages;
        using (await _userLockingService.ReadLockAllUsersAsync(request.Guild.Id))
        {
            var guildUsers = _usersProvider.GetUsersInGuild(request.Guild.Id);
            if (guildUsers.Count == 0)
            {
                request.Responder.Respond(StatsMessages.NoUsersWithStatistics());
                return;
            }

            if (top > guildUsers.Count)
            {
                top = guildUsers.Count;
            }
            
            // Sort by XP.
            guildUsers.Sort((u1, u2)=> u2.TotalXp.CompareTo(u1.TotalXp));

            var orderedByXp = guildUsers.GetRange(0, top);

            var baseEmbed = StatsMessages.LeaderboardBase(request.Guild.Name);

            pages = PaginationHelper.GenerateEmbedPages(baseEmbed, orderedByXp, 2, StatsMessages.AppendUserStats);
        }
        
        request.Responder.RespondPaginated(pages);
    }
}