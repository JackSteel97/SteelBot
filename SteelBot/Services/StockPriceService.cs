using AlphaVantage.Net.Core.Client;
using AlphaVantage.Net.Core.HttpClientWrapper;
using AlphaVantage.Net.Stocks;
using AlphaVantage.Net.Stocks.Client;
using SteelBot.DataProviders;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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
            var rateLimitedClient = new CustomHttpClientWithRateLimit(httpClient, 5, 1);
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
            HttpResponseMessage? response = null;
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
        }
    }

    public struct CachedStock
    {
        public GlobalQuote stockQuote;
        public DateTime lastUpdated;
    }
}