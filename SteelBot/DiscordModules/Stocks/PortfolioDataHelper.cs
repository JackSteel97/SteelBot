using AlphaVantage.Net.Forex;
using AlphaVantage.Net.Stocks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Services;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stocks
{
    public class PortfolioDataHelper
    {
        private readonly DataCache Cache;
        private readonly StockPriceService StockPriceService;
        private readonly AppConfigurationService AppConfigurationService;

        public PortfolioDataHelper(DataCache cache, StockPriceService stockPriceService, AppConfigurationService appConfigurationService)
        {
            Cache = cache;
            StockPriceService = stockPriceService;
            AppConfigurationService = appConfigurationService;
        }

        public Dictionary<string, GlobalQuote> GetQuotesFromCache(List<OwnedStock> stocks)
        {
            Dictionary<string, GlobalQuote> quotesBySymbol = new Dictionary<string, GlobalQuote>();
            foreach (OwnedStock stock in stocks)
            {
                GlobalQuote quote = StockPriceService.GetStockFromInternalCache(stock.Symbol);
                quotesBySymbol.Add(stock.Symbol, quote);
            }
            return quotesBySymbol;
        }

        public DiscordEmbedBuilder GetPortfolioStocksDisplay(string username, List<OwnedStock> stocks, Dictionary<string, GlobalQuote> quotesBySymbol, decimal lastSnapshotValue, decimal exchangeRateFromDollars, out bool anyStillLoading)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                           .WithTitle($"{username} Stock Portfolio.");
            const string loading = EmojiConstants.CustomDiscordEmojis.LoadingSpinner;

            anyStillLoading = false;
            decimal grandTotal = 0;
            foreach (OwnedStock stock in stocks)
            {
                quotesBySymbol.TryGetValue(stock.Symbol, out GlobalQuote quote);
                string value = loading;
                if (quote != null)
                {
                    decimal totalValue = stock.AmountOwned * quote.Price;
                    grandTotal += totalValue;
                    value = $"`${totalValue:N2}`";
                }
                else
                {
                    anyStillLoading = true;
                }
                embed.AddField($"{stock.Symbol} - `{stock.AmountOwned:G29} shares`", $"Equity {value}", true);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"**Portfolio Value:** {(anyStillLoading ? loading : $"`${grandTotal:N2}` ≈ `£{(grandTotal * exchangeRateFromDollars):N2}`")}");

            if (!anyStillLoading)
            {
                if (lastSnapshotValue != grandTotal)
                {
                    string indicator = grandTotal > lastSnapshotValue ? EmojiConstants.CustomDiscordEmojis.GreenArrowUp : EmojiConstants.CustomDiscordEmojis.RedArrowDown;

                    decimal percentageChange = MathsHelper.PercentageChange(lastSnapshotValue, grandTotal);
                    sb.Append($" - {indicator}`{percentageChange:P2}`");
                }
            }
            else
            {
                embed = embed.WithFooter("Prices may a take a while to load due to limits imposed by the data provider.");
            }

            embed = embed.WithDescription(sb.ToString());

            return embed;
        }

        public async Task RunUpdateValuesTask(DiscordMessage originalMessage, string username, List<OwnedStock> stocks, Dictionary<string, GlobalQuote> quotesBySymbol, ulong userId, decimal lastSnapshotValue, decimal exchangeRateFromDollars)
        {
            foreach (KeyValuePair<string, GlobalQuote> quote in quotesBySymbol)
            {
                if (quote.Value == null)
                {
                    // Need to get from API.
                    for (int i = 0; i < 2; i++)
                    {
                        // Retry up to 2 times to get around API blips.
                        GlobalQuote newQuote = await StockPriceService.GetStock(quote.Key);
                        if (newQuote != null)
                        {
                            quotesBySymbol[quote.Key] = newQuote;
                            break;
                        }
                    }

                    // Generate new embed.
                    DiscordEmbedBuilder newEmbed = GetPortfolioStocksDisplay(username, stocks, quotesBySymbol, lastSnapshotValue, exchangeRateFromDollars, out _);

                    DiscordMessageBuilder newMessage = new DiscordMessageBuilder();

                    newMessage = newMessage.WithEmbed(newEmbed.Build());

                    await originalMessage.ModifyAsync(newMessage);
                }
            }

            _ = TakePortfolioSnapshot(userId);
        }

        public async Task SendBreakdownChart(ulong replyToMessageId, StockPortfolio portfolio, Dictionary<string, GlobalQuote> quotesBySymbol, DiscordChannel sendToChannel)
        {
            string breakdownFileName = $"portfolio_breakdown_{portfolio.RowId}.png";

            DiscordMessageBuilder message = new DiscordMessageBuilder().WithReply(replyToMessageId, true);

            ScottPlot.Plot breakdownChart = portfolio.GeneratePortfolioBreakdownChart(quotesBySymbol);
            breakdownChart.SaveFig(breakdownFileName);
            using (FileStream imageStream = File.OpenRead(breakdownFileName))
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                    .WithImageUrl($"attachment://{breakdownFileName}")
                    .WithTitle("Portfolio Breakdown")
                    .WithColor(EmbedGenerator.InfoColour);
                message = message.WithFile(breakdownFileName, imageStream).WithEmbed(embed);

                await sendToChannel.SendMessageAsync(message);
            }
        }

        public bool UserHasPortfolio(ulong userId)
        {
            return Cache.Portfolios.UserHasPortfolio(userId);
        }

        public bool TryGetPortfolio(ulong userId, out StockPortfolio portfolio)
        {
            return Cache.Portfolios.TryGetPortfolio(userId, out portfolio);
        }

        public async Task TakePortfolioSnapshot(ulong userId)
        {
            if (TryGetPortfolio(userId, out StockPortfolio portfolio))
            {
                StockPortfolioSnapshot snapshot = await portfolio.GenerateSnapshot(StockPriceService);
                await Cache.Portfolios.AddPortfolioSnapshot(userId, snapshot);
            }
        }

        public async Task<bool> AddStock(ulong guildId, ulong userId, string stockSymbol, decimal amount)
        {
            bool added = false;
            // Create a portfolio if they don't have one.
            if (!Cache.Portfolios.UserHasPortfolio(userId))
            {
                await CreatePortfolio(guildId, userId);
            }

            if (Cache.Portfolios.TryGetPortfolio(userId, out StockPortfolio portfolio))
            {
                // Check if they already have some of this stock.
                if (Cache.Portfolios.UserOwnsStock(userId, stockSymbol))
                {
                    // Get from portfolio.
                    portfolio.OwnedStockBySymbol.TryGetValue(stockSymbol.ToUpper(), out OwnedStock ownedStock);
                    OwnedStock newStock = ownedStock.Clone();
                    newStock.AmountOwned += amount;
                    // Add to the amount.
                    await Cache.Portfolios.UpdateOwnedStock(userId, newStock);
                }
                else
                {
                    // Add new stock.
                    await Cache.Portfolios.AddNewOwnedStock(userId, new OwnedStock(portfolio, stockSymbol, amount, DateTime.UtcNow));
                };
                added = true;
            }
            return added;
        }

        public async Task<(bool removed, string errorMessage)> RemoveStock(ulong userId, string stockSymbol, decimal? amount = null)
        {
            if (!Cache.Portfolios.UserHasPortfolio(userId))
            {
                return (false, "You don't have a stock portfolio yet.");
            }
            if (!Cache.Portfolios.TryGetOwnedStock(userId, stockSymbol, out OwnedStock stock))
            {
                return (false, "You don't own any of this stock yet.");
            }

            decimal amountToRemove;
            if (amount.HasValue)
            {
                if (amount.Value > stock.AmountOwned)
                {
                    return (false, $"You don't own that much **{stock.Symbol}** (MAX: {stock.AmountOwned})");
                }
                amountToRemove = amount.Value;
            }
            else
            {
                // Remove it all.
                amountToRemove = stock.AmountOwned;
            }

            OwnedStock newStock = stock.Clone();
            newStock.AmountOwned -= amountToRemove;
            // Remove the stock amount.
            await Cache.Portfolios.UpdateOwnedStock(userId, newStock);
            return (true, null);
        }

        public async Task CreatePortfolio(ulong guildId, ulong userId)
        {
            if (Cache.Users.TryGetUser(guildId, userId, out User user))
            {
                await Cache.Portfolios.CreatePortfolio(user);
            }
        }

        public List<StockPortfolio> GetPortfoliosInGuild(ulong guildId)
        {
            List<ulong> allUsersIds = Cache.Users.GetUsersInGuild(guildId).ConvertAll(u => u.DiscordId);

            return Cache.Portfolios.TryGetPortfolios(allUsersIds);
        }

        public bool AnyPortfolioSnapshotsOutOfDate(List<StockPortfolio> portfolios)
        {
            foreach (StockPortfolio portfolio in portfolios)
            {
                if (portfolio.Snapshots.Count == 0)
                {
                    return true;
                }
                // Get the latest snapshot.
                StockPortfolioSnapshot snapshot = portfolio.Snapshots[^1];
                TimeSpan timeSinceSnapshot = (DateTime.UtcNow - snapshot.SnapshotTaken);

                // Check if it is out of date.
                if (timeSinceSnapshot.TotalMinutes >= AppConfigurationService.Application.StockCacheTimeMinutes)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task GenerateSnapshots(List<StockPortfolio> portfolios)
        {
            foreach (StockPortfolio portfolio in portfolios)
            {
                StockPortfolioSnapshot snapshot = await portfolio.GenerateSnapshot(StockPriceService);
                await Cache.Portfolios.AddPortfolioSnapshot(portfolio.Owner.DiscordId, snapshot);
            }
        }

        public async Task SendPortfoliosLeaderboard(CommandContext context, List<StockPortfolio> portfolios)
        {
            // Check if any of them need updating.
            DiscordMessage loadingMessage = null;
            if (AnyPortfolioSnapshotsOutOfDate(portfolios))
            {
                loadingMessage = await context.RespondAsync(EmbedGenerator.Info($"Got it, I'll ping you when it's ready!\n\n{EmojiConstants.CustomDiscordEmojis.LoadingSpinner} Crunching the numbers...", "Processing"));
                await GenerateSnapshots(portfolios);
            }
            ForexExchangeRate exchangeRate = await StockPriceService.GetGbpUsdExchangeRate();

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder().WithColor(EmbedGenerator.InfoColour)
                .WithTitle($"{context.Guild.Name} Stock Portfolio Value Leaderboard")
                .WithTimestamp(DateTime.UtcNow);

            IOrderedEnumerable<StockPortfolio> orderedPortfolios = portfolios.OrderByDescending(pf => pf.Snapshots[^1].TotalValueDollars);
            List<Page> pages = PaginationHelper.GenerateEmbedPages(embedBuilder, orderedPortfolios, 5, (builder, portfolio, index) =>
            {
                decimal valueInDollars = portfolio.Snapshots[^1].TotalValueDollars;
                return builder
                    .AppendLine($"**{(index + 1).Ordinalize()}** - <@{portfolio.Owner.DiscordId}>")
                    .Append($"`${valueInDollars:N2}`").AppendLine($" ≈ `£{(valueInDollars * exchangeRate.ExchangeRate):N2}`");
            });

            InteractivityExtension interactivity = context.Client.GetInteractivity();

            // Delete the loading message if it was sent.
            if (loadingMessage != null)
            {
                _ = loadingMessage.DeleteAsync("Finished processing");
            }

            await context.RespondAsync(context.User.Mention);
            await interactivity.SendPaginatedMessageAsync(context.Channel, context.User, pages);
        }
    }
}