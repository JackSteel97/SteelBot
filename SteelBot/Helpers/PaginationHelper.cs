using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using SteelBot.Helpers.Interactivity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
    public static class PaginationHelper
    {
        public static List<Page> GenerateEmbedPages<TItem>(DiscordEmbedBuilder baseEmbed, IEnumerable<TItem> items, int itemsPerPage, Func<StringBuilder, TItem, int, StringBuilder> itemFormatter)
        {
            int numberOfItems = items.Count();
            int lastIndex = numberOfItems - 1;
            int requiredPages = (int)Math.Ceiling((double)numberOfItems / itemsPerPage);
            int pageTickOverIndexRemainder = itemsPerPage - 1;

            StringBuilder listBuilder = new StringBuilder();
            List<Page> pages = new List<Page>(requiredPages);
            int index = 0;
            foreach (TItem item in items)
            {
                listBuilder = itemFormatter(listBuilder, item, index);

                if (index != lastIndex)
                {
                    listBuilder.AppendLine();
                }

                if ((index % itemsPerPage == pageTickOverIndexRemainder && index > 0) || index == lastIndex)
                {
                    // Create a new page.
                    int currentPage = (index / itemsPerPage) + 1;
                    DiscordEmbedBuilder embedPage = baseEmbed.WithDescription(listBuilder.ToString())
                        .WithFooter($"Page {currentPage}/{requiredPages}");

                    pages.Add(new Page(embed: embedPage));
                    listBuilder.Clear();
                }
                index++;
            }

            return pages;
        }

        public static List<PageWithSelectionButtons> GenerateEmbedPages<TItem>(DiscordEmbedBuilder baseEmbed, IEnumerable<TItem> items, int itemsPerPage, Func<StringBuilder, TItem, StringBuilder> itemAppender, Func<TItem, DiscordComponent> componentGetter)
        {
            var chunkedItems = items.Chunk(itemsPerPage).ToList();

            var pages = new List<PageWithSelectionButtons>();
            int currentPage = 1;
            int totalPages = chunkedItems.Count;
            foreach (var chunk in chunkedItems)
            {
                var listBuilder = new StringBuilder();
                var components = new List<DiscordComponent>();
                int itemIndex = 0;
                int lastIndex = chunk.Length - 1;
                foreach (var item in chunk)
                {
                    listBuilder = itemAppender(listBuilder, item);
                    if (itemIndex != lastIndex)
                    {
                        listBuilder.AppendLine();
                    }
                    components.Add(componentGetter(item));
                    ++itemIndex;
                }

                var embedPage = baseEmbed.WithDescription(listBuilder.ToString())
                    .WithFooter($"Page {currentPage}/{totalPages}");
                var page = new PageWithSelectionButtons(embed: embedPage, options: components);
                pages.Add(page);
                ++currentPage;
            }

            return pages;
        }
    }
}