using Newtonsoft.Json;
using System.Collections.Generic;

namespace AspNetCore.WebSocket.RESTfulAPI.TestClient
{
    public class RequestModel
    {
        public string Id { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public Dictionary<string, object> Params { get; set; }

        public object Result { get; set; }
        public bool ShouldSerializeResult() => false;
        public object Data { get; set; }

        public int ErrorId { get; set; }
        public bool ShouldSerializeErrorId() => false;

        [JsonProperty("Description")]
        public string Error { get; set; } = string.Empty;
        public bool ShouldSerializeError() => false;

        public static string SerializeObject(object textJson)
        {
            return JsonConvert.SerializeObject(textJson, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        }

        public static RequestModel DeserializeJsonObject(string textJson)
        {
            return JsonConvert.DeserializeObject<RequestModel>(textJson);
        }
    }

    public static class RequestModelExtensions
    {
        public static string SerializeObject(this RequestModel requestModel)
        {
            return RequestModel.SerializeObject(requestModel);
        }
    }
}
