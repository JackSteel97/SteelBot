using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.DataProviders;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Polls;
using SteelBot.DiscordModules.RankRoles;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Stocks;
using SteelBot.DiscordModules.Triggers;
using SteelBot.Helpers;
using SteelBot.Helpers.Levelling;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.IO;
using System.Reflection;

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
            appConfigurationService.StartUpTime = DateTime.UtcNow;

            serviceProvider.AddSingleton(appConfigurationService);

            // Set static dependency
            UserExtensions.LevelConfig = appConfigurationService.Application.Levelling;

            // Logging setup.
            serviceProvider.AddLogging(opt =>
            {
                opt
                .AddConsole()
                .AddConfiguration(configuration.GetSection("Logging"));
            });

            // Database DI.
            serviceProvider.AddPooledDbContextFactory<SteelBotContext>(options => options.UseNpgsql(appConfigurationService.Database.ConnectionString,
                                                                                                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                                                                                                        .EnableRetryOnFailure(10))
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
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
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
            serviceProvider.AddSingleton<TriggerDataHelper>();
            serviceProvider.AddSingleton<PortfolioDataHelper>();
            serviceProvider.AddSingleton<FunDataHelper>();
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
            serviceProvider.AddSingleton<TriggersProvider>();
            serviceProvider.AddSingleton<CommandStatisticProvider>();
            serviceProvider.AddSingleton<FunProvider>();
            serviceProvider.AddSingleton<StockPortfoliosProvider>();

            // Add base provider.
            serviceProvider.AddSingleton<DataCache>();
        }

        private static void ConfigureCustomServices(IServiceCollection serviceProvider)
        {
            // Add custom services.
            serviceProvider.AddSingleton<UserTrackingService>();
            serviceProvider.AddSingleton<LevelCardGenerator>();
            serviceProvider.AddSingleton<StockPriceService>();
            serviceProvider.AddSingleton<PetFactory>();
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