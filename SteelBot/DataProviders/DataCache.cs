using SteelBot.DataProviders.SubProviders;

namespace SteelBot.DataProviders
{
    public class DataCache
    {
        public GuildsProvider Guilds { get; }
        public UsersProvider Users { get; }
        public SelfRolesProvider SelfRoles { get; }
        public PollsProvider Polls { get; }
        public ExceptionProvider Exceptions { get; }
        public RankRolesProvider RankRoles { get; }
        public TriggersProvider Triggers { get; }
        public CommandStatisticProvider CommandStatistics { get; set; }
        public FunProvider Fun { get; set; }
        public StockPortfoliosProvider Portfolios { get; set; }

        public DataCache(GuildsProvider guildsProvider,
            UsersProvider usersProvider,
            SelfRolesProvider selfRolesProvider,
            PollsProvider pollsProvider,
            ExceptionProvider exceptionProvider,
            RankRolesProvider rankRolesProvider,
            TriggersProvider triggersProvider,
            CommandStatisticProvider commandStatisticsProvider,
            FunProvider funProvider,
            StockPortfoliosProvider portfoliosProvider)
        {
            Guilds = guildsProvider;
            Users = usersProvider;
            SelfRoles = selfRolesProvider;
            Polls = pollsProvider;
            Exceptions = exceptionProvider;
            RankRoles = rankRolesProvider;
            Triggers = triggersProvider;
            CommandStatistics = commandStatisticsProvider;
            Fun = funProvider;
            Portfolios = portfoliosProvider;
        }
    }
}