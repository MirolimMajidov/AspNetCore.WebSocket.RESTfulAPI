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
    private IReadOnlyDictionary<string, (WsController Controller, WsMethod Method)> _endpointsByName;
    public IWebSocketManager Manager { get; } = manager;

    internal void InitializeEndpoints()
    {
        _endpointsByName = Manager.Controllers
            .SelectMany(controller => controller.Methods.Select(method =>
                (method.Name, Controller: controller, Method: method)))
            .ToDictionary(endpoint => endpoint.Name,
                endpoint => (endpoint.Controller, endpoint.Method), StringComparer.Ordinal);
    }

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
        if (responseMessage == null)
            return;

        await SendMessageBytesAsync(socket, Encoding.UTF8.GetBytes(responseMessage), logger, method, userId,
            responseMessage);
    }

    private static async Task SendMessageBytesAsync(System.Net.WebSockets.WebSocket socket,
        ReadOnlyMemory<byte> responseMessage, ILogger logger, string method = "", Guid? userId = null,
        string responseForLogging = null)
    {
        if (socket?.State != WebSocketState.Open || responseMessage.IsEmpty)
            return;

        var sendLock = SendLocks.GetValue(socket, static _ => new SemaphoreSlim(1, 1));
        await sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                if (WebSocketManager.LogAllWsRequest && logger != null)
                    logger.LogInformation("User id: {UserId}, Method: {Method}, Response data: {ResponseData}",
                        userId, method, responseForLogging ?? Encoding.UTF8.GetString(responseMessage.Span));

                await socket.SendAsync(responseMessage, WebSocketMessageType.Binary, true, CancellationToken.None)
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
        var responseMessage = ResponseModel.GenerateJsonBytes(
            NotificationResponseModel.SendNotification(responseData, method), logger);
        await SendMessageBytesAsync(socket, responseMessage, logger, method, userId);
    }

    /// <summary>
    /// The method to send response message to the client
    /// </summary>
    /// <param name="userId">User's id to send response message</param>
    /// <param name="responseData">The response data to send message</param>
    /// <param name="method">Method name to notify the client</param>
    public async Task SendNotificationAsync(Guid userId, object responseData, string method)
    {
        var responseMessage = ResponseModel.GenerateJsonBytes(
            NotificationResponseModel.SendNotification(responseData, method), logger);
        await SendMessageBytesAsync(Manager.GetWebSocket(userId), responseMessage, logger, method, userId);
    }

    /// <summary>
    /// The method to send response message to the client
    /// </summary>
    /// <param name="userIds">List of User's id to send response message</param>
    /// <param name="responseData">The response data to send message</param>
    /// <param name="method">Method name to notify the client</param>
    public async Task SendNotificationAsync(IEnumerable<Guid> userIds, object responseData, string method)
    {
        var responseMessage = ResponseModel.GenerateJsonBytes(
            NotificationResponseModel.SendNotification(responseData, method), logger);
        await Task.WhenAll(userIds.Select(socketId =>
            SendMessageBytesAsync(Manager.GetWebSocket(socketId), responseMessage, logger, method, socketId)));
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
                    "The WebSocket connection is not registered or is no longer authenticated.",
                    WebSocketErrorCodes.ConnectionNotRegistered);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(requestId))
                {
                    responseModel = ResponseModel.ErrorRequest(
                        "The required request property 'id' is missing or empty.",
                        WebSocketErrorCodes.MissingRequestProperty);
                }
                else if (string.IsNullOrWhiteSpace(requestMethod))
                {
                    responseModel = ResponseModel.ErrorRequest(
                        "The required request property 'method' is missing or empty.",
                        WebSocketErrorCodes.MissingRequestProperty);
                }
                else
                {
                    var separatorIndex = requestMethod.IndexOf('.');
                    if (separatorIndex > 0 && separatorIndex == requestMethod.LastIndexOf('.'))
                    {
                        if (_endpointsByName.TryGetValue(requestMethod, out var endpoint))
                        {
                            var methodParams = new object[endpoint.Method.ParameterNames.Length];
                            for (var index = 0; index < endpoint.Method.ParameterNames.Length; index++)
                            {
                                parameterName = endpoint.Method.ParameterNames[index];
                                if (requestModel.Params != null &&
                                    requestModel.Params.TryGetValue(parameterName, out var value))
                                {
                                    methodParams[index] = value.ConvertTo(endpoint.Method.ParameterTypes[index]);
                                }
                                else if (endpoint.Method.HasDefaultValues[index])
                                {
                                    methodParams[index] = endpoint.Method.DefaultValues[index];
                                }
                                else
                                {
                                    throw new ArgumentException(
                                        $"Required parameter '{parameterName}' is missing.", parameterName);
                                }
                            }

                            var callingClass = endpoint.Controller.Factory(this, userInfo, logger);
                            using var controllerLifetime = callingClass as IDisposable;
                            executingMethod = true;
                            responseModel = await endpoint.Method.InvokeAsync(callingClass, methodParams);
                            if (responseModel == null)
                                throw new InvalidOperationException(
                                    $"Method '{requestMethod}' returned a null response.");
                        }
                        else
                        {
                            responseModel = ResponseModel.ErrorRequest(
                                $"Method '{requestMethod}' was not found.", WebSocketErrorCodes.MethodNotFound);
                        }
                    }
                    else
                    {
                        responseModel = ResponseModel.ErrorRequest(
                            $"Method '{requestMethod}' has an invalid format. Expected 'Controller.Method'.",
                            WebSocketErrorCodes.InvalidMethodFormat);
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
                    "The request body contains invalid JSON. Check its syntax and property types.",
                    WebSocketErrorCodes.InvalidJson);
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
                    $"Parameter '{parameterName}' has an invalid value or type.",
                    WebSocketErrorCodes.InvalidParameters);
            }
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid parameters for WebSocket method {Method}", requestMethod);
            responseModel = ResponseModel.ErrorRequest(
                $"Parameters for method '{requestMethod}' are missing or invalid. Check parameter '{parameterName}'.",
                WebSocketErrorCodes.InvalidParameters);
        }
        catch (MissingMethodException ex)
        {
            logger.LogError(ex, "WebSocket controller constructor was not found for method {Method}", requestMethod);
            responseModel = ResponseModel.ErrorRequest(
                $"Method '{requestMethod}' is currently unavailable because of a server configuration error.",
                WebSocketErrorCodes.ControllerActivationFailed);
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
                $"Method '{requestMethod}' is currently unavailable because of a server configuration error.",
                WebSocketErrorCodes.InvalidEndpointConfiguration);
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
            await SendMessageBytesAsync(socket, ResponseModel.GenerateJsonBytes(response, logger), logger,
                response.Method, userId);
        }
    }

    private static ResponseModel CreateSafeServerError(string requestMethod)
    {
        return ResponseModel.ErrorRequest(
            $"Method '{requestMethod}' could not be completed because of an internal server error.",
            WebSocketErrorCodes.InternalServerError);
    }
}
