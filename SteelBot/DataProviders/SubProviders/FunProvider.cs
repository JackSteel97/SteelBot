using Newtonsoft.Json;
using RestSharp;
using SteelBot.DiscordModules.Fun.Models;
using System;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class FunProvider
    {
        private JokeWrapper CachedJoke;

        public FunProvider()
        {
        }

        private async Task UpdateJoke()
        {
            RestClient client = new RestClient("https://api.jokes.one");
            RestRequest request = new RestRequest("jod", Method.Get);

            RestResponse response = await client.ExecuteAsync(request);

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