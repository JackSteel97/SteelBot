using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config
{
    [Group("Configuration")]
    [Description("Bot configuration commands.")]
    [Aliases("config", "c")]
    [RequireGuild]
    public class ConfigCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelpers;

        public ConfigCommands(DataHelpers dataHelpers)
        {
            DataHelpers = dataHelpers;
        }

        [Command("Environment")]
        [Aliases("env")]
        [Description("Gets the environment the bot is currently running in.")]
        [RequireOwner]
        [Cooldown(1, 300, CooldownBucketType.Channel)]
        public async Task Environment(CommandContext context)
        {
            await context.RespondAsync(embed: EmbedGenerator.Primary($"I'm currently running in my **{DataHelpers.Config.GetEnvironment()}** environment!"));
        }

        [Command("Version")]
        [Aliases("v")]
        [Description("Displays the current version of the bot.")]
        [Cooldown(10, 120, CooldownBucketType.Channel)]
        public async Task Version(CommandContext context)
        {
            await context.RespondAsync(embed: EmbedGenerator.Primary(DataHelpers.Config.GetVersion(), "Version"));
        }

        [Command("SetPrefix")]
        [Description("Sets the bot command prefix for this server.")]
        [RequireUserPermissions(Permissions.Administrator)]
        [Cooldown(1, 300, CooldownBucketType.Guild)]
        public async Task SetPrefix(CommandContext context, [RemainingText] string newPrefix)
        {
            if (newPrefix.Length > 20)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The new prefix must be 20 characters or less."));
                return;
            }
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid prefix specified."));
                return;
            }
            await DataHelpers.Config.SetPrefix(context.Guild.Id, newPrefix);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Prefix changed to **{newPrefix}**"));
        }

        [Command("SetLevelChannel")]
        [Aliases("SetAnnouncementChannel", "SetChannel", "SetLC", "SetAC")]
        [Description("Set the channel to use to notify users of level-ups")]
        [RequireUserPermissions(Permissions.Administrator)]
        [Cooldown(1, 300, CooldownBucketType.Guild)]
        public async Task SetLevelChannel(CommandContext context, DiscordChannel channel)
        {
            if (channel == null || !context.Guild.Channels.ContainsKey(channel.Id))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("That channel is not valid."));
                return;
            }

            await DataHelpers.Config.SetLevellingChannel(context.Guild.Id, channel.Id);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Levelling channel set to {channel.Mention}"));
        }
    }
}