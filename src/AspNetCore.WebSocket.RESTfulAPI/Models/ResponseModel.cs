using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class ResponseModel
{
    public int ErrorId { get; set; }
    public bool ShouldSerializeErrorId() => ErrorId > 0;

    [JsonProperty("Description")]
    public string Error { get; set; } = string.Empty;
    public bool ShouldSerializeError() => !string.IsNullOrEmpty(Error);

    /// <summary>
    /// It will be get value when the data returning by Web Socket RESTful API
    /// </summary>
    public object Result { get; set; }
    public bool ShouldSerializeResult() => Result != null;

    /// <summary>
    /// Create response model for an error
    /// </summary>
    /// <param name="error">Message of error</param>
    /// <param name="errorId">Id of error</param>
    /// <returns>Created model</returns>
    public static ResponseModel ErrorRequest(string error = "error", int errorId = 400)
    {
        return new ResponseModel() { Error = error, ErrorId = errorId };
    }

    /// <summary>
    /// Create response model for an error
    /// </summary>
    /// <param name="error">Message of error</param>
    /// <param name="errorId">Id of error</param>
    /// <returns>Created model</returns>
    public static async Task<ResponseModel> ErrorRequestAsync(string error = "error", int errorId = 400)
    {
        return await Task.Run(() => ErrorRequest(error, errorId));
    }

    /// <summary>
    /// Create response model for a success request
    /// </summary>
    /// <param name="result">Data to transfer the client</param>
    /// <returns>Created model</returns>
    public static ResponseModel SuccessRequest(object result = null)
    {
        if (result == null) result = new { };
        return new ResponseModel() { Result = result };
    }

    /// <summary>
    /// Create response model for a success request
    /// </summary>
    /// <param name="result">Data to transfer the client</param>
    /// <returns>Created model</returns>
    public static async Task<ResponseModel> SuccessRequestAsync(object result = null)
    {
        return await Task.Run(() => SuccessRequest(result));
    }

    /// <summary>
    /// Create response model for NoAccess error. Massage of error will be "You haven't access to API!"
    /// </summary>
    /// <param name="errorId">Id of error</param>
    /// <returns>Created model</returns>
    public static ResponseModel NoAccess(int errorId = 401)
    {
        return ErrorRequest("You haven't access to API!", errorId);
    }

    /// <summary>
    /// Create response model for NoAccess error. Massage of error will be "You haven't access to API!"
    /// </summary>
    /// <param name="errorId">Id of error</param>
    /// <returns>Created model</returns>
    public static async Task<ResponseModel> NoAccessAsync(int errorId = 401)
    {
        return await Task.Run(() => NoAccess(errorId));
    }

    /// <summary>
    /// Serialize any object to string
    /// </summary>
    /// <param name="textJson">Object for serializing</param>
    /// <param name="logger">ILogger to write an error if it cannot serialize</param>
    /// <returns>Serialized string value</returns>
    public static string GenerateJson(object textJson, ILogger logger = null)
    {
        try
        {
            return JsonConvert.SerializeObject(textJson, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() });
        }
        catch (Exception ex)
        {
            if (logger != null)
                logger.LogError($"An error on serializing object. Message: {ex.Message}!");

            return null;
        }
    }
}

public static class RequestModelExtensions
{
    /// <summary>
    /// Serialize current object to string
    /// </summary>
    /// <param name="logger">ILogger to write an error if it cannot serialize</param>
    /// <returns>Serialized string value</returns>
    public static string GenerateJson(this ResponseModel requestModel, ILogger logger = null)
    {
        return ResponseModel.GenerateJson(requestModel, logger);
    }
}