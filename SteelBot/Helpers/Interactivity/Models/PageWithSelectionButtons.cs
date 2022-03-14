using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Interactivity.Models
{
    public class PageWithSelectionButtons : Page
    {
        public ICollection<DiscordComponent> Options { get; set; }

        public PageWithSelectionButtons(string content = "", DiscordEmbedBuilder embed = null, ICollection<DiscordComponent> options = null) : base(content, embed)
        {
            Options = options;
        }
    }
}
