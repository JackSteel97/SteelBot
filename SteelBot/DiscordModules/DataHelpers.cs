﻿using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.Services;

namespace SteelBot.DiscordModules
{
    public class DataHelpers
    {
        public StatsDataHelper Stats { get; }

        public ConfigDataHelper Config { get; }

        public RolesDataHelper Roles { get; }

        public PollsDataHelper Polls { get; }

        public RankRoleDataHelper RankRoles { get; }

        public TriggerDataHelper Triggers { get; }

        public DataHelpers(StatsDataHelper statsHelper,
            ConfigDataHelper configHelper,
            RolesDataHelper rolesHelper,
            PollsDataHelper pollsHelper,
            RankRoleDataHelper rankRolesHelper,
            TriggerDataHelper triggersDataHelper)
        {
            Stats = statsHelper;
            Config = configHelper;
            Roles = rolesHelper;
            Polls = pollsHelper;
            RankRoles = rankRolesHelper;
            Triggers = triggersDataHelper;
        }
    }
}