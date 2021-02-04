using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
    [Description("Commands to view and manage your stock portfolio tracker"]
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
        }

        [Command("add")]
        [Aliases("buy")]
        [Description("Add an amount of this stock to your portfolio tracker.")]
        [Cooldown(5, 60, CooldownBucketType.User)]
        public async Task AddToPortfolio(CommandContext context, string stockSymbol, double amount)
        {
        }

        [Command("remove")]
        [Aliases("sell")]
        [Description("Remove an amount of this stock from your portfolio tracker.\nIf no amount is specified the maximum amount will be removed.")]
        [Cooldown(10, 30, CooldownBucketType.User)]
        public async Task RemoveFromPortfolio(CommandContext context, string stockSymbol, double? amount = null)
        {
        }
    }
}