using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Utility
{
    [Group("Utility")]
    [Description("Helpful functions.")]
    [Aliases("util", "u")]
    public class UtilityCommands : TypingCommandModule
    {
        private readonly Random Rand;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly DataCache Cache;

        public UtilityCommands(AppConfigurationService appConfigurationService, DataCache cache)
        {
            Rand = new Random();
            AppConfigurationService = appConfigurationService;
            Cache = cache;
        }

        [Command("ServerInfo")]
        [Aliases("si")]
        [Description("Displays various information about this server.")]
        [Cooldown(2, 300, CooldownBucketType.Guild)]
        public Task ServerInfo(CommandContext context)
        {
            if (!Cache.Guilds.TryGetGuild(context.Guild.Id, out var guild))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("Something went wrong and I couldn't get this Server's info, please try again later."));
            }

            var totalUsers = context.Guild.MemberCount;
            var totalRoles = context.Guild.Roles.Count;
            int textChannels = 0, voiceChannels = 0, categories = 0;
            foreach (var channel in context.Guild.Channels)
            {
                if (channel.Value.Type == ChannelType.Text)
                {
                    ++textChannels;
                }
                else if (channel.Value.Type == ChannelType.Voice)
                {
                    ++voiceChannels;
                }else if(channel.Value.Type == ChannelType.Category)
                {
                    ++categories;
                }
            }


            var created = (context.Guild.CreationTimestamp - DateTime.UtcNow).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year);
            var botAdded = (guild.BotAddedTo - DateTime.UtcNow).Humanize(2, maxUnit: Humanizer.Localisation.TimeUnit.Year);
            var levelAnnouncementChannel = guild.GetLevelAnnouncementChannel(context.Guild);
            
            var rankRolesCount = 0;
            var selfRolesCount = 0;
            var triggersCount = 0;

            if(Cache.RankRoles.TryGetGuildRankRoles(guild.DiscordId, out var rankRoles))
            {
                rankRolesCount = rankRoles.Count;
            }

            if(Cache.SelfRoles.TryGetGuildRoles(guild.DiscordId, out var selfRoles))
            {
                selfRolesCount = selfRoles.Count;
            }

            if(Cache.Triggers.TryGetGuildTriggers(guild.DiscordId, out var triggers))
            {
                triggersCount = triggers.Count;
            }

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} Info")
                .AddField("Total Users", Formatter.InlineCode(totalUsers.ToString()), true)
                .AddField("Roles", Formatter.InlineCode(totalRoles.ToString()), true)
                .AddField("Text Channels", Formatter.InlineCode(textChannels.ToString()), true)
                .AddField("Voice Channels", Formatter.InlineCode(voiceChannels.ToString()), true)
                .AddField("Categories", Formatter.InlineCode(categories.ToString()), true)
                .AddField("System Channel", context.Guild.SystemChannel?.Mention ?? "`None Set`", true)
                .AddField("AFK Timeout", Formatter.InlineCode(TimeSpan.FromSeconds(context.Guild.AfkTimeout).Humanize()), true)
                .AddField("AFK Channel", context.Guild.AfkChannel?.Mention ?? "`None Set`", true)
                .AddField("Created", Formatter.InlineCode($"{created} ago ({context.Guild.CreationTimestamp:dd-MMM-yyyy HH:mm})"), true)
                .AddField("Guild Id", Formatter.InlineCode(guild.DiscordId.ToString()), true)
                .AddField("Verification Level", Formatter.InlineCode(context.Guild.VerificationLevel.ToString()), true)
                .AddField("Owner", context.Guild.Owner.Mention, true)
                .AddField("SteelBot Added", Formatter.InlineCode($"{botAdded} ago ({guild.BotAddedTo:dd-MMM-yyyy HH:mm})"), true)
                .AddField("Announcement Channel", levelAnnouncementChannel?.Mention ?? "`None Set`", true)
                .AddField("Rank Roles", Formatter.InlineCode(rankRolesCount.ToString()), true)
                .AddField("Self Roles", Formatter.InlineCode(selfRolesCount.ToString()), true)
                .AddField("Triggers", Formatter.InlineCode(triggersCount.ToString()), true)
                .AddField("Good Bot Votes", Formatter.InlineCode(guild.GoodBotVotes.ToString()), true)
                .AddField("Bad Bot Votes", Formatter.InlineCode(guild.BadBotVotes.ToString()), true)
                .WithThumbnail(context.Guild.IconUrl);

            return context.RespondAsync(embed: builder.Build());
        }

        [Command("Status")]
        [Aliases("s", "uptime", "info")]
        [Description("Displays various information about the current status of the bot.")]
        [Cooldown(2, 300, CooldownBucketType.Channel)]
        public Task BotStatus(CommandContext context)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan uptime = (now - AppConfigurationService.StartUpTime);
            TimeSpan ping = (now - context.Message.CreationTimestamp);

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle("Bot Status")
                .AddField("Uptime", Formatter.InlineCode(uptime.Humanize(3)))
                .AddField("Processed Commands", Formatter.InlineCode(AppConfigurationService.HandledCommands.ToString()))
                .AddField("You -> Discord -> Bot Ping", Formatter.InlineCode(ping.Humanize()))
                .AddField("Bot -> Discord Ping", Formatter.InlineCode(TimeSpan.FromMilliseconds(context.Client.Ping).Humanize()))
                .AddField("Version", Formatter.InlineCode(AppConfigurationService.Version));

            return context.RespondAsync(embed: builder.Build());
        }

        [Command("Ping")]
        [Description("Pings the bot.")]
        [Cooldown(10, 60, CooldownBucketType.Channel)]
        public Task Ping(CommandContext context)
        {
            string ret = DateTime.UtcNow.Millisecond % 5 == 0 ? "POG!" : "PONG!";
            return context.RespondAsync(embed: EmbedGenerator.Primary("", ret));
        }

        [Command("Choose")]
        [Aliases("PickFrom", "Pick", "Select", "pf")]
        [Description("Select x options randomly from a given list.")]
        [Cooldown(5, 60, CooldownBucketType.Channel)]
        public Task Choose(CommandContext context, int numberToSelect, params string[] options)
        {
            // Validation.
            if (numberToSelect <= 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("X must be greater than zero."));
            }
            if (options.Length == 0)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No options were provided."));
            }
            if (numberToSelect > options.Length)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error($"There are not enough options to choose {numberToSelect} unique options.\nPlease provide more options or choose less."));
            }
            List<string> remainingOptions = options.ToList();
            List<string> selectedOptions = new List<string>(numberToSelect);
            for (int i = 0; i < numberToSelect; i++)
            {
                // Pick random option.
                int randIndex = Rand.Next(remainingOptions.Count);
                selectedOptions.Add(remainingOptions[randIndex]);
                // Remove from possible options.
                remainingOptions.RemoveAt(randIndex);
            }

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .WithEmbed(EmbedGenerator.Primary(string.Join(", ", selectedOptions), $"Chosen Option{(numberToSelect > 1 ? "s" : "")}"))
                .WithReply(context.Message.Id, true);
            return context.RespondAsync(message);
        }

        [Command("FlipCoin")]
        [Aliases("TossCoin", "fc", "flip")]
        [Description("Flips a coin.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public Task FlipCoin(CommandContext context)
        {
            int side = Rand.Next(100);
            string result = "Heads!";
            if (side < 50)
            {
                result = "Tails!";
            }

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .WithEmbed(EmbedGenerator.Primary(result))
                .WithReply(context.Message.Id, true);
            return context.RespondAsync(message);
        }

        [Command("RollDie")]
        [Aliases("Roll", "rd")]
        [Description("Rolls a die.")]
        [Cooldown(10, 60, CooldownBucketType.User)]
        public Task RollDie(CommandContext context, int sides = 6)
        {
            int rolledNumber = Rand.Next(1, sides + 1);

            DiscordMessageBuilder message = new DiscordMessageBuilder()
                .WithEmbed(EmbedGenerator.Primary($"You rolled {rolledNumber}"))
                .WithReply(context.Message.Id, true);
            return context.RespondAsync(message);
        }

        [Command("Speak")]
        [Description("Get the bot to post the given message in a channel.")]
        [RequireUserPermissions(Permissions.Administrator)]
        [Cooldown(1, 60, CooldownBucketType.Guild)]
        public Task Speak(CommandContext context, DiscordChannel channel, string title, string content, string footerContent = "")
        {
            if (!context.Guild.Channels.ContainsKey(channel.Id))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified does not exist in this server."));
            }
            if (channel.Type != ChannelType.Text)
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("The channel specified is not a text channel."));
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No valid message title was provided."));
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return context.RespondAsync(embed: EmbedGenerator.Error("No valid message content was provided."));
            }

            return channel.SendMessageAsync(embed: EmbedGenerator.Info(content, title, footerContent));
        }
    }
}