using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using SteelBot.Helpers.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
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
            int currentRowCount = 0;
            List<DiscordComponent> currentRowComponents = new List<DiscordComponent>(maxColumns);

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
    }
}
