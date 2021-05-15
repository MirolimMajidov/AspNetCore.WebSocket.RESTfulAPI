using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Models
{
    public class WSRequestModel
    {
        public int ErrorId { get; set; }
        public bool ShouldSerializeErrorId() => ErrorId > 0;

        [JsonProperty("Description")]
        public string Error { get; set; } = string.Empty;
        public bool ShouldSerializeError() => !string.IsNullOrEmpty(Error);

        /// <summary>
        /// It will be get value when the data returning by Web Socket RESTfull API
        /// </summary>
        public object Result { get; set; }
        public bool ShouldSerializeResult() => Result != null;

        public static async Task<WSRequestModel> ErrorRequestAsync(string error = "error", int errorId = 400)
        {
            return await Task.Run(() => new WSRequestModel() { Error = error, ErrorId = errorId });
        }

        public static async Task<WSRequestModel> SuccessAsync(object result = null)
        {
            return await Task.Run(() =>
            {
                if (result == null) result = new { };

                return new WSRequestModel() { Result = result };
            });
        }

        public static async Task<WSRequestModel> NotAccessAsync(string error = "You can't get access to Websocket!!!", int errorId = 400)
        {
            return await ErrorRequestAsync(error, errorId);
        }

        public static async Task<WSRequestModel> NotFoundAsync(string error = "Not found", int errorId = 404)
        {
            return await ErrorRequestAsync(error, errorId);
        }

        public static string GenaretJson(object textJson)
        {
            return JsonConvert.SerializeObject(textJson, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }

        public string Method { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public Dictionary<string, object> Params { get; set; }
        public bool ShouldSerializeParams() => false;

        /// <summary>
        /// It will be get value when the data sending by Notification
        /// </summary>
        public object Data { get; set; }
        public bool ShouldSerializeData() => Data != null;

        public static WSRequestModel SuccessRequest(object result, string method)
        {
            return new WSRequestModel() { Result = result, Method = method };
        }

        public static WSRequestModel SendNotification(object result, string method)
        {
            return new WSRequestModel() { Data = result, Method = method };
        }
    }

    internal static class WSRequestModelExtensions
    {
        public static string GenaretJson(this WSRequestModel requestModel)
        {
            return WSRequestModel.GenaretJson(requestModel);
        }
    }
}