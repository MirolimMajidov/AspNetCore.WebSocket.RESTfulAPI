using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Reflection;
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
        string parameterName = null;
        bool executingMethod = false;
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
                responseModel = ResponseModel.ErrorRequest(
                    "The WebSocket connection is not registered or is no longer authenticated.", 105);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(requestId))
                {
                    responseModel = ResponseModel.ErrorRequest(
                        "The required request property 'id' is missing or empty.", 102);
                }
                else if (string.IsNullOrWhiteSpace(requestMethod))
                {
                    responseModel = ResponseModel.ErrorRequest(
                        "The required request property 'method' is missing or empty.", 102);
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
                                    parameterName = parameter.Name;
                                    if (requestModel.Params != null &&
                                        requestModel.Params.TryGetValue(parameter.Name!, out var value))
                                    {
                                        methodParams[index] = value.ConvertTo(parameter.ParameterType);
                                    }
                                    else if (parameter.HasDefaultValue)
                                    {
                                        methodParams[index] = parameter.DefaultValue;
                                    }
                                    else
                                    {
                                        throw new ArgumentException(
                                            $"Required parameter '{parameter.Name}' is missing.", parameter.Name);
                                    }
                                }

                                var callingClass =
                                    Activator.CreateInstance(controller.Controller, this, userInfo, logger)
                                    ?? throw new InvalidOperationException(
                                        $"Controller '{controller.Controller.Name}' could not be created.");
                                executingMethod = true;
                                var invocationResult = wsHubMethod.Method.Invoke(callingClass, methodParams);
                                if (invocationResult is not Task<ResponseModel> responseTask)
                                    throw new InvalidOperationException(
                                        $"Method '{requestMethod}' must return Task<ResponseModel>.");

                                responseModel = await responseTask;
                                if (responseModel == null)
                                    throw new InvalidOperationException(
                                        $"Method '{requestMethod}' returned a null response.");
                            }
                            else
                            {
                                responseModel = ResponseModel.ErrorRequest(
                                    $"Method '{requestMethod}' was not found. Check the method name and its WsHubMethodName attribute.",
                                    107);
                            }
                        }
                        else
                        {
                            responseModel = ResponseModel.ErrorRequest(
                                $"Controller '{requestMethod[..separatorIndex]}' was not found for method '{requestMethod}'.",
                                104);
                        }
                    }
                    else
                    {
                        responseModel = ResponseModel.ErrorRequest(
                            $"Method '{requestMethod}' has an invalid format. Expected 'Controller.Method'.", 103);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            if (executingMethod)
            {
                logger.LogError(ex, "WebSocket method {Method} failed", requestMethod);
                responseModel = CreateSafeServerError(requestMethod);
            }
            else
            {
                logger.LogWarning(ex, "Invalid WebSocket JSON request for method {Method}", requestMethod);
                responseModel = ResponseModel.ErrorRequest(
                    "The request body contains invalid JSON. Check its syntax and property types.", 101);
            }
        }
        catch (InvalidCastException ex)
        {
            if (executingMethod)
            {
                logger.LogError(ex, "WebSocket method {Method} failed", requestMethod);
                responseModel = CreateSafeServerError(requestMethod);
            }
            else
            {
                logger.LogWarning(ex, "Invalid parameter {Parameter} for WebSocket method {Method}", parameterName,
                    requestMethod);
                responseModel = ResponseModel.ErrorRequest(
                    $"Parameter '{parameterName}' has an invalid value or type.", 106);
            }
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid parameters for WebSocket method {Method}", requestMethod);
            responseModel = ResponseModel.ErrorRequest(
                $"Parameters for method '{requestMethod}' are missing or invalid. Check parameter '{parameterName}'.",
                106);
        }
        catch (MissingMethodException ex)
        {
            logger.LogError(ex, "WebSocket controller constructor was not found for method {Method}", requestMethod);
            responseModel = ResponseModel.ErrorRequest(
                $"Method '{requestMethod}' is currently unavailable because of a server configuration error.", 108);
        }
        catch (TargetInvocationException ex)
        {
            var actualException = ex.InnerException ?? ex;
            logger.LogError(actualException, "WebSocket method {Method} failed", requestMethod);
            responseModel = CreateSafeServerError(requestMethod);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid WebSocket method configuration for {Method}", requestMethod);
            responseModel = ResponseModel.ErrorRequest(
                $"Method '{requestMethod}' is currently unavailable because of a server configuration error.", 109);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error while executing WebSocket method {Method}", requestMethod);
            responseModel = CreateSafeServerError(requestMethod);
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

    private static ResponseModel CreateSafeServerError(string requestMethod)
    {
        return ResponseModel.ErrorRequest(
            $"Method '{requestMethod}' could not be completed because of an internal server error.", 110);
    }
}
