using AlphaVantage.Net.Core.Client;
using AlphaVantage.Net.Core.HttpClientWrapper;
using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.Client;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteelBot.Services
{
    public class StockPriceService : IDisposable
    {
        private readonly AppConfigurationService AppConfigurationService;
        private readonly DataCache Cache;
        private readonly Dictionary<string, CachedStock> StockCache = new Dictionary<string, CachedStock>();
        private readonly TimeSpan CacheTime;
        private readonly StocksClient StocksClient;

        public StockPriceService(AppConfigurationService appConfigurationService, DataCache cache)
        {
            AppConfigurationService = appConfigurationService;
            CacheTime = TimeSpan.FromMinutes(AppConfigurationService.Application.StockCacheTimeMinutes);

            var httpClient = new HttpClient();
            var rateLimitedClient = new HttpClientWithRateLimit(httpClient, 5, 3);
            var client = new AlphaVantageClient(AppConfigurationService.Application.AlphaVantageApiKey, rateLimitedClient);
            Cache = cache;
            StocksClient = client.Stocks();
        }

        public void Dispose()
        {
            StocksClient.Dispose();
        }

        public async Task<GlobalQuote> GetStock(string stockSymbol)
        {
            string lowerStockSymbol = stockSymbol.ToLower();
            if (StockCache.TryGetValue(lowerStockSymbol, out CachedStock cachedStock))
            {
                // Cache hit.
                var timeSinceLastUpdate = DateTime.UtcNow - cachedStock.lastUpdated;
                if (timeSinceLastUpdate < CacheTime)
                {
                    // Still valid.
                    return cachedStock.stockQuote;
                }
            }

            // Cache miss or not valid
            var stock = await GetStockFromApi(stockSymbol);
            var newCachedStock = new CachedStock()
            {
                lastUpdated = DateTime.UtcNow,
                stockQuote = stock
            };
            if (StockCache.ContainsKey(lowerStockSymbol))
            {
                // Update.
                StockCache[lowerStockSymbol] = newCachedStock;
            }
            else
            {
                // Insert.
                StockCache.Add(lowerStockSymbol, newCachedStock);
            }
            return stock;
        }

        private async Task<GlobalQuote> GetStockFromApi(string stockSymbol)
        {
            try
            {
                GlobalQuote quote = await StocksClient.GetGlobalQuoteAsync(stockSymbol);
                return quote;
            }
            catch (Exception e)
            {
                // Likely entered an invalid stock symbol.
                await Cache.Exceptions.InsertException(new Database.Models.ExceptionLog(e, $"GetStockPrice({stockSymbol})"));
                return null;
            }
        }
    }

    public struct CachedStock
    {
        public GlobalQuote stockQuote;
        public DateTime lastUpdated;
    }
}