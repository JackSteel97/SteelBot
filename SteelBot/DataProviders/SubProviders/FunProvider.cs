using Newtonsoft.Json;
using RestSharp;
using SteelBot.DiscordModules.Fun.Models;
using System;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class FunProvider
{
    private JokeWrapper _cachedJoke;

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
        if (_cachedJoke == null || _cachedJoke.Jokes[0].Joke.Date.Date != DateTime.Today)
        {
            // Needs updating.
            await UpdateJoke();
        }

        return _cachedJoke;
    }
}