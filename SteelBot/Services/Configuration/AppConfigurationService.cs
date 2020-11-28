using System;
using System.Collections.Generic;
using System.Text;

namespace SteelBot.Services.Configuration
{
    public class AppConfigurationService
    {
        public ApplicationConfig Application { get; set; }
        public DatabaseConfig Database { get; set; }
        public string Environment { get; set; }
        public string Version { get; set; }
        public string BasePath { get; set; }
    }
}