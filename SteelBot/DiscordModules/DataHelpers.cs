using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Stocks;

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

        public PortfolioDataHelper Portfolios { get; }

        public FunDataHelper Fun { get; }

        public DataHelpers(StatsDataHelper statsHelper,
            ConfigDataHelper configHelper,
            RolesDataHelper rolesHelper,
            PollsDataHelper pollsHelper,
            RankRoleDataHelper rankRolesHelper,
            TriggerDataHelper triggersDataHelper,
            PortfolioDataHelper portfolioDataHelper,
            FunDataHelper fun)
        {
            Stats = statsHelper;
            Config = configHelper;
            Roles = rolesHelper;
            Polls = pollsHelper;
            RankRoles = rankRolesHelper;
            Triggers = triggersDataHelper;
            Portfolios = portfolioDataHelper;
            Fun = fun;
        }
    }
}