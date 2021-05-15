using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestClient
{
    public class GetContext
    {
        public static async Task<string> PostRequest(string urlAPI, List<KeyValuePair<string, string>> pairs)
        {
            try
            {
                var httpContent = new FormUrlEncodedContent(pairs);
                using HttpClient client = new();
                using HttpResponseMessage responce = await client.PostAsync(urlAPI, httpContent);
                using HttpContent content = responce.Content;
                return await content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
