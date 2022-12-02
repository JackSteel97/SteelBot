using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Helpers.Constants;
using SteelBot.Helpers.Extensions;
using SteelBot.Helpers.Interactivity.Models;
using SteelBot.Responders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelBot.Helpers;

public static class InteractivityHelper
{
    /// <summary>
    /// Add components, handling starting new rows.
    /// Cuts off any excess components.
    /// </summary>
    /// <param name="message">The message builder.</param>
    /// <param name="components">The flat collection of components to add.</param>
    /// <returns>The message builder with components added.</returns>
    public static DiscordMessageBuilder AddComponents(DiscordMessageBuilder message, IEnumerable<DiscordComponent> components)
    {
        const int maxColumns = 5;
        const int maxRows = 5;

        int currentColumnCount = 0;
        int currentRowCount = message.Components?.Count ?? 0;

        if (currentRowCount == maxRows)
        {
            // Message already at max rows.
            return message;
        }

        var currentRowComponents = new List<DiscordComponent>(maxColumns);

        foreach (var component in components)
        {
            ++currentColumnCount;

            currentRowComponents.Add(component);
            if (currentColumnCount == maxColumns)
            {
                // Start a new row.
                message.AddComponents(currentRowComponents);
                currentRowComponents.Clear();
                currentColumnCount = 0;
                ++currentRowCount;
                if (currentRowCount == maxRows)
                {
                    // Can't fit any more rows.
                    return message;
                }
            }
        }

        if (currentRowComponents.Count > 0)
        {
            // Add remaining components.
            message.AddComponents(currentRowComponents);
        }

        return message;
    }

    /// <summary>
    /// Send a simple confirm/cancel confirmation message to the current channel.
    /// </summary>
    /// <param name="context">Current command context.</param>
    /// <param name="actionDescription">A description for the confirmation action.</param>
    /// <returns><see langword="true"/> if the user confirms the action, otherwise <see langword="false"/></returns>
    public static async Task<bool> GetConfirmation(CommandContext context, string actionDescription)
    {
        var confirmMessageBuilder = new DiscordMessageBuilder()
            .WithContent($"Attention {context.Member.Mention}!")
            .WithEmbed(EmbedGenerator.Warning($"This action ({actionDescription}) **cannot** be undone, please confirm you want to continue."))
            .AddComponents(new DiscordComponent[] {
                Interactions.Confirmation.Confirm,
                Interactions.Confirmation.Cancel
            });

        var message = await context.Channel.SendMessageAsync(confirmMessageBuilder);

        var result = await message.WaitForButtonAsync(context.Member);

        confirmMessageBuilder.ClearComponents();

        if (!result.TimedOut)
        {
            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(confirmMessageBuilder));
            return result.Result.Id == InteractionIds.Confirmation.Confirm;
        }
        else
        {
            await message.DeleteAsync();
            return false;
        }
    }

    public static async Task<bool> GetConfirmation(IResponder responder, DiscordMember member, string actionDescription)
    {
        var confirmMessageBuilder = new DiscordMessageBuilder()
            .WithContent($"Attention {member.Mention}!")
            .WithEmbed(EmbedGenerator.Warning($"This action ({actionDescription}) **cannot** be undone, please confirm you want to continue.")
            ).AddComponents((new[] { Interactions.Confirmation.Confirm, Interactions.Confirmation.Cancel }));

        var message = await responder.RespondAsync(confirmMessageBuilder);
        var result = await message.WaitForButtonAsync(member);
        responder.SetInteraction(result.Result?.Interaction);
        confirmMessageBuilder.ClearComponents();

        if (!result.TimedOut)
        {
            await message.ModifyAsync(confirmMessageBuilder);
            return result.Result.Id == InteractionIds.Confirmation.Confirm;
        }

        await message.DeleteAsync();
        return false;
    }

    private static DiscordMessageBuilder BuildPaginatedMessageWithComponents(DiscordComponent[] paginationComponents, List<PageWithSelectionButtons> pages)
    {
        var currentPage = pages[0];
        var messageBuilder = new DiscordMessageBuilder()
            .WithContent(currentPage.Content)
            .WithEmbed(currentPage.Embed)
            .AddComponents(paginationComponents);
        if (currentPage.Options.Count > 0)
        {
            AddComponents(messageBuilder, currentPage.Options);
        }

        return messageBuilder;
    }
    
    public static async Task<(string selectedId, DiscordInteraction interaction)> SendPaginatedMessageWithComponentsAsync(IResponder responder, DiscordUser user, List<PageWithSelectionButtons> pages)
    {
        var paginationComponents = new DiscordComponent[]
        {
            Interactions.Pagination.PrevPage.Disable(pages.Count <= 1),
            Interactions.Pagination.Exit,
            Interactions.Pagination.NextPage.Disable(pages.Count <= 1),
        };
        
        var messageBuilder = BuildPaginatedMessageWithComponents(paginationComponents, pages);

       var message = await responder.RespondAsync(messageBuilder);
        
        return await HandlePaginatedMessageWithComponentsResponses(user, pages, message, messageBuilder, paginationComponents);
    }

    private static async Task<(string selectedId, DiscordInteraction interaction)> HandlePaginatedMessageWithComponentsResponses(DiscordUser user,
        List<PageWithSelectionButtons> pages,
        DiscordMessage message,
        DiscordMessageBuilder messageBuilder,
        DiscordComponent[] paginationComponents)
    {
        int currentPageIndex = 0;
        while (true)
        {
            var result = await message.WaitForButtonAsync(user);

            if (!result.TimedOut && result.Result.Id != InteractionIds.Pagination.Exit)
            {
                switch (result.Result.Id)
                {
                    case InteractionIds.Pagination.PrevPage:
                        --currentPageIndex;
                        break;
                    case InteractionIds.Pagination.NextPage:
                        ++currentPageIndex;
                        break;
                    default:
                        messageBuilder.ClearComponents();
                        await message.ModifyAsync(messageBuilder);
                        return (result.Result.Id, result.Result.Interaction);
                }

                currentPageIndex = MathsHelper.Modulo(currentPageIndex, pages.Count);
                var currentPage = pages[currentPageIndex];

                await UpdateMessageToPage(messageBuilder, result.Result.Interaction, currentPage, paginationComponents);
            }
            else
            {
                messageBuilder.ClearComponents();
                await message.ModifyAsync(messageBuilder);

                return (null, null);
            }
        }
    }

    private static async Task UpdateMessageToPage(DiscordMessageBuilder messageBuilder, DiscordInteraction messageInteraction, PageWithSelectionButtons page, DiscordComponent[] paginationComponents)
    {
        messageBuilder.ClearComponents();

        messageBuilder.WithContent(page.Content).WithEmbed(page.Embed);
        messageBuilder.AddComponents(paginationComponents);

        if (page.Options.Count > 0)
        {
            AddComponents(messageBuilder, page.Options);
        }

        await messageInteraction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(messageBuilder));
    }
}