using AlphaVantage.Net.Stocks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteelBot.Helpers;
using SteelBot.Helpers.Extensions;
using SteelBot.Services;
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

        [Command("Search")]
        [Aliases("find", "f", "s")]
        [Description("Look up a stock by search keywords instead of the symbol.")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task SearchStocks(CommandContext context, [RemainingText] string keywords)
        {
            if (string.IsNullOrWhiteSpace(keywords))
            {
                await context.RespondAsync(EmbedGenerator.Warning("No search keywords provided."));
                return;
            }
            DiscordEmbedBuilder initialEmbed = new DiscordEmbedBuilder()
                .WithTitle($"Searching for `{keywords}`")
                .WithDescription($"{EmojiConstants.CustomDiscordEmojis.LoadingSpinner} Please wait {EmojiConstants.CustomDiscordEmojis.LoadingSpinner}");
            DiscordMessage initialMessage = await context.RespondAsync(initialEmbed.Build());

            GlobalQuote quote = await StockPriceService.SearchStock(keywords);
            DiscordMessageBuilder finalMessage = new DiscordMessageBuilder().WithContent(context.User.Mention);
            if (quote == null)
            {
                finalMessage = finalMessage.WithEmbed(EmbedGenerator.Info("Sorry, I couldn't find any results for that search."));
            }
            else
            {
                finalMessage = finalMessage.WithEmbed(GenerateStockDataEmbed(quote.Symbol, false, quote));
            }
            await initialMessage.ModifyAsync(finalMessage);
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
            DiscordEmbed initialResponse = GenerateStockDataEmbed(upperSymbol, detailed);
            DiscordMessage initialResponseMsg = await context.RespondAsync(initialResponse);

            GlobalQuote stockQuote = await StockPriceService.GetStock(stockSymbol);

            DiscordEmbed finalResponse;
            if (stockQuote == null)
            {
                finalResponse = EmbedGenerator.Error($"It looks like **{upperSymbol}** might not be available");
            }
            else
            {
                finalResponse = GenerateStockDataEmbed(upperSymbol, detailed, stockQuote);
            }

            DiscordMessageBuilder finalMessage = new DiscordMessageBuilder().WithContent(context.User.Mention)
                .WithEmbed(finalResponse);
            await initialResponseMsg.ModifyAsync(finalMessage);
        }

        private static DiscordEmbed GenerateStockDataEmbed(string symbol, bool detailed = false, GlobalQuote quote = null)
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