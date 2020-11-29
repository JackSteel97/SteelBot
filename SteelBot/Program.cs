using DSharpPlus;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using SteelBot.Services.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SteelBot.Services;
using SteelBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DSharpPlus.CommandsNext;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DataProviders;
using System.Reflection;
using SteelBot.DiscordModules.RankRoles;
using System.IO;

namespace SteelBot
{
    public class Program
    {
        private const string Environment = "Development";

        private static IServiceProvider ConfigureServices(IServiceCollection serviceProvider)
        {
            // Config setup.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{Environment.ToLower()}.json", false, true)
                .Build();

            AppConfigurationService appConfigurationService = new AppConfigurationService();
            configuration.Bind("AppConfig", appConfigurationService);
            appConfigurationService.Environment = Environment;
            appConfigurationService.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            appConfigurationService.BasePath = Directory.GetCurrentDirectory();

            serviceProvider.AddSingleton(appConfigurationService);

            // Logging setup.
            serviceProvider.AddLogging(opt =>
            {
                opt
                .AddConsole()
                .AddConfiguration(configuration.GetSection("Logging"));
            });

            // Database DI.
            serviceProvider.AddPooledDbContextFactory<SteelBotContext>(options => options.UseSqlServer(appConfigurationService.Database.ConnectionString)
            .EnableSensitiveDataLogging(Environment.Equals("Development"))
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));

            ConfigureCustomServices(serviceProvider);
            ConfigureDataProviders(serviceProvider);
            ConfigureDataHelpers(serviceProvider);

            // Discord client setup.
            LogLevel discordLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), appConfigurationService.Application.Discord.LogLevel);

            DiscordClient client = new DiscordClient(new DiscordConfiguration()
            {
                MinimumLogLevel = discordLogLevel,
                MessageCacheSize = appConfigurationService.Application.Discord.MessageCacheSize,
                Token = appConfigurationService.Application.Discord.LoginToken,
                TokenType = TokenType.Bot
            });

            serviceProvider.AddSingleton(client);

            // Main app.
            serviceProvider.AddHostedService<BotMain>();

            return serviceProvider.BuildServiceProvider(true);
        }

        private static void ConfigureDataHelpers(IServiceCollection serviceProvider)
        {
            // Add data helpers.
            serviceProvider.AddSingleton<UserTrackingService>();
            serviceProvider.AddSingleton<StatsDataHelper>();
            serviceProvider.AddSingleton<ConfigDataHelper>();
            serviceProvider.AddSingleton<RolesDataHelper>();
            serviceProvider.AddSingleton<PollsDataHelper>();
            serviceProvider.AddSingleton<RankRoleDataHelper>();
            // Add base provider.
            serviceProvider.AddSingleton<DataHelpers>();
        }

        private static void ConfigureDataProviders(IServiceCollection serviceProvider)
        {
            // Add data providers.
            serviceProvider.AddSingleton<GuildsProvider>();
            serviceProvider.AddSingleton<UsersProvider>();
            serviceProvider.AddSingleton<SelfRolesProvider>();
            serviceProvider.AddSingleton<PollsProvider>();
            serviceProvider.AddSingleton<ExceptionProvider>();
            serviceProvider.AddSingleton<RankRolesProvider>();
            // Add base provider.
            serviceProvider.AddSingleton<DataCache>();
        }

        private static void ConfigureCustomServices(IServiceCollection serviceProvider)
        {
            // Add custom services.
            serviceProvider.AddSingleton<UserTrackingService>();
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).UseConsoleLifetime().Build().RunAsync().GetAwaiter().GetResult();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                IServiceProvider serviceProvider = ConfigureServices(services);
            });
    }
}