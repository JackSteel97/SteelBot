using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class StockPortfoliosProvider
    {
        private readonly ILogger<StockPortfoliosProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private Dictionary<ulong, StockPortfolio> PortfoliosByUserDiscordId;

        public StockPortfoliosProvider(ILogger<StockPortfoliosProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            LoadPortfoliosData();
        }

        public void LoadPortfoliosData()
        {
            Logger.LogInformation("Loading data from database: Portfolios");

            using (var db = DbContextFactory.CreateDbContext())
            {
                var allPortfolios = db.StockPortfolios.AsNoTracking()
                    .Include(pf => pf.Owner)
                    .Include(pf => pf.OwnedStock);
                foreach (StockPortfolio portfolio in allPortfolios)
                {
                    portfolio.BuildOwnedStockCache();
                    PortfoliosByUserDiscordId.Add(portfolio.Owner.DiscordId, portfolio);
                }
            }
        }

        public bool UserHasPortfolio(ulong userId)
        {
            return PortfoliosByUserDiscordId.ContainsKey(userId);
        }

        public bool UserOwnsStock(ulong userId, string stockSymbol)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                return portfolio.OwnedStockBySymbol.ContainsKey(stockSymbol.ToUpper());
            }
            return false;
        }

        public async Task CreatePortfolio(User user)
        {
            if (!UserHasPortfolio(user.DiscordId))
            {
                await InsertPortfolio(new StockPortfolio(user));
            }
        }

        private void AddStockToInternalCache(ulong userId, string stockSymbol, double amount, DateTime updateTime)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                string upperSymbol = stockSymbol.ToUpper();
                OwnedStock stock = new OwnedStock(portfolio, upperSymbol, amount, updateTime);
                portfolio.OwnedStock.Add(stock);
                portfolio.OwnedStockBySymbol.Add(upperSymbol, stock);
            }
        }

        private void UpdateStockAmountInInternalCache(ulong userId, string stockSymbol, double newAmount, DateTime updateTime)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                string upperSymbol = stockSymbol.ToUpper();
                if (portfolio.OwnedStockBySymbol.TryGetValue(upperSymbol, out OwnedStock stock))
                {
                    stock.AmountOwned = newAmount;
                    stock.LastUpdated = updateTime;
                }
            }
        }

        private async Task InsertPortfolio(StockPortfolio portfolio)
        {
            Logger.LogInformation($"Writing a new stock portfolio for user [{portfolio.OwnerRowId}] to the database.");
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.StockPortfolios.Add(portfolio);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                PortfoliosByUserDiscordId.Add(portfolio.Owner.DiscordId, portfolio);
            }
            else
            {
                Logger.LogError($"Writing a new stock portfolio for user [{portfolio.OwnerRowId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task InsertOwnedStock(OwnedStock stock)
        {
            DateTime updateTime = DateTime.UtcNow;
            Logger.LogInformation($"Writing a new owned stock for user [{stock.ParentPortfolio.OwnerRowId}] to the database.");
            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                db.OwnedStocks.Add(stock);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                AddStockToInternalCache(stock.ParentPortfolio.Owner.DiscordId, stock.Symbol, stock.AmountOwned, updateTime);
            }
            else
            {
                Logger.LogError($"Writing a new owned stock for user [{stock.ParentPortfolio.OwnerRowId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task UpdateOwnedStockAmount(OwnedStock stockToUpdate, double newAmount)
        {
            DateTime updateTime = DateTime.UtcNow;
            Logger.LogInformation($"Updating an owned stock [{stockToUpdate.Symbol}] for user [{stockToUpdate.ParentPortfolio.OwnerRowId}] in the database.");

            int writtenCount;
            using (var db = DbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                OwnedStock original = db.OwnedStocks.First(u => u.RowId == stockToUpdate.RowId);
                original.AmountOwned = newAmount;
                original.LastUpdated = updateTime;
                db.OwnedStocks.Update(original);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                UpdateStockAmountInInternalCache(stockToUpdate.ParentPortfolio.Owner.DiscordId, stockToUpdate.Symbol, newAmount, updateTime);
            }
            else
            {
                Logger.LogError($"Updating an owned stock [{stockToUpdate.Symbol}] for user [{stockToUpdate.ParentPortfolio.OwnerRowId}] in the database did not alter any entities. The internal cache was not changed.");
            }
        }
    }
}