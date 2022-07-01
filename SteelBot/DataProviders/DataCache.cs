using SteelBot.DataProviders.SubProviders;

namespace SteelBot.DataProviders
{
    public class DataCache
    {
        public GuildsProvider Guilds { get; }
        public UsersProvider Users { get; }
        public SelfRolesProvider SelfRoles { get; }
        public ExceptionProvider Exceptions { get; }
        public RankRolesProvider RankRoles { get; }
        public TriggersProvider Triggers { get; }
        public CommandStatisticProvider CommandStatistics { get; set; }
        public FunProvider Fun { get; set; }
        public PetsProvider Pets { get; set; }

        public DataCache(GuildsProvider guildsProvider,
            UsersProvider usersProvider,
            SelfRolesProvider selfRolesProvider,
            ExceptionProvider exceptionProvider,
            RankRolesProvider rankRolesProvider,
            TriggersProvider triggersProvider,
            CommandStatisticProvider commandStatisticsProvider,
            FunProvider funProvider,
            PetsProvider pets)
        {
            Guilds = guildsProvider;
            Users = usersProvider;
            SelfRoles = selfRolesProvider;
            Exceptions = exceptionProvider;
            RankRoles = rankRolesProvider;
            Triggers = triggersProvider;
            CommandStatistics = commandStatisticsProvider;
            Fun = funProvider;
            Pets = pets;
        }
    }
}