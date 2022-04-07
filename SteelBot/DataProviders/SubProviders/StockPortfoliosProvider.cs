using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.Database.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class StockPortfoliosProvider
    {
        private readonly ILogger<StockPortfoliosProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly Dictionary<ulong, StockPortfolio> PortfoliosByUserDiscordId;

        public StockPortfoliosProvider(ILogger<StockPortfoliosProvider> logger, IDbContextFactory<SteelBotContext> contextFactory)
        {
            Logger = logger;
            DbContextFactory = contextFactory;

            PortfoliosByUserDiscordId = new Dictionary<ulong, StockPortfolio>();
            LoadPortfoliosData();
        }

        public void LoadPortfoliosData()
        {
            Logger.LogInformation("Loading data from database: Portfolios");

            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                IIncludableQueryable<StockPortfolio, List<OwnedStock>> allPortfolios = db.StockPortfolios.AsNoTracking()
                    .Include(pf => pf.Owner)
                    .Include(pf => pf.Snapshots)
                    .Include(pf => pf.OwnedStock);
                foreach (StockPortfolio portfolio in allPortfolios)
                {
                    // Make sure the snapshots are in ascending order.
                    portfolio.Snapshots = portfolio.Snapshots.OrderBy(ss => ss.SnapshotTaken).ToList();

                    portfolio.BuildOwnedStockCache();
                    PortfoliosByUserDiscordId.Add(portfolio.Owner.DiscordId, portfolio);
                }
            }
        }

        public List<StockPortfolio> TryGetPortfolios(IEnumerable<ulong> userIds)
        {
            List<StockPortfolio> result = new List<StockPortfolio>();
            foreach (ulong userId in userIds)
            {
                if (TryGetPortfolio(userId, out StockPortfolio portfolio))
                {
                    result.Add(portfolio);
                }
            }
            return result;
        }

        public bool TryGetPortfolio(ulong userId, out StockPortfolio portfolio)
        {
            return PortfoliosByUserDiscordId.TryGetValue(userId, out portfolio);
        }

        public bool TryGetOwnedStock(ulong userId, string stockSymbol, out OwnedStock stock)
        {
            stock = null;
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                return portfolio.OwnedStockBySymbol.TryGetValue(stockSymbol.ToUpper(), out stock);
            }
            return false;
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
                await InsertPortfolio(new StockPortfolio(user), user);
            }
        }

        private void AddStockToInternalCache(ulong userId, string stockSymbol, decimal amount, DateTime updateTime, long rowId)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                string upperSymbol = stockSymbol.ToUpper();
                OwnedStock stock = new OwnedStock(portfolio, upperSymbol, amount, updateTime);
                stock.RowId = rowId;
                portfolio.OwnedStock.Add(stock);
                portfolio.OwnedStockBySymbol.Add(upperSymbol, stock);
            }
        }

        private void AddSnapshotToInternalCache(ulong userId, StockPortfolioSnapshot snapshot)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                snapshot.ParentPortfolio = portfolio;
                portfolio.Snapshots.Add(snapshot);
            }
        }

        private void UpdateStockAmountInInternalCache(ulong userId, string stockSymbol, decimal newAmount, DateTime updateTime)
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

        private void RemoveStockFromInternalCache(ulong userId, string stockSymbol)
        {
            if (PortfoliosByUserDiscordId.TryGetValue(userId, out StockPortfolio portfolio))
            {
                string upperSymbol = stockSymbol.ToUpper();
                portfolio.OwnedStockBySymbol.Remove(upperSymbol);
                portfolio.OwnedStock.RemoveAt(
                    portfolio.OwnedStock.FindIndex(s =>
                        s.Symbol.Equals(upperSymbol, StringComparison.OrdinalIgnoreCase))
                    );
            }
        }

        private async Task InsertPortfolio(StockPortfolio portfolio, User owner)
        {
            Logger.LogInformation($"Writing a new stock portfolio for user [{portfolio.OwnerRowId}] to the database.");
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                portfolio.Owner = null;
                db.StockPortfolios.Add(portfolio);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                portfolio.Owner = owner;
                portfolio.OwnedStock = new List<OwnedStock>();
                portfolio.Snapshots = new List<StockPortfolioSnapshot>();
                portfolio.BuildOwnedStockCache();
                PortfoliosByUserDiscordId.Add(portfolio.Owner.DiscordId, portfolio);
            }
            else
            {
                Logger.LogError($"Writing a new stock portfolio for user [{portfolio.OwnerRowId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        public async Task AddPortfolioSnapshot(ulong userId, StockPortfolioSnapshot snapshot)
        {
            await InsertPortfolioSnapshot(userId, snapshot);
        }

        public async Task AddNewOwnedStock(ulong userId, OwnedStock stock)
        {
            await InsertOwnedStock(userId, stock);
        }

        public async Task UpdateOwnedStock(ulong userId, OwnedStock newStock)
        {
            if (newStock.AmountOwned <= 0)
            {
                // Remove it.
                await DeleteOwnedStock(userId, newStock);
            }
            else
            {
                await UpdateOwnedStockAmount(userId, newStock, newStock.AmountOwned);
            }
        }

        private async Task InsertOwnedStock(ulong userId, OwnedStock stock)
        {
            DateTime updateTime = DateTime.UtcNow;
            stock.LastUpdated = updateTime;
            Logger.LogInformation($"Writing a new owned stock for portfolio [{stock.ParentPortfolioRowId}] to the database.");
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.OwnedStocks.Add(stock);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                AddStockToInternalCache(userId, stock.Symbol, stock.AmountOwned, updateTime, stock.RowId);
            }
            else
            {
                Logger.LogError($"Writing a new owned stock for portfolio [{stock.ParentPortfolioRowId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task InsertPortfolioSnapshot(ulong userId, StockPortfolioSnapshot snapshot)
        {
            Logger.LogInformation($"Writing a new snapshot for portfolio [{snapshot.ParentPortfolioRowId}] to the database.");
            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                db.StockPortfolioSnapshots.Add(snapshot);
                writtenCount = await db.SaveChangesAsync();
            }
            if (writtenCount > 0)
            {
                AddSnapshotToInternalCache(userId, snapshot);
            }
            else
            {
                Logger.LogError($"Writing a new snapshot for portfolio [{snapshot.ParentPortfolioRowId}] to the database inserted no entities. The internal cache was not changed.");
            }
        }

        private async Task UpdateOwnedStockAmount(ulong userId, OwnedStock stockToUpdate, decimal newAmount)
        {
            DateTime updateTime = DateTime.UtcNow;
            Logger.LogInformation($"Updating an owned stock [{stockToUpdate.Symbol}] for portfolio [{stockToUpdate.ParentPortfolioRowId}] in the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
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
                UpdateStockAmountInInternalCache(userId, stockToUpdate.Symbol, newAmount, updateTime);
            }
            else
            {
                Logger.LogError($"Updating an owned stock [{stockToUpdate.Symbol}] for portfolio [{stockToUpdate.ParentPortfolioRowId}] in the database did not alter any entities. The internal cache was not changed.");
            }
        }

        private async Task DeleteOwnedStock(ulong userId, OwnedStock stockToUpdate)
        {
            Logger.LogInformation($"Deleting an owned stock [{stockToUpdate.Symbol}] for user [{stockToUpdate.ParentPortfolioRowId}] from the database.");

            int writtenCount;
            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                // To prevent EF tracking issue, grab and alter existing value.
                OwnedStock original = db.OwnedStocks.First(u => u.RowId == stockToUpdate.RowId);
                db.OwnedStocks.Remove(original);
                writtenCount = await db.SaveChangesAsync();
            }

            if (writtenCount > 0)
            {
                RemoveStockFromInternalCache(userId, stockToUpdate.Symbol);
            }
            else
            {
                Logger.LogError($"Deleting an owned stock [{stockToUpdate.Symbol}] for user [{stockToUpdate.ParentPortfolioRowId}] from the database did not alter any entities. The internal cache was not changed.");
            }
        }
    }
}