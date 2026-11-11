using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.TestClient
{
    public class GetContext
    {
        public static async Task<T> PostRequest<T>(string urlAPI, Dictionary<string, string> pairs)
        {
            try
            {
                var httpContent = new FormUrlEncodedContent(pairs);
                using HttpClient client = new();
                using HttpResponseMessage response = await client.PostAsync(urlAPI, httpContent);
                using HttpContent content = response.Content;
                var responseData = await content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(responseData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
