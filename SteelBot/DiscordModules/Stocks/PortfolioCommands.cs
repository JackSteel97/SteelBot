using AlphaVantage.Net.Forex;
using AlphaVantage.Net.Stocks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stocks
{
    [Group("Portfolio")]
    [Aliases("pf")]
    [Description("Commands to view and manage your stock portfolio tracker")]
    public class PortfolioCommands : TypingCommandModule
    {
        private readonly DataHelpers DataHelpers;
        private readonly StockPriceService StockPriceService;

        public PortfolioCommands(DataHelpers dataHelpers, StockPriceService stockPriceService)
        {
            DataHelpers = dataHelpers;
            StockPriceService = stockPriceService;
        }

        [Command("leaderboard")]
        [Aliases("l")]
        [Description("View the leaderboard for portolfio values in this server.")]
        [Cooldown(1, 60, CooldownBucketType.Channel)]
        public Task ViewLeaderboard(CommandContext context)
        {
            // Get all user portfolios in this server.
            List<StockPortfolio> allPortfolios = DataHelpers.Portfolios.GetPortfoliosInGuild(context.Guild.Id);

            _ = DataHelpers.Portfolios.SendPortfoliosLeaderboard(context, allPortfolios);
            return Task.CompletedTask;
        }

        [Command("history")]
        [Aliases("h", "value")]
        [Description("View a chart showing the value of your portfolio over time.")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task ViewPortfolioHistory(CommandContext context)
        {
            if (!DataHelpers.Portfolios.TryGetPortfolio(context.User.Id, out StockPortfolio portfolio))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You don't have a portfolio yet."));
                return;
            }

            if (portfolio.OwnedStock.Count <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Info("Your porfolio is empty."));
                return;
            }

            _ = Task.Run(async () =>
            {
                string historyFileName = $"portfolio_history_{portfolio.RowId}.png";

                DiscordMessageBuilder message = new DiscordMessageBuilder().WithReply(context.Message.Id, true);

                ScottPlot.Plot historyChart = portfolio.GenerateValueHistoryChart();
                historyChart.SaveFig(historyFileName);
                using (FileStream imageStream = File.OpenRead(historyFileName))
                {
                    DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                        .WithImageUrl($"attachment://{historyFileName}")
                        .WithTitle($"{context.Member.Username} Portfolio Value History")
                        .WithColor(EmbedGenerator.InfoColour);
                    message = message.WithFile(historyFileName, imageStream).WithEmbed(embed);

                    await context.RespondAsync(message);
                }
            });
        }

        [GroupCommand]
        [Description("View my stock portfolio.")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task ViewMyPortfolio(CommandContext context)
        {
            if (!DataHelpers.Portfolios.TryGetPortfolio(context.User.Id, out StockPortfolio portfolio))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You don't have a portfolio yet."));
                return;
            }

            if (portfolio.OwnedStock.Count <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Info("Your porfolio is empty."));
                return;
            }

            List<OwnedStock> ownedStocks = portfolio.OwnedStock.OrderBy(os => os.Symbol).ToList();
            // Get all the prices we can.
            Dictionary<string, GlobalQuote> quotesBySymbol = DataHelpers.Portfolios.GetQuotesFromCache(ownedStocks);

            // Get the last snapshot value in dollars.
            decimal lastSnapshotValue = portfolio.GetLastSnapshotValue();

            // Get the exchange rate for secondary prices.
            ForexExchangeRate exchangeRate = await StockPriceService.GetGbpUsdExchangeRate();

            // Generate embed.
            DiscordEmbedBuilder initialEmbed = DataHelpers.Portfolios.GetPortfolioStocksDisplay(context.Member.Username, ownedStocks, quotesBySymbol, lastSnapshotValue, exchangeRate.ExchangeRate, out bool stillLoading);

            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();

            messageBuilder = messageBuilder.WithEmbed(initialEmbed.Build());

            DiscordMessage originalMessage = await context.RespondAsync(messageBuilder);

            if (stillLoading)
            {
                _ = DataHelpers.Portfolios.RunUpdateValuesTask(originalMessage, context.Member.Username, ownedStocks,
                    quotesBySymbol, context.User.Id, lastSnapshotValue, exchangeRate.ExchangeRate)
                    .ContinueWith((t) => DataHelpers.Portfolios.SendBreakdownChart(context.Message.Id, portfolio, quotesBySymbol, context.Channel));
            }
            else
            {
                _ = DataHelpers.Portfolios.SendBreakdownChart(context.Message.Id, portfolio, quotesBySymbol, context.Channel);
                _ = DataHelpers.Portfolios.TakePortfolioSnapshot(context.User.Id);
            }
        }

        [Command("add")]
        [Aliases("buy")]
        [Description("Add an amount of this stock to your portfolio tracker.")]
        [Cooldown(5, 60, CooldownBucketType.User)]
        public async Task AddToPortfolio(CommandContext context, string stockSymbol, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(stockSymbol))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No stock symbol provided."));
                return;
            }
            if (amount <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("Amount must be greater than zero."));
                return;
            }
            GlobalQuote stockResult = await StockPriceService.GetStock(stockSymbol);
            if (stockResult == null)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error($"It looks like **{stockSymbol.ToUpper()}** might not be available"));
                return;
            }

            if (!await DataHelpers.Portfolios.AddStock(context.Guild.Id, context.User.Id, stockSymbol, amount))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning($"Failed to add {amount} **{stockSymbol.ToUpper()}** with an unknown error. Try again later.", "If this message persists please contact my creator."));
                return;
            }

            await context.RespondAsync(embed: EmbedGenerator.Success($"Added {amount} **{stockSymbol.ToUpper()}** to your portfolio."));

            _ = DataHelpers.Portfolios.TakePortfolioSnapshot(context.User.Id);
        }

        [Command("remove")]
        [Aliases("sell")]
        [Description("Remove an amount of this stock from your portfolio tracker.\nIf no amount is specified the maximum amount will be removed.")]
        [Cooldown(10, 30, CooldownBucketType.User)]
        public async Task RemoveFromPortfolio(CommandContext context, string stockSymbol, decimal? amount = null)
        {
            if (string.IsNullOrWhiteSpace(stockSymbol))
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("No stock symbol provided."));
                return;
            }
            if (amount.HasValue && amount <= 0)
            {
                await context.RespondAsync(embed: EmbedGenerator.Error("Amount must be greater than zero."));
                return;
            }

            (bool removed, string errorMessage) = await DataHelpers.Portfolios.RemoveStock(context.User.Id, stockSymbol, amount);
            if (!removed)
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning(errorMessage));
                return;
            }
            await context.RespondAsync(embed: EmbedGenerator.Success($"Removed {(amount.HasValue ? amount.ToString() : "All")} **{stockSymbol.ToUpper()}** from your portfolio."));
        }
    }
}