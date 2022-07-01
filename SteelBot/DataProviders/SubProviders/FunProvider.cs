using Newtonsoft.Json;
using RestSharp;
using Sentry;
using SteelBot.DiscordModules.Fun.Models;
using SteelBot.Helpers.Sentry;
using System;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class FunProvider
    {
        private JokeWrapper CachedJoke;
        private readonly IHub _sentry;

        public FunProvider(IHub sentry)
        {
            _sentry = sentry;
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
            var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(GetJoke));
            if (CachedJoke == null || CachedJoke.Jokes[0].Joke.Date.Date != DateTime.Today)
            {
                transaction.StartChild("Get Joke From API");
                // Needs updating.
                await UpdateJoke();
                transaction.Finish();
            }

            transaction.Finish();
            return CachedJoke;
        }
    }
}