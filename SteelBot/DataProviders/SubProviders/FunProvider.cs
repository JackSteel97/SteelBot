using Newtonsoft.Json;
using RestSharp;
using Sentry;
using SteelBot.DiscordModules.Fun.Models;
using SteelBot.Helpers.Sentry;
using System;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class FunProvider
{
    private JokeWrapper _cachedJoke;
    private readonly IHub _sentry;

    public FunProvider(IHub sentry)
    {
        _sentry = sentry;
    }

    private async Task UpdateJoke()
    {
        var client = new RestClient("https://api.jokes.one");
        var request = new RestRequest("jod", Method.Get);

        var response = await client.ExecuteAsync(request);

        var jokeData = JsonConvert.DeserializeObject<JokeResponse>(response.Content);

        _cachedJoke = jokeData.Contents;
    }

    public async Task<JokeWrapper> GetJoke()
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(GetJoke));
        if (_cachedJoke == null || _cachedJoke.Jokes[0].Joke.Date.Date != DateTime.Today)
        {
            transaction.StartChild("Get Joke From API");
            // Needs updating.
            await UpdateJoke();
            transaction.Finish();
        }

        transaction.Finish();
        return _cachedJoke;
    }
}