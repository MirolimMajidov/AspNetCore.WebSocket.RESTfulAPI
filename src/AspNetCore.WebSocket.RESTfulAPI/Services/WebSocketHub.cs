using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Helpers;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using Microsoft.Extensions.Logging;

namespace AspNetCore.WebSocket.RESTfulAPI.Services;

public class WebSocketHub(IWebSocketManager manager, ILogger<WebSocketHub> logger) : Disposable
{
    private static readonly ConditionalWeakTable<System.Net.WebSockets.WebSocket, SemaphoreSlim> SendLocks = new();
    private IReadOnlyDictionary<string, WsController> _controllersByName;
    public IWebSocketManager Manager { get; } = manager;

    /// <summary>
    /// To connect new created WebSocket to the WebSocketManager's sockets list
    /// </summary>
    /// <param name="socket">New created socket</param>
    /// <param name="userInfo">Current User info</param>
    public virtual async Task OnConnectedAsync(System.Net.WebSockets.WebSocket socket, WsUserInfo userInfo)
    {
        await Manager.AddSocket(userInfo.Id, socket, userInfo);

        await SendNotificationAsync(userInfo.Id, new { }, Notification.WSConnected);
    }

    /// <summary>
    /// To disconnect exists WebSocket from the WebSocketManager's sockets list
    /// </summary>
    /// <param name="socketId">The id of current WebSocket to disconnect</param>
    public virtual async Task OnDisconnectedAsync(Guid? socketId)
    {
        if (socketId is null) return;

        await Manager.RemoveSocket(socketId.Value, closeDescription: "The connection closed by the client",
            notifyClient: false);
    }

    /// <summary>
    /// The main method to send response message to the client
    /// </summary>
    /// <param name="socket">WebSocket to send response message</param>
    /// <param name="responseMessage">The response data to send message</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="method">Method name to notify the client</param>
    /// <param name="userId">User's id to send response message</param>
    public static async Task SendMessageAsync(System.Net.WebSockets.WebSocket socket, string responseMessage,
        ILogger logger, string method = "", Guid? userId = null)
    {
        if (socket?.State != WebSocketState.Open || responseMessage == null)
            return;

        var sendLock = SendLocks.GetValue(socket, static _ => new SemaphoreSlim(1, 1));
        await sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                if (WebSocketManager.LogAllWsRequest && logger != null)
                    logger.LogInformation($"User id: {userId}, Method: {method}, Response data: {responseMessage}");

                var bufferContent = Encoding.UTF8.GetBytes(responseMessage);
                await socket.SendAsync(bufferContent, WebSocketMessageType.Binary, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            sendLock.Release();
        }
    }

    /// <summary>
    /// The method to send response message to the client
    /// </summary>
    /// <param name="socket">WebSocket to send response message</param>
    /// <param name="responseData">The response data to send message</param>
    /// <param name="method">Method name to notify the client</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="userId">User's id to send response message</param>
    public static async Task SendNotificationAsync(System.Net.WebSockets.WebSocket socket, object responseData,
        string method, ILogger logger, Guid? userId = null)
    {
        var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson(logger);
        await SendMessageAsync(socket, responseMessage: responseMessage, method: method, userId: userId,
            logger: logger);
    }

    /// <summary>
    /// The method to send response message to the client
    /// </summary>
    /// <param name="userId">User's id to send response message</param>
    /// <param name="responseData">The response data to send message</param>
    /// <param name="method">Method name to notify the client</param>
    public async Task SendNotificationAsync(Guid userId, object responseData, string method)
    {
        var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson(logger);
        await SendMessageAsync(Manager.GetWebSocket(userId), responseMessage: responseMessage, method: method,
            userId: userId, logger: logger);
    }

    /// <summary>
    /// The method to send response message to the client
    /// </summary>
    /// <param name="userIds">List of User's id to send response message</param>
    /// <param name="responseData">The response data to send message</param>
    /// <param name="method">Method name to notify the client</param>
    public async Task SendNotificationAsync(IEnumerable<Guid> userIds, object responseData, string method)
    {
        var responseMessage = NotificationResponseModel.SendNotification(responseData, method).GenerateJson(logger);
        await Task.WhenAll(userIds.Select(socketId =>
            SendMessageAsync(Manager.GetWebSocket(socketId), responseMessage, logger, method, socketId)));
    }

    /// <summary>
    /// The main method to receive requested message from the client
    /// </summary>
    /// <param name="socket">WebSocket which receive message</param>
    /// <param name="receiveMessageData">Receive message data from user</param>
    public async Task ReceiveMessageAsync(System.Net.WebSockets.WebSocket socket, string receiveMessageData)
    {
        ResponseModel responseModel;
        string requestId = string.Empty;
        string requestMethod = string.Empty;
        Guid? userId = null;
        try
        {
            var requestModel = JsonSerializer.Deserialize<NotificationResponseModel>(receiveMessageData,
                ResponseModel.JsonOptions) ?? throw new JsonException("The WebSocket request body is empty.");
            var userInfo = Manager.GetUserInfo(socket);
            userId = userInfo?.Id;

            if (WebSocketManager.LogAllWsRequest)
                logger.LogInformation(
                    $"User id: {userId}, Method: {requestModel.Method}, Request data: {receiveMessageData}");

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
                    var separatorIndex = requestMethod.IndexOf('.');
                    if (separatorIndex > 0 && separatorIndex == requestMethod.LastIndexOf('.'))
                    {
                        _controllersByName ??= Manager.Controllers.ToDictionary(c => c.Name, StringComparer.Ordinal);
                        _controllersByName.TryGetValue(requestMethod[..separatorIndex], out var controller);
                        if (controller != null)
                        {
                            var wsHubMethod = controller.Methods.FirstOrDefault(m => m.Name == requestModel.Method);
                            if (wsHubMethod != null)
                            {
                                var methodParams = new object[wsHubMethod.Parameters.Count];
                                for (var index = 0; index < wsHubMethod.Parameters.Count; index++)
                                {
                                    var parameter = wsHubMethod.Parameters[index];
                                    methodParams[index] = requestModel.Params != null &&
                                                          requestModel.Params.TryGetValue(parameter.Name!, out var value)
                                        ? value.ConvertTo(parameter.ParameterType)
                                        : parameter.DefaultValue;
                                }

                                var callingClass =
                                    Activator.CreateInstance(controller.Controller, this, userInfo, logger);
                                responseModel =
                                    await ((Task<ResponseModel>)wsHubMethod.Method.Invoke(callingClass, methodParams))!;
                            }
                            else
                            {
                                responseModel =
                                    await ResponseModel.ErrorRequestAsync(
                                        $"{requestMethod} method's parameters is invalid", 106);
                            }
                        }
                        else
                        {
                            responseModel = await ResponseModel.ErrorRequestAsync(
                                $"{requestMethod} controller is invalid", 104);
                        }
                    }
                    else
                    {
                        responseModel =
                            await ResponseModel.ErrorRequestAsync(
                                "Websocket request will support only two levels methods", 103);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Websocket request is invalid");
            responseModel = await ResponseModel.ErrorRequestAsync("Websocket request is invalid", 101);
        }

        if (responseModel != null)
        {
            var response = new NotificationResponseModel
            {
                Id = requestId,
                Method = requestMethod,
                ErrorId = responseModel.ErrorId,
                Error = responseModel.Error,
                Result = responseModel.Result
            };
            await SendMessageAsync(socket, response.GenerateJson(logger), logger: logger, method: response.Method,
                userId: userId);
        }
    }
}
