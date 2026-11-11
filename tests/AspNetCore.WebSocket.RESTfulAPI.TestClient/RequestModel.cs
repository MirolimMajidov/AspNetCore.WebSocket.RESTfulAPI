using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspNetCore.WebSocket.RESTfulAPI.TestClient
{
    public class RequestModel
    {
        public string Id { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public Dictionary<string, object> Params { get; set; }
        public object Result { get; set; }
        public object Data { get; set; }
        public int ErrorId { get; set; }

        [JsonPropertyName("Description")]
        public string Error { get; set; } = null!;

        private static JsonSerializerOptions JsonOptions { get; } = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string SerializeObject(object textJson) => JsonSerializer.Serialize(textJson, JsonOptions);

        public static RequestModel DeserializeJsonObject(string textJson) =>
            JsonSerializer.Deserialize<RequestModel>(textJson, JsonOptions);
    }

    public static class RequestModelExtensions
    {
        public static string SerializeObject(this RequestModel requestModel) =>
            RequestModel.SerializeObject(requestModel);
    }
}
