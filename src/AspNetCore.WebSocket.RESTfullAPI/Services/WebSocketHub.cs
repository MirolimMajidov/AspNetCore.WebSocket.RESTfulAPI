using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public class WebSocketHub : Disposable
    {
        public readonly IWebSocketManager WSManager;
        private readonly ILogger _logger;

        public WebSocketHub(IWebSocketManager _manager, ILogger<WebSocketHub> logger)
        {
            WSManager = _manager;
            _logger = logger;
        }

        /// <summary>
        /// To connect new created WebSocket to the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socket">New created socket</param>
        /// <param name="userInfo">Current User info</param>
        public virtual async Task OnConnectedAsync(System.Net.WebSockets.WebSocket socket, WSUserInfo userInfo)
        {
            await WSManager.AddSocket(userInfo.Id, socket, userInfo);

            await SendNotificationAsync(userInfo.Id, new { }, Notification.WSConnected);
        }

        /// <summary>
        /// To disconnect exists WebSocket from the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socketId">Id of current WebSocket to disconnect</param>
        /// <returns></returns>
        public virtual async Task OnDisconnectedAsync(object socketId)
        {
            await WSManager?.RemoveSocket(socketId, closeDescription: "The connection closed by the client", nitifyClient: false);
        }

        /// <summary>
        /// The main method to send response message to the client
        /// </summary>
        /// <param name="socket">WebSocket to send response message</param>
        /// <param name="responseMessage">The response data to send message</param>
        public static async Task SendMessageAsync(System.Net.WebSockets.WebSocket socket, string responseMessage, ILogger logger, string method = "", object userId = null)
        {
            try
            {
                if (socket?.State == WebSocketState.Open)
                {
                    if (WebSocketManager.LoggAllWSRequest && logger != null)
                        logger.LogInformation($"User id: {userId}, Method: {method}, Response data: {responseMessage}");

                    var bufferContant = Encoding.UTF8.GetBytes(responseMessage);
                    var buffer = new ArraySegment<byte>(array: bufferContant, offset: 0, count: bufferContant.Length);
                    await socket.SendAsync(buffer: buffer, messageType: WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: CancellationToken.None);
                }
            }
            catch
            { }
        }

        /// <summary>
        /// The method to send response message to the client
        /// </summary>
        /// <param name="socket">WebSocket to send response message</param>
        /// <param name="responseData">The response data to send message</param>
        /// <param name="method">Method name to natify the client</param>
        public static async Task SendNotificationAsync(System.Net.WebSockets.WebSocket socket, object responseData, string method, ILogger logger, object userId = null)
        {
            var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson();
            await SendMessageAsync(socket, responseMessage: responseMessage, method: method, userId: userId, logger: logger);
        }

        /// <summary>
        /// The method to send response message to the client
        /// </summary>
        /// <param name="userId">User's Id to send response message</param>
        /// <param name="responseData">The response data to send message</param>
        /// <param name="method">Method name to natify the client</param>
        public async Task SendNotificationAsync(object userId, object responseData, string method)
        {
            var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson();
            await SendMessageAsync(WSManager.GetWebSocket(userId), responseMessage: responseMessage, method: method, userId: userId, logger: _logger);
        }

        /// <summary>
        /// The method to send response message to the client
        /// </summary>
        /// <param name="userIds">List of User's Id to send response message</param>
        /// <param name="responseData">The response data to send message</param>
        /// <param name="method">Method name to natify the client</param>
        public async Task SendNotificationAsync(IEnumerable<object> userIds, object responseData, string method)
        {
            var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson();
            foreach (var socketId in userIds)
                await SendMessageAsync(WSManager.GetWebSocket(socketId), responseMessage: responseMessage, method: method, userId: socketId, logger: _logger);
        }

        /// <summary>
        /// The main method to receive requested message from the client
        /// </summary>
        /// <param name="socket">WebSocket which receive message</param>
        /// <param name="receiveMessageData">Receive message data from user</param>
        public async Task ReceiveMessageAsync(System.Net.WebSockets.WebSocket socket, string receiveMessageData)
        {
            ResponseModel responseModel = null;
            string requestId = string.Empty;
            string requestMethod = string.Empty;
            object userId = null;
            try
            {
                var requestModel = JsonConvert.DeserializeObject<NotificationResponseModel>(receiveMessageData);
                var userInfo = WSManager.GetUserInfo(socket);
                userId = userInfo?.Id;
                if (WebSocketManager.LoggAllWSRequest)
                    _logger.LogInformation($"User id: {userId}, Method: {requestModel.Method}, Request data: {receiveMessageData}");
                requestId = requestModel.Id;
                requestMethod = requestModel.Method;
                if (userInfo == null)
                {
                    responseModel = await ResponseModel.NoAccessAsync(errorId: 105);
                }
                else
                {
                    if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(requestMethod))
                    {
                        responseModel = await ResponseModel.NoAccessAsync(errorId: 102);
                    }
                    else
                    {
                        var methodLevels = requestMethod.Split('.');
                        if (methodLevels.Length == 2)
                        {
                            Type callingController = Type.GetType($"{WebSocketManager.CurrentAssemblyName.Split(',').First()}.Hubs.{methodLevels.First()}Controller, {WebSocketManager.CurrentAssemblyName}");
                            if (callingController != null)
                            {
                                var wsHubMethod = callingController.GetMethods().FirstOrDefault(m => m.GetWSHubAttribute()?.Name == requestModel.Method);
                                var paramsLength = requestModel.Params?.Count ?? 0;
                                if (wsHubMethod != null)
                                {
                                    object[] methodParams = null;
                                    if (paramsLength >= 0)
                                    {
                                        var parameters = new List<object>();
                                        foreach (var parameter in wsHubMethod.GetParameters())
                                        {
                                            var value = requestModel.Params.ContainsKey(parameter.Name) ? requestModel.Params[parameter.Name].ConvertTo(parameter.ParameterType) : parameter.DefaultValue;
                                            parameters.Add(value);
                                        }
                                        methodParams = parameters.ToArray();
                                    }
                                    var callingClass = Activator.CreateInstance(callingController, new object[] { this, userInfo, _logger });
                                    responseModel = await (Task<ResponseModel>)wsHubMethod.Invoke(callingClass, methodParams);
                                }
                                else
                                {
                                    responseModel = await ResponseModel.ErrorRequestAsync($"{requestMethod} method's parameters is invalid", 106);
                                }
                            }
                            else
                            {
                                responseModel = await ResponseModel.ErrorRequestAsync($"{requestMethod} method of {methodLevels.First()} class is invalid", 104);
                            }
                        }
                        else
                        {
                            responseModel = await ResponseModel.ErrorRequestAsync("Websocket request will support only two levels methods", 103);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Websocket request is invalid");
                responseModel = await ResponseModel.ErrorRequestAsync("Websocket request is invalid", 101);
            }

            if (responseModel != null)
            {
                var response = new NotificationResponseModel()
                {
                    Id = requestId,
                    Method = requestMethod,
                    ErrorId = responseModel.ErrorId,
                    Error = responseModel.Error,
                    Result = responseModel.Result
                };
                await SendMessageAsync(socket, response.GenerateJson(), logger: _logger, method: response.Method, userId: userId);
            }
        }
    }
}
