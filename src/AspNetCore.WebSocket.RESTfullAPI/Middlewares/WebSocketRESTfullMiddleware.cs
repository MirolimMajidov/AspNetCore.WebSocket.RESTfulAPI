using AspNetCore.WebSocket.RESTfullAPI.Models;
using AspNetCore.WebSocket.RESTfullAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Middlewares
{
    public class WebSocketRESTfullMiddleware
    {
        protected WebSocketHub WebSocketHub { get; set; }
        protected ILogger<WebSocketRESTfullMiddleware> _logger { get; set; }

        public WebSocketRESTfullMiddleware(RequestDelegate next, WebSocketHub webSocketHub, ILogger<WebSocketRESTfullMiddleware> logger)
        {
            WebSocketHub = webSocketHub;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                    return;

                var userId = context.Request.GetHeaderValue("UserId");
                var userName = context.Request.GetHeaderValue("UserName");
                ConnectionAborted abortStatus = ConnectionAborted.None;

                if (userId.ToString().IsNullOrEmpty())
                    abortStatus = ConnectionAborted.UserIdNotFound;
                else if (userName.IsNullOrEmpty())
                    abortStatus = ConnectionAborted.UserNameNotFound;

                await InvokeWSAsync(context, userId, userName, abortStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error on opening connection of WebSocket to the server");
            }
        }

        protected async Task InvokeWSAsync(HttpContext context, object userId, string userName, ConnectionAborted abortStatus = ConnectionAborted.None)
        {
            try
            {
                await WebSocketHub.WebSocketManager.RemoveWebSocketIfExists(userId);
                var socket = await context.WebSockets.AcceptWebSocketAsync();

                if (abortStatus == ConnectionAborted.None)
                {
                    await WebSocketHub.OnConnectedAsync(socket, userId, userName);

                    await Receive(socket, async (result, buffer) =>
                    {
                        if (result.MessageType == WebSocketMessageType.Binary)
                            await WebSocketHub.ReceiveMessageAsync(socket, Encoding.UTF8.GetString(buffer, 0, result.Count));
                        else
                            await WebSocketHub.OnDisconnectedAsync(userId);
                        return;
                    });
                }
                else
                    await WebSocketHub.WebSocketManager.AbortConnection(socket, abortStatus);
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
                var buffer = new byte[Services.WebSocketManager.WebSocketBufferSize];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
                    handleMessage(result, buffer);
                }
            }
            catch
            {
                await WebSocketHub.OnDisconnectedAsync(WebSocketHub.WebSocketManager.GetId(socket));
            }
        }
    }

    public static class WebSocketHubExtensions
    {
        /// <summary>
        /// For mapping path to WebSocketHub and set some Web Socket comfigurations
        /// </summary>
        /// <param name="path"></param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <returns></returns>
        public static IApplicationBuilder MapWebSocket<T>(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60) where T : WebSocketRESTfullMiddleware
        {
            Services.WebSocketManager.CurrentAssemblyName = System.Reflection.Assembly.GetEntryAssembly().FullName;
            Services.WebSocketManager.WebSocketBufferSize = 1024 * receiveBufferSize;
            builder.UseWebSockets(new WebSocketOptions() { KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval) });

            var serviceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;

            return builder.Map(path, (_app) => _app.UseMiddleware<T>(serviceProvider.GetService<WebSocketHub>()));
        }

        /// <summary>
        /// For mapping path to WebSocket RESTfull API and set some Web Socket comfigurations
        /// </summary>
        /// <param name="path"></param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <returns></returns>
        public static IApplicationBuilder WebSocketRESTfullAPI(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60)
        {
            return builder.MapWebSocket<WebSocketRESTfullMiddleware>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize);
        }
    }
}
