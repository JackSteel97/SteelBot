using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteelBot.Database.Models;
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

        [GroupCommand]
        [Description("View my portfolio.")]
        [Cooldown(2, 60, CooldownBucketType.User)]
        public async Task ViewMyPortfolio(CommandContext context)
        {
            if (!DataHelpers.Portfolios.UserHasPortfolio(context.User.Id))
            {
                await context.RespondAsync(embed: EmbedGenerator.Warning("You don't have a portfolio yet."));
                return;
            }

            if (DataHelpers.Portfolios.TryGetPortfolio(context.User.Id, out StockPortfolio portfolio))
            {
                if (portfolio.OwnedStock.Count <= 0)
                {
                    await context.RespondAsync(embed: EmbedGenerator.Info("Your porfolio is empty."));
                    return;
                }

                var ownedStocks = portfolio.OwnedStock.OrderBy(os => os.Symbol).ToList();
                // Get all the prices we can.
                var quotesBySymbol = DataHelpers.Portfolios.GetQuotesFromCache(ownedStocks);
                // Generate embed.
                var initialEmbed = DataHelpers.Portfolios.GetPortfolioStocksDisplay(context.Member.Username, ownedStocks, quotesBySymbol);

                var originalMessage = await context.RespondAsync(initialEmbed.Build());

                _ = DataHelpers.Portfolios.RunUpdateValuesTask(originalMessage, context.Member.Username, ownedStocks, quotesBySymbol, context.User.Id);
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
            var stockResult = await StockPriceService.GetStock(stockSymbol);
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

            _ = DataHelpers.Portfolios.TakePortfolioSnapshot(context.User.Id);
        }
    }
}