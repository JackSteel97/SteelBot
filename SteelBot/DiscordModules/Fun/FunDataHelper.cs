using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Fun
{
    public class FunDataHelper
    {
        public async Task<Stream> GetMotivationalQuote()
        {
            RestClient client = new RestClient("http://inspirobot.me/");
            RestRequest request = new RestRequest("api", Method.Get);
            request.AddQueryParameter("generate", "true");

            RestResponse response = await client.ExecuteAsync(request);

            var imageUrl= response.Content;

            RestClient imageClient = new RestClient(imageUrl);
            return await imageClient.DownloadStreamAsync(new RestRequest());
        }
    }
}
