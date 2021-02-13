using AlphaVantage.Net.Core.Client;
using AlphaVantage.Net.Core.HttpClientWrapper;
using AlphaVantage.Net.Forex;
using AlphaVantage.Net.Forex.Client;
using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.Client;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.Services
{
    public class StockPriceService : IDisposable
    {
        private readonly AppConfigurationService AppConfigurationService;
        private readonly DataCache Cache;
        private readonly ConcurrentDictionary<string, CachedStock> StockCache = new ConcurrentDictionary<string, CachedStock>();
        private readonly TimeSpan CacheTime;
        private readonly StocksClient StocksClient;
        private readonly ForexClient FxClient;
        private CachedFxRate CachedFxRate = new CachedFxRate();

        public StockPriceService(AppConfigurationService appConfigurationService, DataCache cache)
        {
            AppConfigurationService = appConfigurationService;
            CacheTime = TimeSpan.FromMinutes(AppConfigurationService.Application.StockCacheTimeMinutes);

            var httpClient = new HttpClient();
            var rateLimitedClient = new CustomHttpClientWithRateLimit(httpClient, 4, 1);
            var client = new AlphaVantageClient(AppConfigurationService.Application.AlphaVantageApiKey, rateLimitedClient);
            Cache = cache;
            StocksClient = client.Stocks();
            FxClient = client.Forex();
        }

        public void Dispose()
        {
            StocksClient.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<ForexExchangeRate> GetGbpUsdExchangeRate()
        {
            var timeSinceLastUpdate = DateTime.UtcNow - CachedFxRate.lastUpdated;
            if (timeSinceLastUpdate >= CacheTime)
            {
                // Get new rate.
                var rate = await FxClient.GetExchangeRateAsync(AlphaVantage.Net.Common.Currencies.PhysicalCurrency.USD, AlphaVantage.Net.Common.Currencies.PhysicalCurrency.GBP);
                CachedFxRate.exchangeRate = rate;
                CachedFxRate.lastUpdated = DateTime.UtcNow;
            }
            return CachedFxRate.exchangeRate;
        }

        public async Task<GlobalQuote> SearchStock(string keywords)
        {
            var matches = await StocksClient.SearchSymbolAsync(keywords);

            if (matches.Count > 0)
            {
                return await GetStock(matches.First().Symbol);
            }
            return null;
        }

        public async Task<GlobalQuote> GetStock(string stockSymbol)
        {
            string upperStockSymbol = stockSymbol.ToUpper();
            var stockFromCache = GetStockFromInternalCache(upperStockSymbol);
            if (stockFromCache != null)
            {
                return stockFromCache;
            }

            // Cache miss or not valid
            return await GetAndCacheStock(upperStockSymbol);
        }

        private async Task<GlobalQuote> GetAndCacheStock(string stockSymbol)
        {
            var stock = await GetStockFromApi(stockSymbol);
            var newCachedStock = new CachedStock()
            {
                lastUpdated = DateTime.UtcNow,
                stockQuote = stock
            };
            if (StockCache.ContainsKey(stockSymbol))
            {
                // Update.
                StockCache[stockSymbol] = newCachedStock;
            }
            else
            {
                // Insert.
                StockCache.TryAdd(stockSymbol, newCachedStock);
            }
            return stock;
        }

        public GlobalQuote GetStockFromInternalCache(string stockSymbol)
        {
            if (StockCache.TryGetValue(stockSymbol, out CachedStock cachedStock))
            {
                // Cache hit.
                var timeSinceLastUpdate = DateTime.UtcNow - cachedStock.lastUpdated;
                if (timeSinceLastUpdate < CacheTime)
                {
                    // Still valid.
                    return cachedStock.stockQuote;
                }
            }
            return null;
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

    /// <summary>
    /// Only requests passing through given instance of the client are throttled.
    /// Two different instances of the client may have totally different rate limits.
    /// </summary>
    /// <remarks>
    /// Pulled out from AlphaVantage.Net library source at (https://github.com/LutsenkoKirill/AlphaVantage.Net) and altered to fix implementation.
    /// For fixes see: https://github.com/LutsenkoKirill/AlphaVantage.Net/pull/20/files
    /// </remarks>
    public class CustomHttpClientWithRateLimit : IHttpClientWrapper
    {
        private readonly HttpClient _client;
        private readonly TimeSpan _minRequestInterval;//Calculated based on rpm limit in constructor
        private readonly Semaphore _concurrentRequestsCounter;
        private DateTime _previousRequestStartTime;
        private readonly object _lockObject = new object();

        public CustomHttpClientWithRateLimit(HttpClient client, int maxRequestPerMinutes, int maxConcurrentRequests)
        {
            _client = client;
            _minRequestInterval = new TimeSpan(0, 0, 0, 0, 60000 / maxRequestPerMinutes);
            _concurrentRequestsCounter = new Semaphore(maxConcurrentRequests, maxConcurrentRequests);
            _previousRequestStartTime = DateTime.MinValue;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            HttpResponseMessage response = null;
            _concurrentRequestsCounter.WaitOne();
            await WaitForRequestedMinimumInterval();
            try
            {
                response = await _client.SendAsync(request);
            }
            finally
            {
                _concurrentRequestsCounter.Release();
            }

            return response;
        }

        private async Task WaitForRequestedMinimumInterval()
        {
            TimeSpan? delayInterval = null;
            lock (_lockObject)
            {
                var timeSinceLastRequest = DateTime.Now - _previousRequestStartTime;
                if (timeSinceLastRequest < _minRequestInterval)
                {
                    delayInterval = _minRequestInterval - timeSinceLastRequest;
                }
                _previousRequestStartTime = DateTime.Now;
                if (delayInterval.HasValue)
                {
                    _previousRequestStartTime.AddMilliseconds(delayInterval.Value.Milliseconds);
                }
            }

            if (delayInterval.HasValue)
            {
                await Task.Delay((int)Math.Ceiling(delayInterval.Value.TotalMilliseconds));
            }
        }

        public void SetTimeOut(TimeSpan timeSpan)
        {
            _client.Timeout = timeSpan;
        }

        public void Dispose()
        {
            _client.Dispose();
            _concurrentRequestsCounter.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public struct CachedStock
    {
        public GlobalQuote stockQuote;
        public DateTime lastUpdated;
    }

    public struct CachedFxRate
    {
        public ForexExchangeRate exchangeRate;
        public DateTime lastUpdated;
    }
}