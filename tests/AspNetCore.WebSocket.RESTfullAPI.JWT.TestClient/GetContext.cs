using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestClient
{
    public class GetContext
    {
        public static async Task<T> PostRequest<T>(string urlAPI, Dictionary<string, string> pairs)
        {
            try
            {
                var httpContent = new FormUrlEncodedContent(pairs);
                using HttpClient client = new();
                using HttpResponseMessage responce = await client.PostAsync(urlAPI, httpContent);
                using HttpContent content = responce.Content;
                var responceData =  await content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(responceData);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
