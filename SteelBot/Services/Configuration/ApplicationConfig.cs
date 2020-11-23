using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.Services.Configuration
{
    public class ApplicationConfig
    {
        public DiscordConfig Discord { get; set; }
        public string DefaultCommandPrefix { get; set; }
        public string UnknownCommandResponse { get; set; }
    }
}
