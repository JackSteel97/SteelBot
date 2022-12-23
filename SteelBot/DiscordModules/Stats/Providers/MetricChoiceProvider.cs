using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Humanizer;
using SteelBot.DiscordModules.Stats.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats.Providers;

public class MetricChoiceProvider : IChoiceProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
    {
        var options = AllowedMetrics.Metrics;
        var choices = new List<DiscordApplicationCommandOptionChoice>(options.Count);
        foreach (string option in options) choices.Add(new DiscordApplicationCommandOptionChoice(option.Titleize(), option));

        return Task.FromResult(choices.AsEnumerable());
    }
}