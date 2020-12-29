using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using SteelBot.Database;
using SteelBot.Database.Models;
using SteelBot.DiscordModules.Fun.Models;
using SteelBot.Services.Configuration;
using System;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class FunProvider
    {
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;
        private JokeWrapper CachedJoke;

        public FunProvider()
        {
            DbContextFactory = contextFactory;
            AppConfigurationService = appConfig;
        }

        private async Task UpdateJoke()
        {
            RestClient client = new RestClient("https://api.jokes.one");
            RestRequest request = new RestRequest("jod", Method.GET, DataFormat.Json);

            IRestResponse response = await client.ExecuteAsync(request);

            JokeResponse jokeData = JsonConvert.DeserializeObject<JokeResponse>(response.Content);

            CachedJoke = jokeData.Contents;
        }

        public async Task<JokeWrapper> GetJoke()
        {
            if (CachedJoke == null || CachedJoke.Jokes[0].Joke.Date.Date != DateTime.Today)
            {
                // Needs updating.
                await UpdateJoke();
            }
            return CachedJoke;
        }
    }
}