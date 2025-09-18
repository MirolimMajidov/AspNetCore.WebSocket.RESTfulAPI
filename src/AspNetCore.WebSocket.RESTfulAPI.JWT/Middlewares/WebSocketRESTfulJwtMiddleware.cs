using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.JWT.Services;
using AspNetCore.WebSocket.RESTfulAPI.Middlewares;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.Middlewares;

public class WebSocketRESTfulJwtMiddleware(
    RequestDelegate next,
    WsHub webSocketHub,
    ILogger<WebSocketRESTfulJwtMiddleware>
        logger)
    : WebSocketRESTfulMiddleware(next, webSocketHub, logger)
{
    public override async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var authResult = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            var isAuthenticated = authResult.Succeeded && authResult.Principal.Identity?.IsAuthenticated == true;
            var userId = isAuthenticated ? authResult.Principal.Claims.SingleOrDefault(c => c.Type == "UserId")?.Value : null;
            var userName = isAuthenticated ? authResult.Principal.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Name)?.Value : string.Empty;
            ConnectionAborted abortStatus = ConnectionAborted.None;

            Guid userIdAsGuid = Guid.Empty;
            if (!isAuthenticated)
                abortStatus = ConnectionAborted.TokenExpiredOrInvalid;
            else if (!Guid.TryParse(userId, out userIdAsGuid))
                abortStatus = ConnectionAborted.UserIdNotFound;
            else if (string.IsNullOrEmpty(userName))
                abortStatus = ConnectionAborted.UserNameNotFound;

            var info = new WsUserInfo
            {
                Id = userIdAsGuid,
                Name = userName
            };

            //Here you are able to pass your own subclass instead of WSUserInfo. Note: If you bind your own subclass instead of WSUserInfo, all your Web Socket controllers should be used your passed subclass
            await InvokeWsAsync(context, info, abortStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error on opening connection of WebSocket to the server");
        }
    }
}

public static class WebSocketJwtExtensions
{
    /// <summary>
    /// For mapping path to WebSocket RESTful API and set some Web Socket configurations
    /// </summary>
    /// <param name="builder">The IApplicationBuilder instance this method extends</param>
    /// <param name="path">Path to bind Web socket</param>
    /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
    /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 seconds</param>
    /// <param name="logAllWebSocketRequestAndResponse">When you turn on it all request and response data of web sockets will be logged. By default, it's false because it can be effected to performance</param>
    public static IApplicationBuilder WebSocketRESTfulApiJwt(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool logAllWebSocketRequestAndResponse = false)
    {
        //Here you are able to bind your own subclasses instead of WebSocketRestfulMiddleware and WebSocketHub generic types. Note: If you bind your own subclass instead of WebSocketHub, all your Web Socket controllers should be used your subclass
        return builder.MapWebSocket<WebSocketRESTfulJwtMiddleware, WsHub>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize, logAllWebSocketRequestAndResponse: logAllWebSocketRequestAndResponse);
    }
}