using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public class WebSocketRestfullMiddleware
    {
        protected WebSocketHub WebSocketHub { get; set; }
        protected ILogger Logger { get; set; }

        public WebSocketRestfullMiddleware(RequestDelegate next, WebSocketHub webSocketHub, ILogger<WebSocketRestfullMiddleware> logger)
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

                if (string.IsNullOrEmpty(userId.ToString()))
                    abortStatus = ConnectionAborted.UserIdNotFound;
                else if (string.IsNullOrEmpty(userName))
                    abortStatus = ConnectionAborted.UserNameNotFound;

                var info = new WSUserInfo()
                {
                    Id = userId,
                    Name = userName
                };

                await InvokeWSAsync(context, info, abortStatus);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error on opening connection of WebSocket to the server");
            }
        }

        protected async Task InvokeWSAsync(HttpContext context, WSUserInfo userInfo, ConnectionAborted abortStatus = ConnectionAborted.None)
        {
            try
            {
                await WebSocketHub.WSManager.RemoveWebSocketIfExists(userInfo.Id);
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
                        return;
                    });
                }
                else
                    await WebSocketHub.WSManager.AbortConnection(socket, abortStatus);
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
                await WebSocketHub.OnDisconnectedAsync(WebSocketHub.WSManager.GetId(socket));
            }
        }
    }

    public static class WebSocketHubExtensions
    {
        /// <summary>
        /// For mapping path to WebSocketHub and set some Web Socket comfigurations
        /// </summary>
        /// <param name="path">Path to bind Web socket</param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <param name="loggAllWebSocketRequestAndResponse">When you turn on it all request and response data of web socket will be logged to the your configurated file. By default it's false because it can be effect to performance</param>
        public static IApplicationBuilder MapWebSocket<TMiddleware, TWebSocketHub>(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool loggAllWebSocketRequestAndResponse = false) where TMiddleware : WebSocketRestfullMiddleware where TWebSocketHub : WebSocketHub
        {
            WebSocketManager.LoggAllWSRequest = loggAllWebSocketRequestAndResponse;
            WebSocketManager.CurrentAssemblyName = Assembly.GetEntryAssembly().FullName;
            WebSocketManager.WebSocketBufferSize = 1024 * receiveBufferSize;
            builder.UseWebSockets(new WebSocketOptions() { KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval) });

            var serviceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;


            return builder.Map(path, (_app) => _app.UseMiddleware<TMiddleware>(serviceProvider.GetService<TWebSocketHub>()));
        }

        /// <summary>
        /// For mapping path to WebSocket RESTfull API and set some Web Socket comfigurations
        /// </summary>
        /// <param name="path">Path to bind Web socket</param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <param name="loggAllWebSocketRequestAndResponse">When you turn on it all request and response data of web socket will be logged to the your configurated file. By default it's false because it can be effect to performance</param>
        public static IApplicationBuilder WebSocketRESTfullAPI(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool loggAllWebSocketRequestAndResponse = false)
        {
            return builder.MapWebSocket<WebSocketRestfullMiddleware, WebSocketHub>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize, loggAllWebSocketRequestAndResponse: loggAllWebSocketRequestAndResponse);
        }
    }
}
