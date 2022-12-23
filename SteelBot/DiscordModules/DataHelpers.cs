using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Triggers;

namespace SteelBot.DiscordModules;

public class DataHelpers
{
    public StatsDataHelper Stats { get; }

    public ConfigDataHelper Config { get; }

    public RolesDataHelper Roles { get; }

    public TriggerDataHelper Triggers { get; }

    public FunDataHelper Fun { get; }

    public PetsDataHelper Pets { get; }

    public DataHelpers(StatsDataHelper statsHelper,
        ConfigDataHelper configHelper,
        RolesDataHelper rolesHelper,
        TriggerDataHelper triggersDataHelper,
        FunDataHelper fun,
        PetsDataHelper pets)
    {
        Stats = statsHelper;
        Config = configHelper;
        Roles = rolesHelper;
        Triggers = triggersDataHelper;
        Fun = fun;
        Pets = pets;
    }
}