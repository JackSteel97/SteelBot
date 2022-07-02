using System;

namespace SteelBot.Services.Configuration;

public class AppConfigurationService
{
    public ApplicationConfig Application { get; set; }
    public DatabaseConfig Database { get; set; }
    public string Environment { get; set; }
    public string Version { get; set; }
    public string BasePath { get; set; }
    public DateTime StartUpTime { get; set; }

    // Cannot be a property due to interlocked.increment usage.
    public ulong HandledCommands = 0;
}