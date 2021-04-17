using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers
{
    public class PaginationHelper
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
    }
}