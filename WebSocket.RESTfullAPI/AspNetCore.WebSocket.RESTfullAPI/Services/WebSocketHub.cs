using AspNetCore.WebSocket.RESTfullAPI.Configurations;
using AspNetCore.WebSocket.RESTfullAPI.Middlewares;
using AspNetCore.WebSocket.RESTfullAPI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Services
{
    public class WebSocketHub : Disposable
    {
        public readonly WebSocketManager WebSocketManager;
        private readonly ILogger<WebSocketHub> _logger;

        public WebSocketHub(WebSocketManager webSocketManager, ILogger<WebSocketHub> logger)
        {
            WebSocketManager = webSocketManager;
            _logger = logger;
        }

        /// <summary>
        /// To connect new created WebSocket to the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socket">New created socket</param>
        /// <param name="userId">Current User's id</param>
        /// <param name="userName">Current User's name</param>
        public async Task OnConnectedAsync(System.Net.WebSockets.WebSocket socket, Guid userId, string userName)
        {
            var info = new WSUserInfo()
            {
                UserId = userId,
                UserName = userName
            };

            await WebSocketManager.AddSocket(userId, socket, info);

            await SendNotificationAsync(userId, new { }, Notification.WSConnected);
        }

        /// <summary>
        /// To disconnect exists WebSocket from the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socketId">Id of current WebSocket to disconnect</param>
        /// <returns></returns>
        public async Task OnDisconnectedAsync(Guid socketId)
        {
            await WebSocketManager.RemoveSocket(socketId, closeDescription: "The connection closed by the client", nitifyClient: false);
        }

        /// <summary>
        /// The main method to send response message to the client
        /// </summary>
        /// <param name="socket">WebSocket to send response message</param>
        /// <param name="responseMessage">The response data to send message</param>
        public static async Task SendMessageAsync(System.Net.WebSockets.WebSocket socket, string responseMessage, ILogger logger, string method = "", Guid? userId = null)
        {
            try
            {
                if (socket?.State == WebSocketState.Open)
                {
                    if (WebSocketManager.LoggAllWSRequest)
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
        public static async Task SendNotificationAsync(System.Net.WebSockets.WebSocket socket, object responseData, string method, ILogger logger, Guid? userId = null)
        {
            var responseMessage = WSRequestModel.SendNotification(responseData, method).GenaretJson();
            await SendMessageAsync(socket, responseMessage: responseMessage, method: method, userId: userId, logger: logger);
        }

        /// <summary>
        /// The method to send response message to the client
        /// </summary>
        /// <param name="userId">User's Id to send response message</param>
        /// <param name="responseData">The response data to send message</param>
        /// <param name="method">Method name to natify the client</param>
        public async Task SendNotificationAsync(Guid userId, object responseData, string method)
        {
            var responseMessage = WSRequestModel.SendNotification(responseData, method).GenaretJson();
            await SendMessageAsync(WebSocketManager.GetWebSocket(userId), responseMessage: responseMessage, method: method, userId: userId, logger: _logger);
        }

        /// <summary>
        /// The method to send response message to the client
        /// </summary>
        /// <param name="userIds">List of User's Id to send response message</param>
        /// <param name="responseData">The response data to send message</param>
        /// <param name="method">Method name to natify the client</param>
        public async Task SendNotificationAsync(IEnumerable<Guid> userIds, object responseData, string method)
        {
            var responseMessage = WSRequestModel.SendNotification(responseData, method).GenaretJson();
            foreach (var socketId in userIds)
                await SendMessageAsync(WebSocketManager.GetWebSocket(socketId), responseMessage: responseMessage, method: method, userId: socketId, logger: _logger);
        }

        /// <summary>
        /// The main method to receive requested message from the client
        /// </summary>
        /// <param name="socket">WebSocket which receive message</param>
        /// <param name="receiveMessageData">Receive message data from user</param>
        public async Task ReceiveMessageAsync(System.Net.WebSockets.WebSocket socket, string receiveMessageData)
        {
            WSRequestModel responseModel = null;
            string requestId = string.Empty;
            string requestMethod = string.Empty;
            Guid? userId = null;
            try
            {
                var requestModel = JsonConvert.DeserializeObject<WSRequestModel>(receiveMessageData);
                var userInfo = WebSocketManager.GetInfo(socket);
                userId = userInfo?.UserId;
                if (WebSocketManager.LoggAllWSRequest)
                    _logger.LogInformation($"User id: {userId}, Method: {requestModel.Method}, Request data: {receiveMessageData}");
                requestId = requestModel.Id;
                requestMethod = requestModel.Method;
                if (userInfo == null || userId == Guid.Empty)
                {
                    responseModel = await WSRequestModel.NotAccessAsync(errorId: 105);
                }
                else
                {
                    if (string.IsNullOrEmpty(requestId) || string.IsNullOrEmpty(requestMethod))
                    {
                        responseModel = await WSRequestModel.NotAccessAsync(errorId: 102);
                    }
                    else
                    {
                        var methodLevels = requestMethod.Split('.');
                        if (methodLevels.Length == 2)
                        {
                            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
                            Type callingController = Type.GetType($"Hubs.{methodLevels.First()}Controller, {assemblyName}");
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
                                    var callingClass = Activator.CreateInstance(callingController, new object[] { this, userInfo });
                                    responseModel = await (Task<WSRequestModel>)wsHubMethod.Invoke(callingClass, methodParams);
                                }
                                else
                                {
                                    responseModel = await WSRequestModel.ErrorRequestAsync($"{requestMethod} method's parameters is invalid", 106);
                                }
                            }
                            else
                            {
                                responseModel = await WSRequestModel.ErrorRequestAsync($"{requestMethod} method's class is invalid", 104);
                            }
                        }
                        else
                        {
                            responseModel = await WSRequestModel.ErrorRequestAsync("Websocket request will support only two levels methods", 103);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Websocket request is invalid");
                responseModel = await WSRequestModel.ErrorRequestAsync("Websocket request is invalid", 101);
            }

            if (responseModel != null)
            {
                var response = new WSRequestModel()
                {
                    Id = requestId,
                    Method = requestMethod,
                    ErrorId = responseModel.ErrorId,
                    Error = responseModel.Error,
                    Result = responseModel.Result
                };
                await SendMessageAsync(socket, response.GenaretJson(), logger: _logger, method: response.Method, userId: userId);
            }
        }
    }
}
