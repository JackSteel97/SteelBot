using DSharpPlus.Interactivity;
using Humanizer;
using SteelBot.Channels.Stats;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules.Stats.Helpers;
using SteelBot.DiscordModules.Stats.Models;
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
        const int top = 50;
        if (string.IsNullOrWhiteSpace(request.Metric))
        {
            request.Responder.Respond(StatsMessages.MissingMetric());
            return;
        }

        if (!AllowedMetrics.Metrics.Contains(request.Metric.ToLower()))
        {
            request.Responder.Respond(StatsMessages.InvalidMetric(request.Metric));
            return;
        }

        string[] metricValues;
        User[] orderedUsers;
        using (await _userLockingService.ReadLockAllUsersAsync(request.Guild.Id))
        {
            var guildUsers = _usersProvider.GetUsersInGuild(request.Guild.Id);
            if (guildUsers.Count == 0)
            {
                request.Responder.Respond(StatsMessages.NoUsersWithStatistics());
                return;
            }

            switch (request.Metric.ToLower())
            {
                case "xp":
                    orderedUsers = guildUsers.Where(u => u.TotalXp > 0).OrderByDescending(u => u.TotalXp).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"XP: `{u.TotalXp:N0}`");
                    break;

                case "level":
                    orderedUsers = guildUsers.Where(u => u.CurrentLevel > 0).OrderByDescending(u => u.CurrentLevel).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Level: `{u.CurrentLevel}`");
                    break;

                case "message count":
                    orderedUsers = guildUsers.Where(u => u.MessageCount > 0).OrderByDescending(u => u.MessageCount).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Message Count: `{u.MessageCount:N0}`");
                    break;

                case "message length":
                    orderedUsers = guildUsers.Where(u => u.GetAverageMessageLength() > 0).OrderByDescending(u => u.GetAverageMessageLength()).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Average Message Length: `{u.GetAverageMessageLength()} Characters`");
                    break;

                case "afk":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentAfk > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentAfk).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"AFK Time: `{u.TimeSpentAfk.Humanize(3)}`");
                    break;

                case "voice":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentInVoice > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentInVoice).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Voice Time: `{u.TimeSpentInVoice.Humanize(3)}`");
                    break;

                case "muted":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentMuted > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentMuted).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Muted Time: `{u.TimeSpentMuted.Humanize(3)}`");
                    break;

                case "deafened":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentDeafened > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentDeafened).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Deafened Time: `{u.TimeSpentDeafened.Humanize(3)}`");
                    break;

                case "activity":
                case "last active":
                    orderedUsers = guildUsers.Where(u => u.LastActivity != default).OrderByDescending(u => u.LastActivity).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Last Active: `{u.LastActivity.Humanize()}`");
                    break;

                case "stream":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentStreaming > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentStreaming).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Streaming Time: `{u.TimeSpentStreaming.Humanize(3)}`");
                    break;

                case "video":
                    orderedUsers = guildUsers.Where(u => u.TimeSpentOnVideo > TimeSpan.Zero).OrderByDescending(u => u.TimeSpentOnVideo).Take(top).ToArray();
                    metricValues = Array.ConvertAll(orderedUsers, u => $"Video Time: `{u.TimeSpentOnVideo.Humanize(3)}`");
                    break;

                default:
                    request.Responder.Respond(StatsMessages.InvalidMetric(request.Metric));
                    return;
            }
        }

        if (orderedUsers.Length == 0)
        {
            request.Responder.Respond(StatsMessages.NoEntriesToShow());
            return;
        }

        var embedBase = StatsMessages.LeaderboardBase(request.Guild.Name, request.Metric);

        var pages = PaginationHelper.GenerateEmbedPages(embedBase, orderedUsers, 5, (builder, user, index) => StatsMessages.AppendUserMetric(builder, user, index, metricValues));
        request.Responder.RespondPaginated(pages);
    }

    public async Task LevelsLeaderboard(StatsCommandAction request)
    {
        if (request.Action != StatsCommandActionType.ViewLevelsLeaderboard) throw new ArgumentException($"Unexpected action type sent to {nameof(LevelsLeaderboard)}");

        long top = request.Top;
        if (top <= 0)
        {
            request.Responder.Respond(StatsMessages.NoEntriesLeaderboard());
            return;
        }

        if (top > 1000)
        {
            request.Responder.Respond(StatsMessages.TopNumberTooLarge());
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
                .Take((int)top)
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

        long top = request.Top;
        if (top <= 0)
        {
            request.Responder.Respond(StatsMessages.NoEntriesLeaderboard());
            return;
        }

        if (top > 1000)
        {
            request.Responder.Respond(StatsMessages.TopNumberTooLarge());
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

            if (top > guildUsers.Count) top = guildUsers.Count;

            // Sort by XP.
            guildUsers.Sort((u1, u2) => u2.TotalXp.CompareTo(u1.TotalXp));

            var orderedByXp = guildUsers.GetRange(0, (int)top);

            var baseEmbed = StatsMessages.LeaderboardBase(request.Guild.Name);

            pages = PaginationHelper.GenerateEmbedPages(baseEmbed, orderedByXp, 2, StatsMessages.AppendUserStats);
        }

        request.Responder.RespondPaginated(pages);
    }
}