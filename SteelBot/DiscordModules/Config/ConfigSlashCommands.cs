using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Sentry;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config;

[SlashCommandGroup("Config", "Bot configuration commands")]
[SlashRequireGuild]
public class ConfigSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ConfigDataHelper _configDataHelper;
    private readonly ILogger<ConfigSlashCommands> _logger;
    private readonly IHub _sentry;

    /// <inheritdoc />
    public ConfigSlashCommands(ConfigDataHelper configDataHelper, ILogger<ConfigSlashCommands> logger, IHub sentry) : base(logger)
    {
        _configDataHelper = configDataHelper;
        _logger = logger;
        _sentry = sentry;
    }

    [SlashCommand("Environment", "Gets the environment the bot is currently running in")]
    [SlashRequireOwner]
    [SlashCooldown(1, 300, SlashCooldownBucketType.Channel)]
    public Task Environment(InteractionContext context) =>
        context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Primary($"I'm currently running in my **{_configDataHelper.GetEnvironment()}** environment!")));
    
    [SlashCommand("Version", "Displays the current version of the bot")]
    [SlashRequireOwner]
    [SlashCooldown(2, 300, SlashCooldownBucketType.Channel)]
    public Task Version(InteractionContext context) =>
        context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Primary(_configDataHelper.GetVersion(), "Version")));

    [SlashCommand("ToggleDadJoke", "Toggles the Dad Joke Detector on/off for this server")]
    [SlashRequireUserPermissions(Permissions.Administrator)]
    [SlashCooldown(2, 300, SlashCooldownBucketType.Guild)]
    public async Task ToggleDadJoke(InteractionContext context)
    {
        var transaction = _sentry.StartNewConfiguredTransaction(nameof(ConfigSlashCommands), nameof(ToggleDadJoke), context.User, context.Guild);
        bool newSetting = await _configDataHelper.ToggleDadJoke(context.Guild.Id);
        await context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"Dad Joke Detector Toggled **{(newSetting ? "On" : "Off")}**")));
        transaction.Finish();
    }

    [SlashCommand("SetLevelChannel", "Set the channel to notify users of level-ups")]
    [SlashRequireUserPermissions(Permissions.Administrator)]
    [SlashCooldown(1, 300, SlashCooldownBucketType.Guild)]
    public async Task SetLevelChannel(InteractionContext context, [Option("Channel", "Channel to send level-up notifications to")] DiscordChannel channel)
    {
        var transaction = _sentry.StartNewConfiguredTransaction(nameof(ConfigSlashCommands), nameof(SetLevelChannel), context.User, context.Guild);
        if (channel == null || channel.Type != ChannelType.Text || !context.Guild.Channels.ContainsKey(channel.Id))
        {
            _logger.LogWarning("Invalid channel entered for setting the levelling channel");
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Error("That channel is not valid.")));
        }

        await _configDataHelper.SetLevellingChannel(context.Guild.Id, channel.Id);
        await context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"Levelling channel set to {channel.Mention}")));
        transaction.Finish();
    }
}