using AlphaVantage.Net.Stocks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stocks
{
    [Group("Stock")]
    [Aliases("Stocks")]
    [Description("Commands to get stock data")]
    public class StocksCommands : TypingCommandModule
    {
        private readonly StockPriceService StockPriceService;

        public StocksCommands(StockPriceService stockPriceService)
        {
            StockPriceService = stockPriceService;
        }

        [GroupCommand]
        [Description("Get the price data for a particular stock.")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task GetStockPrice(CommandContext context, string stockSymbol)
        {
            await GetStockPriceInternal(context, stockSymbol);
        }

        [Command("Detailed")]
        [Aliases("d", "verbose")]
        [Description("Get the detailed price data for a particular stock.")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task GetDetailedStockPrice(CommandContext context, string stockSymbol)
        {
            await GetStockPriceInternal(context, stockSymbol, true);
        }

        #region Helpers

        private async Task GetStockPriceInternal(CommandContext context, string stockSymbol, bool detailed = false)
        {
            if (string.IsNullOrWhiteSpace(stockSymbol))
            {
                await context.RespondAsync(EmbedGenerator.Warning("No stock symbol provided."));
                return;
            }
            string upperSymbol = stockSymbol.ToUpper();
            var initialResponse = GenerateStockDataEmbed(upperSymbol, detailed);
            var initialResponseMsg = await context.RespondAsync(initialResponse);

            var stockQuote = await StockPriceService.GetStock(stockSymbol);

            DiscordEmbed finalResponse;
            if (stockQuote == null)
            {
                finalResponse = EmbedGenerator.Error($"It looks like **{upperSymbol}** might not be available");
            }
            else
            {
                finalResponse = GenerateStockDataEmbed(upperSymbol, detailed, stockQuote);
            }

            DiscordMessageBuilder finalMessage = new DiscordMessageBuilder().WithReply(context.Message.Id, true)
                .WithEmbed(finalResponse);
            await initialResponseMsg.ModifyAsync(finalMessage);
        }

        private DiscordEmbed GenerateStockDataEmbed(string symbol, bool detailed = false, GlobalQuote quote = null)
        {
            bool quoteAvailable = quote != null;
            const string loading = EmojiConstants.CustomDiscordEmojis.LoadingSpinner;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(EmbedGenerator.InfoColour)
                .WithTitle(symbol.ToUpper())
                .AddField("Current Price", quoteAvailable ? $"${quote.Price:N2}" : loading);

            if (detailed)
            {
                builder = builder.AddField("Open", quoteAvailable ? $"${quote.OpeningPrice:N2}" : loading, true)
                    .AddField("Close", quoteAvailable ? $"${quote.PreviousClosingPrice:N2}" : loading, true)
                    .AddField("High", quoteAvailable ? $"${quote.HighestPrice:N2}" : loading, true)
                    .AddField("Low", quoteAvailable ? $"${quote.LowestPrice:N2}" : loading, true)
                    .AddField("Change", quoteAvailable ? $"{quote.ChangePercent:N2}%" : loading, true);
            }

            if (quote == null)
            {
                builder = builder.WithFooter("Data may take a moment to load due to rate limits imposed by the data provider.");
            }

            return builder.Build();
        }

        #endregion Helpers
    }
}