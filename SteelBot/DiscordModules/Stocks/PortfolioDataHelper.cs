using AlphaVantage.Net.Stocks;
using DSharpPlus.Entities;
using SteelBot.Database.Models;
using SteelBot.DataProviders;
using SteelBot.Helpers;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stocks
{
    public class PortfolioDataHelper
    {
        private readonly DataCache Cache;
        private readonly StockPriceService StockPriceService;

        public PortfolioDataHelper(DataCache cache, StockPriceService stockPriceService)
        {
            Cache = cache;
            StockPriceService = stockPriceService;
        }

        public Dictionary<string, GlobalQuote> GetQuotesFromCache(List<OwnedStock> stocks)
        {
            Dictionary<string, GlobalQuote> quotesBySymbol = new Dictionary<string, GlobalQuote>();
            foreach (var stock in stocks)
            {
                var quote = StockPriceService.GetStockFromInternalCache(stock.Symbol);
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
            foreach (var stock in stocks)
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
            foreach (var quote in quotesBySymbol)
            {
                if (quote.Value == null)
                {
                    // Need to get from API.
                    for (int i = 0; i < 2; i++)
                    {
                        // Retry up to 2 times to get around API blips.
                        var newQuote = await StockPriceService.GetStock(quote.Key);
                        if (newQuote != null)
                        {
                            quotesBySymbol[quote.Key] = newQuote;
                            break;
                        }
                    }

                    // Generate new embed.
                    var newEmbed = GetPortfolioStocksDisplay(username, stocks, quotesBySymbol, lastSnapshotValue, exchangeRateFromDollars, out _);

                    await originalMessage.ModifyAsync(newEmbed.Build());
                }
            }

            _ = TakePortfolioSnapshot(userId);
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
                var snapshot = await portfolio.GenerateSnapshot(StockPriceService);
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
                    var newStock = ownedStock.Clone();
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

            var newStock = stock.Clone();
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
    }
}