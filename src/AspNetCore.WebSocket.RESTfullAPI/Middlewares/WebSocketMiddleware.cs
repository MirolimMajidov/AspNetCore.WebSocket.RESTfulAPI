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
    public class WebSocketMiddleware
    {
        private WebSocketHub WebSocketHub { get; set; }

        public WebSocketMiddleware(RequestDelegate next, WebSocketHub webSocketHub)
        {
            WebSocketHub = webSocketHub;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                    return;

                var userId = context.Request.GetHeaderValue("UserId");
                ConnectionAborted abortStatus = userId.IsNullOrEmpty()? ConnectionAborted.UserIdNotFound : ConnectionAborted.None;

                var userName = context.Request.GetHeaderValue("UserName");
                if (userName.IsNullOrEmpty() && abortStatus == ConnectionAborted.None)
                    abortStatus = ConnectionAborted.UserNameNotFound;

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
            { }
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
        /// For mapping path to WebSocketHub and set some Web Socket comf=figuration
        /// </summary>
        /// <param name="path"></param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <returns></returns>
        public static IApplicationBuilder MapWebSocket(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60)
        {
            var ass = System.Reflection.Assembly.GetCallingAssembly();
            Services.WebSocketManager.WebSocketBufferSize = 1024 * receiveBufferSize;
            Services.WebSocketManager.CurrentAssemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            builder.UseWebSockets(new WebSocketOptions() { KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval) });

            var serviceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;

            return builder.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(serviceProvider.GetService<WebSocketHub>()));
        }
    }
}
