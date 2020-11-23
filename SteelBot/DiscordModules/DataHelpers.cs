using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.DiscordModules
{
    public class DataHelpers
    {
        public UserTrackingService UserTracking { get; }

        public StatsDataHelper Stats { get; }

        public ConfigDataHelper Config { get; }

        public RolesDataHelper Roles { get; }

        public PollsDataHelper Polls { get; }

        public RankRoleDataHelper RankRoles { get; }

        public DataHelpers(UserTrackingService userTracking,
            StatsDataHelper statsHelper,
            ConfigDataHelper configHelper,
            RolesDataHelper rolesHelper,
            PollsDataHelper pollsHelper,
            RankRoleDataHelper rankRolesHelper)
        {
            UserTracking = userTracking;
            Stats = statsHelper;
            Config = configHelper;
            Roles = rolesHelper;
            Polls = pollsHelper;
            RankRoles = rankRolesHelper;
        }
    }
}