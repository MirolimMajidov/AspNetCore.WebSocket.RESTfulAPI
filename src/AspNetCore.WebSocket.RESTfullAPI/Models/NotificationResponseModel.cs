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

        public static ResponseModel CanNotGetAccessToWS(int errorId = 7)
        {
            return ErrorRequest("You can't get access to Websocket!!!", errorId);
        }

        public static NotificationResponseModel SendNotification(object result, string method)
        {
            return new NotificationResponseModel() { Data = result, Method = method };
        }
    }
}