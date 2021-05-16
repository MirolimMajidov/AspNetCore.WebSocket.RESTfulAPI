using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public class ResponseModel
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

        public static ResponseModel ErrorRequest(string error = "error", int errorId = 999)
        {
            return new ResponseModel() { Error = error, ErrorId = errorId };
        }

        public static async Task<ResponseModel> ErrorRequestAsync(string error = "error", int errorId = 999)
        {
            return await Task.Run(() => ErrorRequest(error, errorId));
        }

        public static ResponseModel SuccessRequest(object result = null)
        {
            if (result == null) result = new { };
            return new ResponseModel() { Result = result };
        }

        public static async Task<ResponseModel> SuccessRequestAsync(object result = null)
        {
            return await Task.Run(() => SuccessRequest(result));
        }

        public static async Task<ResponseModel> NotFoundAsync(string error = "Not found", int errorId = 404)
        {
            return await ErrorRequestAsync(error, errorId);
        }

        public static ResponseModel NotAccess(int errorId = 10)
        {
            return ErrorRequest("You haven't access to API!", errorId);
        }

        public static async Task<ResponseModel> NotAccessAsync(int errorId = 10)
        {
            return await Task.Run(() => NotAccess(errorId));
        }

        public static string GenerateJson(object textJson)
        {
            return JsonConvert.SerializeObject(textJson, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
    }

    public static class RequestModelExtensions
    {
        public static string GenerateJson(this ResponseModel requestModel)
        {
            return ResponseModel.GenerateJson(requestModel);
        }
    }
}