using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config;

[SlashCommandGroup("Config", "Bot configuration commands")]
[SlashRequireGuild]
public class ConfigSlashCommands : InstrumentedApplicationCommandModule
{
    private readonly ConfigDataHelper _configDataHelper;
    private readonly ILogger<ConfigSlashCommands> _logger;

    /// <inheritdoc />
    public ConfigSlashCommands(ConfigDataHelper configDataHelper, ILogger<ConfigSlashCommands> logger, AuditLogService auditLogService) : base(logger, auditLogService)
    {
        _configDataHelper = configDataHelper;
        _logger = logger;
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
        bool newSetting = await _configDataHelper.ToggleDadJoke(context.Guild.Id);
        await context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"Dad Joke Detector Toggled **{(newSetting ? "On" : "Off")}**")));
    }

    [SlashCommand("ToggleLevelMentions", "Toggles you getting pinged for level up alerts for this server")]
    [SlashCooldown(6, 300, SlashCooldownBucketType.Channel)]
    public async Task ToggleLevelMentions(InteractionContext context)
    {
        bool newSetting = await _configDataHelper.ToggleLevelMentions(context.Guild.Id, context.User.Id);
        await context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"Level Mentions Toggled **{(!newSetting ? "On" : "Off")}**")));

    }

    [SlashCommand("SetLevelChannel", "Set the channel to notify users of level-ups")]
    [SlashRequireUserPermissions(Permissions.Administrator)]
    [SlashCooldown(1, 300, SlashCooldownBucketType.Guild)]
    public async Task SetLevelChannel(InteractionContext context, [Option("Channel", "Channel to send level-up notifications to")] DiscordChannel channel)
    {
        if (channel == null || channel.Type != ChannelType.Text || !context.Guild.Channels.ContainsKey(channel.Id))
        {
            _logger.LogWarning("Invalid channel entered for setting the levelling channel");
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Error("That channel is not valid.")));
        }

        await _configDataHelper.SetLevellingChannel(context.Guild.Id, channel.Id);
        await context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(EmbedGenerator.Success($"Levelling channel set to {channel.Mention}")));
    }
}