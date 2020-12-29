using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun.Models
{
    public class JokeModel
    {
        [JsonProperty("joke")]
        public Joke Joke { get; set; }

    }

    public class Joke
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }

    public class JokeResponse
    {
        [JsonProperty("contents")]
        public JokeWrapper Contents { get; set; }
    }

    public class JokeWrapper
    {
        [JsonProperty("jokes")]
        public JokeModel[] Jokes;

        [JsonProperty("copyright")]
        public string Copyright { get; set; }
    }
}
