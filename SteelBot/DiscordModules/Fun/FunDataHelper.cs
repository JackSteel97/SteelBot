using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun;

public class FunDataHelper
{
    public static async Task<Stream> GetMotivationalQuote()
    {
        var client = new RestClient("http://inspirobot.me/");
        var request = new RestRequest("api", Method.Get);
        request.AddQueryParameter("generate", "true");

        var response = await client.ExecuteAsync(request);

        string imageUrl = response.Content;

        var imageClient = new RestClient(imageUrl);
        return await imageClient.DownloadStreamAsync(new RestRequest());
    }
}