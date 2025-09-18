using System;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Configurations;
using AspNetCore.WebSocket.RESTfulAPI.Helpers;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using AspNetCore.WebSocket.RESTfulAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSocketManager = AspNetCore.WebSocket.RESTfulAPI.Services.WebSocketManager;

namespace AspNetCore.WebSocket.RESTfulAPI.Middlewares;

public class WebSocketRESTfulMiddleware
{
    private WebSocketHub WebSocketHub { get; }
    private ILogger Logger { get;}

    public WebSocketRESTfulMiddleware(RequestDelegate next, WebSocketHub webSocketHub, ILogger<WebSocketRESTfulMiddleware> logger)
    {
        WebSocketHub = webSocketHub;
        Logger = logger;
    }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var userId = context.Request.GetHeaderValue("UserId");
            var userName = context.Request.GetHeaderValue("UserName");
            ConnectionAborted abortStatus = ConnectionAborted.None;

            if (!Guid.TryParse(userId, out var userIdAsGuid))
                abortStatus = ConnectionAborted.UserIdNotFound;
            else if (string.IsNullOrEmpty(userName))
                abortStatus = ConnectionAborted.UserNameNotFound;

            var info = new WsUserInfo
            {
                Id = userIdAsGuid,
                Name = userName
            };

            await InvokeWsAsync(context, info, abortStatus);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error on opening connection of WebSocket to the server");
        }
    }

    protected async Task InvokeWsAsync(HttpContext context, WsUserInfo userInfo, ConnectionAborted abortStatus = ConnectionAborted.None)
    {
        try
        {
            await WebSocketHub.Manager.RemoveWebSocketBeforeReconnect(userInfo.Id);
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            if (abortStatus == ConnectionAborted.None)
            {
                await WebSocketHub.OnConnectedAsync(socket, userInfo);

                await Receive(socket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Binary)
                        await WebSocketHub.ReceiveMessageAsync(socket, Encoding.UTF8.GetString(buffer, 0, result.Count));
                    else
                        await WebSocketHub.OnDisconnectedAsync(userInfo.Id);
                });
            }
            else
                await WebSocketHub.Manager.AbortConnection(socket, abortStatus);
        }
        catch
        {
            throw;
        }
    }

    private async Task Receive(System.Net.WebSockets.WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
        try
        {
            var buffer = new byte[WebSocketManager.WebSocketBufferSize];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
                handleMessage(result, buffer);
            }
        }
        catch
        {
            var userId = WebSocketHub.Manager?.GetId(socket);
            await WebSocketHub.OnDisconnectedAsync(userId);
        }
    }
}

public static class WebSocketHubExtensions
{
    /// <summary>
    /// For mapping path to WebSocketHub and set some Web Socket configurations
    /// </summary>
    /// <param name="path">Path to bind Web socket</param>
    /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
    /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 seconds</param>
    /// <param name="logAllWebSocketRequestAndResponse">When you turn on it, all request and response data of web sockets will be logged to log files. By default, it's false because it can be effected to performance</param>
    public static IApplicationBuilder MapWebSocket<TMiddleware, TWebSocketHub>(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool logAllWebSocketRequestAndResponse = false) where TMiddleware : WebSocketRESTfulMiddleware where TWebSocketHub : WebSocketHub
    {
        WebSocketManager.LogAllWsRequest = logAllWebSocketRequestAndResponse;
        WebSocketManager.WebSocketBufferSize = 1024 * receiveBufferSize;
        builder.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval) });

        var serviceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
        var wsHub = serviceProvider.GetService<TWebSocketHub>();

        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly!.GetName().Name;
        var controllers = assembly.ExportedTypes.Where(c => c.FullName?.StartsWith($"{assemblyName}.Hubs.") == true && c.FullName.EndsWith("Controller")).ToList();
        foreach (var controller in controllers)
            CreateController(controller);

        return builder.Map(path, (app) => app.UseMiddleware<TMiddleware>(serviceProvider.GetService<TWebSocketHub>()));

        void CreateController(Type controller)
        {
            var wsController = new WsController { Name = controller.Name[..^10], Controller = controller };
            var methods = controller.GetMethods().Where(m => m.GetWsHubAttribute() != null);
            foreach (var method in methods)
                CreateMethod(wsController, method);

            wsHub.Manager.Controllers.Add(wsController);
        }

        void CreateMethod(WsController controller, MethodInfo method)
        {
            var wsMethod = new WsMethod { Name = method.GetWsHubAttribute().Name, Method = method };
            foreach (var parameter in method.GetParameters())
                wsMethod.Parameters.Add(parameter);

            controller.Methods.Add(wsMethod);
        }
    }

    /// <summary>
    /// For mapping path to WebSocket RESTful API and set some Web Socket configurations
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">Path to bind Web socket</param>
    /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
    /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 seconds</param>
    /// <param name="logAllWebSocketRequestAndResponse">When you turn on it, all request and response data of web sockets will be logged to log files. By default, it's false because it can be effected to performance</param>
    public static IApplicationBuilder WebSocketRESTfulAPI(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool logAllWebSocketRequestAndResponse = false)
    {
        return builder.MapWebSocket<WebSocketRESTfulMiddleware, WebSocketHub>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize, logAllWebSocketRequestAndResponse: logAllWebSocketRequestAndResponse);
    }
}