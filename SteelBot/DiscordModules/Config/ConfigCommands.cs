using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Config
{
    [Description("Bot configuration commands.")]
    [RequireGuild]
    public class ConfigCommands : BaseCommandModule
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
        public async Task Environment(CommandContext context)
        {
            await context.RespondAsync(embed: EmbedGenerator.Primary($"I'm currently running in my **{DataHelpers.Config.GetEnvironment()}** environment!"));
        }

        [Command("Version")]
        [Aliases("v")]
        [Description("Displays the current version of the bot.")]
        public async Task Version(CommandContext context)
        {
            await context.RespondAsync(embed: EmbedGenerator.Primary(DataHelpers.Config.GetVersion(), "Version"));
        }

        [Command("SetPrefix")]
        [Description("Sets the bot command prefix for this server.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetPrefix(CommandContext context, string newPrefix)
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

        [Command("SetSelfRole")]
        [Aliases("CreateSelfRole", "ssr")]
        [Description("Sets the given role as a self role that users can join themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task SetSelfRole(CommandContext context, string roleName, string description = "")
        {
            if (roleName.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The role name must be 255 characters or less."));
                return;
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid role name provided."));
                return;
            }

            if (!context.Guild.Roles.Values.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("You must create the role in the server first."));
                return;
            }

            await DataHelpers.Config.CreateSelfRole(context.Guild.Id, roleName, description);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Self Role **{roleName}** created!"));
        }

        [Command("RemoveSelfRole")]
        [Aliases("DeleteSelfRole", "rsr")]
        [Description("Removes the given role from the list of self roles, users will no longer be able to join the role themselves.")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task RemoveSelfRole(CommandContext context, string roleName)
        {
            await DataHelpers.Config.DeleteSelfRole(context.Guild.Id, roleName);
            await context.RespondAsync(embed: EmbedGenerator.Success($"Self Role **{roleName}** deleted!"));
        }

        [Command("SetLevelChannel")]
        [Description("Set the channel to use to notify users of level-ups")]
        [RequireUserPermissions(Permissions.Administrator)]
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