using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Microsoft.Extensions.Logging;
using Sentry;
using SteelBot.Database.Models;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Triggers;

[Group("Triggers")]
[Aliases("trigger", "t")]
[Description("Trigger management commands")]
[RequireGuild]
public class TriggerCommands : TypingCommandModule
{
    private readonly DataHelpers _dataHelpers;

    public TriggerCommands(DataHelpers dataHelpers, IHub sentry, ILogger<TriggerCommands> logger, AuditLogService auditLogService)
        : base(logger, auditLogService, sentry)
    {
        _dataHelpers = dataHelpers;
    }

    [GroupCommand]
    [Description("List the available triggers in this channel.")]
    [Cooldown(2, 60, CooldownBucketType.Channel)]
    public async Task GetTriggers(CommandContext context)
    {
        if (_dataHelpers.Triggers.GetGuildTriggers(context.Guild.Id, out var triggers))
        {
            var embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle("The following triggers are active here.")
                .WithTimestamp(DateTime.UtcNow);

            var triggersAvailable = triggers.Values
                .Where(trigger => !trigger.ChannelDiscordId.HasValue || trigger.ChannelDiscordId.GetValueOrDefault() == context.Channel.Id)
                .OrderByDescending(t => t.TimesActivated);
            if (triggersAvailable.Any())
            {
                var pages = PaginationHelper.GenerateEmbedPages(embedBuilder, triggersAvailable, 5, (builder, trigger, _) =>
                {
                    return builder.AppendLine(Formatter.Bold(trigger.TriggerText))
                       .Append("Response: ").AppendLine(trigger.Response)
                       .Append("By: <@").Append(trigger.Creator.DiscordId).AppendLine(">")
                       .Append("Used: ").AppendLine("time".ToQuantity(trigger.TimesActivated));
                });

                var interactivity = context.Client.GetInteractivity();

                await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
            }
            else
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("There are no triggers here."));
            }
        }
    }

    [Command("SetGlobal")]
    [Aliases("CreateGlobal", "sg")]
    [Description("Creates a global trigger that can be triggered from any channel in this server.")]
    [RequireUserPermissions(Permissions.ManageChannels)]
    [Cooldown(5, 60, CooldownBucketType.Guild)]
    public async Task SetGlobalTrigger(CommandContext context, string triggerText, string response, bool mustMatchEntireMessage = false)
    {
        if (!await ValidateTriggerCreation(context, triggerText, response))
        {
            return;
        }

        var trigger = new Trigger(triggerText, response, mustMatchEntireMessage);

        await _dataHelpers.Triggers.CreateTrigger(context.Guild.Id, context.User.Id, trigger);

        await context.RespondAsync(embed: EmbedGenerator.Success($"**{trigger.TriggerText}** Set as a Global Trigger", "Trigger Created!"));
    }

    [Command("Set")]
    [Aliases("Create", "s")]
    [Description("Creates a trigger that can be triggered from the channel it was created in.")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    [Cooldown(5, 60, CooldownBucketType.Channel)]
    public async Task SetTrigger(CommandContext context, string triggerText, string response, bool mustMatchEntireMessage = false)
    {
        if (!await ValidateTriggerCreation(context, triggerText, response))
        {
            return;
        }

        var trigger = new Trigger(triggerText, response, mustMatchEntireMessage, context.Channel.Id);

        await _dataHelpers.Triggers.CreateTrigger(context.Guild.Id, context.User.Id, trigger);

        await context.RespondAsync(embed: EmbedGenerator.Success($"**{trigger.TriggerText}** Set as a Trigger", "Trigger Created!"));
    }

    [Command("Remove")]
    [Aliases("Delete", "rm")]
    [Description("Removes the given trigger.")]
    [Cooldown(10, 60, CooldownBucketType.Channel)]
    public async Task RemoveTrigger(CommandContext context, [RemainingText] string triggerText)
    {
        bool couldDelete = await _dataHelpers.Triggers.DeleteTrigger(context.Guild.Id, triggerText, context.Member, context.Channel);
        if (couldDelete)
        {
            await context.RespondAsync(embed: EmbedGenerator.Success($"Trigger **{triggerText}** deleted!"));
        }
        else
        {
            var embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.WarningColour).WithTitle("Could Not Delete Trigger.")
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
        if (_dataHelpers.Triggers.TriggerExists(context.Guild.Id, triggerText))
        {
            await context.RespondAsync(embed: EmbedGenerator.Error("This trigger already exists, please delete the existing trigger first."));
            return false;
        }
        return true;
    }
}