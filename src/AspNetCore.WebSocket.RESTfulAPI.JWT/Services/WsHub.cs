using System;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using AspNetCore.WebSocket.RESTfulAPI.Services;
using Microsoft.Extensions.Logging;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.Services;

using System.Net.WebSockets;

public class WsHub(IWebSocketManager manager, ILogger<WsHub> logger) : WebSocketHub(manager, logger)
{
    /// <summary>
    /// To connect new created WebSocket to the WebSocketManager's sockets list
    /// </summary>
    /// <param name="socket">New created socket</param>
    /// <param name="userInfo">Current User info</param>
    public override async Task OnConnectedAsync(WebSocket socket, WsUserInfo userInfo)
    {
        await base.OnConnectedAsync(socket, userInfo);
    }

    /// <summary>
    /// To disconnect exists WebSocket from the WebSocketManager's sockets list
    /// </summary>
    /// <param name="socketId">The id of current WebSocket to disconnect</param>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Guid? socketId)
    {
        await base.OnDisconnectedAsync(socketId);
    }
}