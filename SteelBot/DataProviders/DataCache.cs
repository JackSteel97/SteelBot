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

        public DataCache(GuildsProvider guildsProvider,
            UsersProvider usersProvider,
            SelfRolesProvider selfRolesProvider,
            PollsProvider pollsProvider,
            ExceptionProvider exceptionProvider,
            RankRolesProvider rankRolesProvider)
        {
            Guilds = guildsProvider;
            Users = usersProvider;
            SelfRoles = selfRolesProvider;
            Polls = pollsProvider;
            Exceptions = exceptionProvider;
            RankRoles = rankRolesProvider;
        }
    }
}