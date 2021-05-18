using System.Collections.Generic;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public class NotificationResponseModel : ResponseModel
    {
        public string Method { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public Dictionary<string, object> Params { get; set; }
        public bool ShouldSerializeParams() => false;

        /// <summary>
        /// It will be get value when the data sending by Notification
        /// </summary>
        public object Data { get; set; }
        public bool ShouldSerializeData() => Data != null;

        /// <summary>
        /// Create response model for NoAccess to WS an error. Massage of error will be "You cannot get access to Websocket!!!"
        /// </summary>
        /// <param name="errorId">Id of error</param>
        /// <returns>Created model</returns>
        public static ResponseModel NoAccessToWS(int errorId = 7)
        {
            return ErrorRequest("You cannot get access to Websocket!!!", errorId);
        }

        /// <summary>
        /// This is only to create response model for sending notification to the client
        /// </summary>
        /// <param name="result">Data to transfer the client</param>
        /// <param name="method">Receive method name for sending notification to the client</param>
        /// <returns>Created model</returns>
        public static NotificationResponseModel SendNotification(object result, string method)
        {
            return new NotificationResponseModel() { Data = result, Method = method };
        }
    }
}