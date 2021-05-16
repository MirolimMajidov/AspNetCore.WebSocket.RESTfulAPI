using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Middlewares
{
    public class WebSocketRestfullJWTMiddleware : WebSocketRestfullMiddleware
    {
        public WebSocketRestfullJWTMiddleware(RequestDelegate next, WebSocketHub webSocketHub, ILogger<WebSocketRestfullJWTMiddleware> logger) : base(next, webSocketHub, logger)
        {
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                    return;

                var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                var isAuthenticated = authResult.Succeeded && authResult.Principal.Identity.IsAuthenticated;
                object userId = isAuthenticated ? authResult.Principal.GetUserId() : Guid.NewGuid();
                string userName = isAuthenticated ? authResult.Principal.GetUserName() : string.Empty;
                ConnectionAborted abortStatus = ConnectionAborted.None;

                if(!isAuthenticated)
                    abortStatus = ConnectionAborted.TokenExpiredOrInvalid;
                else if (string.IsNullOrEmpty(userId.ToString()))
                    abortStatus = ConnectionAborted.UserIdNotFound;
                else if (string.IsNullOrEmpty(userName))
                    abortStatus = ConnectionAborted.UserNameNotFound;

                var info = new WSUserInfo()
                {
                    Id = userId,
                    Name = userName
                };

                //Here you able to pass your own subclass instead of WSUserInfo. Note: If you bind your own subclass instead of WSUserInfo, all your Web Socket controllers should be use your passed subclass
                await InvokeWSAsync(context, info, abortStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error on opening connection of WebSocket to the server");
            }
        }
    }

    public static class WebSocketJWTExtensions
    {
        /// <summary>
        /// For mapping path to WebSocket RESTfull API and set some Web Socket comfigurations
        /// </summary>
        /// <param name="path"></param>
        /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
        /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds</param>
        /// <returns></returns>
        public static IApplicationBuilder WebSocketRESTfullJWT(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60)
        {
            //Here you able to bind your own subclasses instead of WebSocketRestfullMiddleware and WebSocketHub generic types. Note: If you bind your own subclass instead of WebSocketHub, all your Web Socket controllers should be use your binded subclass
            return builder.MapWebSocket<WebSocketRestfullJWTMiddleware, WebSocketHub>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize);
        }
    }
}
