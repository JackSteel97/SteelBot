using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.RankRoles
{
    [Group("Triggers")]
    [Aliases("trigger", "t")]
    [Description("Trigger management commands")]
    [RequireGuild]
    public class TriggerCommands : BaseCommandModule
    {
        private readonly DataHelpers DataHelpers;

        public TriggerCommands(DataHelpers dataHelpers)
        {
            DataHelpers = dataHelpers;
        }

        [GroupCommand]
        [Description("List the available triggers in this channel.")]
        public async Task GetTriggers(CommandContext context)
        {
            if (DataHelpers.Triggers.GetGuildTriggers(context.Guild.Id, out Dictionary<string, Trigger> triggers))
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                    .WithTitle("The following triggers are active here.");

                bool triggersExistHere = false;
                foreach (Trigger trigger in triggers.Values)
                {
                    if (!trigger.ChannelDiscordId.HasValue || trigger.ChannelDiscordId.GetValueOrDefault() == context.Channel.Id)
                    {
                        embedBuilder.AddField(trigger.TriggerText, $"{trigger.Response}\nBy: <@{trigger.Creator.DiscordId}>");
                        triggersExistHere = true;
                    }
                }

                if (triggersExistHere)
                {
                    await context.RespondAsync(embed: embedBuilder.Build());
                    return;
                }
            }
            await context.RespondAsync(embed: EmbedGenerator.Warning("There are no triggers here."));
        }

        [Command("SetGlobal")]
        [Aliases("CreateGlobal", "sg")]
        [Description("Creates a global trigger that can be triggered from any channel in this server.")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task SetGlobalTrigger(CommandContext context, string triggerText, string response, bool mustMatchEntireMessage = false)
        {
            if (!await ValidateTriggerCreation(context, triggerText, response))
            {
                return;
            }

            Trigger trigger = new Trigger(triggerText, response, mustMatchEntireMessage);

            await DataHelpers.Triggers.CreateTrigger(context.Guild.Id, context.User.Id, trigger);

            await context.RespondAsync(embed: EmbedGenerator.Success($"**{trigger.TriggerText}** Set as a Global Trigger", "Trigger Created!"));
        }

        [Command("Set")]
        [Aliases("Create", "s")]
        [Description("Creates a trigger that can be triggered from the channel it was created in.")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task SetTrigger(CommandContext context, string triggerText, string response, bool mustMatchEntireMessage = false)
        {
            if (!await ValidateTriggerCreation(context, triggerText, response))
            {
                return;
            }

            Trigger trigger = new Trigger(triggerText, response, mustMatchEntireMessage, context.Channel.Id);

            await DataHelpers.Triggers.CreateTrigger(context.Guild.Id, context.User.Id, trigger);

            await context.RespondAsync(embed: EmbedGenerator.Success($"**{trigger.TriggerText}** Set as a Trigger", "Trigger Created!"));
        }

        [Command("Remove")]
        [Aliases("Delete", "rm")]
        [Description("Removes the given trigger.")]
        public async Task RemoveTrigger(CommandContext context, [RemainingText] string triggerText)
        {
            bool couldDelete = await DataHelpers.Triggers.DeleteTrigger(context.Guild.Id, triggerText, context.Member, context.Channel);
            if (couldDelete)
            {
                await context.RespondAsync(embed: EmbedGenerator.Success($"Trigger **{triggerText}** deleted!"));
            }
            else
            {
                DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.WarningColour).WithTitle("Could Not Delete Trigger.")
                    .WithDescription("Check the trigger exists in this channel and you have permission to delete it.");
                await context.RespondAsync(embed: embedBuilder.Build());
            }
        }

        private async Task<bool> ValidateTriggerCreation(CommandContext context, string triggerText, string response)
        {
            if (triggerText.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The trigger text must be 255 characters or less."));
                return false;
            }
            if (response.Length > 255)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("The response text must be 255 characters or less."));
                return false;
            }
            if (string.IsNullOrWhiteSpace(triggerText))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid trigger text provided."));
                return false;
            }
            if (string.IsNullOrWhiteSpace(response))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No valid response text provided."));
                return false;
            }
            if (DataHelpers.Triggers.TriggerExists(context.Guild.Id, triggerText))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("This trigger already exists, please delete the existing trigger first."));
                return false;
            }
            return true;
        }
    }
}