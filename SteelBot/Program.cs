using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SteelBot.Channels.Message;
using SteelBot.Channels.Pets;
using SteelBot.Channels.Puzzle;
using SteelBot.Channels.RankRole;
using SteelBot.Channels.SelfRole;
using SteelBot.Channels.Stats;
using SteelBot.Channels.Voice;
using SteelBot.Database;
using SteelBot.DataProviders;
using SteelBot.DataProviders.SubProviders;
using SteelBot.DiscordModules;
using SteelBot.DiscordModules.AuditLog.Services;
using SteelBot.DiscordModules.Config;
using SteelBot.DiscordModules.Fun;
using SteelBot.DiscordModules.Pets;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Services;
using SteelBot.DiscordModules.Puzzle.Questions;
using SteelBot.DiscordModules.Puzzle.Services;
using SteelBot.DiscordModules.RankRoles.Services;
using SteelBot.DiscordModules.Roles;
using SteelBot.DiscordModules.Roles.Services;
using SteelBot.DiscordModules.Stats;
using SteelBot.DiscordModules.Stats.Services;
using SteelBot.DiscordModules.Triggers;
using SteelBot.DiscordModules.Utility;
using SteelBot.DSharpPlusOverrides;
using SteelBot.Helpers;
using SteelBot.Helpers.Levelling;
using SteelBot.RateLimiting;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SteelBot;

public static class Program
{
    private static readonly string _environment = Environment.GetEnvironmentVariable("STEELBOTENVIRONMENT") ?? "Test";

    private static IServiceProvider ConfigureServices(IServiceCollection serviceProvider)
    {
        serviceProvider.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
        // Config setup.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.{_environment.ToLower()}.json", false, true)
            .Build();

        var appConfigurationService = new AppConfigurationService();
        configuration.Bind("AppConfig", appConfigurationService);
        appConfigurationService.Environment = _environment;
        appConfigurationService.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        appConfigurationService.BasePath = Directory.GetCurrentDirectory();
        appConfigurationService.StartUpTime = DateTime.UtcNow;

        serviceProvider.AddSingleton(appConfigurationService);

        // Set static dependency
        UserExtensions.LevelConfig = appConfigurationService.Application.Levelling;

        // Logging setup.
        const int fileSizeLimitBytes = 8 * 1000 * 1000;
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.File("Logs/SteelBotLog.txt", rollingInterval: RollingInterval.Day, fileSizeLimitBytes: fileSizeLimitBytes, rollOnFileSizeLimit: true);
#if DEBUG
        loggerConfig.WriteTo.Console();
#endif
        Log.Logger = loggerConfig.CreateLogger();
        Log.Logger.Information("Logger Created");

        try
        {
            var loggerFactory = new LoggerFactory().AddSerilog();
            serviceProvider.AddLogging(opt =>
            {
                opt.ClearProviders();
                opt.AddSerilog(Log.Logger);
                opt.AddConfiguration(configuration);
            });

            // Database DI.
            serviceProvider.AddPooledDbContextFactory<SteelBotContext>(options => options.UseNpgsql(appConfigurationService.Database.ConnectionString,
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                        .EnableRetryOnFailure(10))
                .EnableSensitiveDataLogging(_environment.Equals("Development"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution));

            ConfigureCustomServices(serviceProvider);
            ConfigureDataProviders(serviceProvider);
            ConfigureDataHelpers(serviceProvider);

            // Discord client setup.
            var client = new DiscordClient(new DiscordConfiguration
            {
                LoggerFactory = loggerFactory,
                MinimumLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), appConfigurationService.Application.Discord.LogLevel),
                MessageCacheSize = appConfigurationService.Application.Discord.MessageCacheSize,
                Token = appConfigurationService.Application.Discord.LoginToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents
            });

            serviceProvider.AddSingleton(client);

            // Main app.
            serviceProvider.AddHostedService<BotMain>();

            return serviceProvider.BuildServiceProvider(true);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "A Fatal exception occurred during startup");
            throw;
        }
    }

    private static void ConfigureDataHelpers(IServiceCollection serviceProvider)
    {
        // Add data helpers.
        serviceProvider.AddSingleton<UserTrackingService>();
        serviceProvider.AddSingleton<StatsDataHelper>();
        serviceProvider.AddSingleton<ConfigDataHelper>();
        serviceProvider.AddSingleton<RolesDataHelper>();
        serviceProvider.AddSingleton<TriggerDataHelper>();
        serviceProvider.AddSingleton<FunDataHelper>();
        serviceProvider.AddSingleton<PetsDataHelper>();
        // Add base provider.
        serviceProvider.AddSingleton<DataHelpers>();
    }

    private static void ConfigureDataProviders(IServiceCollection serviceProvider)
    {
        // Add data providers.
        serviceProvider.AddSingleton<GuildsProvider>();
        serviceProvider.AddSingleton<UsersProvider>();
        serviceProvider.AddSingleton<SelfRolesProvider>();
        serviceProvider.AddSingleton<ExceptionProvider>();
        serviceProvider.AddSingleton<RankRolesProvider>();
        serviceProvider.AddSingleton<TriggersProvider>();
        serviceProvider.AddSingleton<CommandStatisticProvider>();
        serviceProvider.AddSingleton<FunProvider>();
        serviceProvider.AddSingleton<PetsProvider>();
        serviceProvider.AddSingleton<PuzzleProvider>();

        // Add base provider.
        serviceProvider.AddSingleton<DataCache>();
    }

    private static void ConfigureCustomServices(IServiceCollection serviceProvider)
    {
        // TODO: Scope services?
        // Add custom services.
        serviceProvider.AddSingleton<UserTrackingService>();
        serviceProvider.AddSingleton<LevelCardGenerator>();
        serviceProvider.AddSingleton<PetFactory>();

        serviceProvider.AddSingleton<ErrorHandlingService>();
        serviceProvider.AddSingleton<CancellationService>();

        serviceProvider.AddSingleton<VoiceStateChannel>();
        serviceProvider.AddSingleton<VoiceStateChangeHandler>();
        serviceProvider.AddSingleton<LevelMessageSender>();

        serviceProvider.AddSingleton<MessagesChannel>();
        serviceProvider.AddSingleton<IncomingMessageHandler>();

        serviceProvider.AddSingleton<SelfRoleManagementChannel>();
        serviceProvider.AddSingleton<SelfRoleCreationService>();
        serviceProvider.AddSingleton<SelfRoleMembershipService>();
        serviceProvider.AddSingleton<SelfRoleViewingService>();

        serviceProvider.AddSingleton<RankRoleManagementChannel>();
        serviceProvider.AddSingleton<RankRoleCreationService>();
        serviceProvider.AddSingleton<RankRoleDeletionService>();
        serviceProvider.AddSingleton<RankRoleViewingService>();

        serviceProvider.AddSingleton<PetCommandsChannel>();
        serviceProvider.AddSingleton<PetViewingService>();
        serviceProvider.AddSingleton<PetBonusViewingService>();
        serviceProvider.AddSingleton<PetBefriendingService>();
        serviceProvider.AddSingleton<PetSearchingService>();
        serviceProvider.AddSingleton<PetManagementService>();
        serviceProvider.AddSingleton<PetTreatingService>();
        serviceProvider.AddSingleton<PetDeathService>();

        serviceProvider.AddSingleton<UserLockingService>();
        serviceProvider.AddSingleton<ErrorHandlingAsynchronousCommandExecutor>();

        serviceProvider.AddSingleton<StatsCommandsChannel>();
        serviceProvider.AddSingleton<StatsAdminService>();
        serviceProvider.AddSingleton<StatsCardService>();
        serviceProvider.AddSingleton<StatsLeaderboardService>();

        serviceProvider.AddSingleton<PuzzleCommandsChannel>();
        serviceProvider.AddSingleton<PuzzleService>();
        serviceProvider.AddSingleton<QuestionFactory>();

        serviceProvider.AddMemoryCache();
        serviceProvider.AddSingleton<RateLimitFactory>();

        serviceProvider.AddSingleton<UtilityService>();

        serviceProvider.AddSingleton<AuditLogProvider>();
        serviceProvider.AddSingleton<AuditLogService>();
    }

    public static async Task Main(string[] args)
    {
        try
        {
            await CreateHostBuilder(args).UseConsoleLifetime().Build().RunAsync();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var serviceProvider = ConfigureServices(services);
            });
}