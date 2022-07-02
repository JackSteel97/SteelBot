using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions;

public static class DiscordComponentExtensions
{
    public static DiscordButtonComponent Disable(this DiscordButtonComponent component, bool disable = true)
    {
        return disable ? new DiscordButtonComponent(component).Disable() : component;
    }
}