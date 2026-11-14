using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using Microsoft.Extensions.Logging;

namespace AspNetCore.WebSocket.RESTfulAPI.Services;
using System.Net.WebSockets;

public class WebSocketManager(ILogger<WebSocketManager> logger) : Disposable, IWebSocketManager
{
    private readonly ConcurrentDictionary<Guid, (WebSocket WS, WsUserInfo Info)> _sockets = new();
    private readonly ConcurrentDictionary<WebSocket, Guid> _socketIds = new();

    public static bool LogAllWsRequest { get; set; }
    public static int WebSocketBufferSize { get; set; }
    public List<WsController> Controllers { get; set; } = new();

    private readonly ILogger _logger = logger;

    public WebSocket GetWebSocket(Guid socketId)
    {
        return _sockets.TryGetValue(socketId, out var socketInfo) ? socketInfo.WS : null;
    }

    public WsUserInfo GetUserInfo(WebSocket socket)
    {
        return _socketIds.TryGetValue(socket, out var socketId) &&
               _sockets.TryGetValue(socketId, out var socketInfo)
            ? socketInfo.Info
            : null;
    }

    public ConcurrentDictionary<Guid, (WebSocket WS, WsUserInfo Info)> Clients()
    {
        return _sockets;
    }

    public IEnumerable<WsUserInfo> UsersInfo()
    {
        return _sockets.Select(s => s.Value.Info);
    }

    public IEnumerable<WebSocket> WebSockets()
    {
        return _sockets.Select(s => s.Value.WS);
    }

    public Guid GetId(WebSocket socket)
    {
        return _socketIds.TryGetValue(socket, out var socketId) ? socketId : Guid.Empty;
    }

    public Task AddSocket(Guid socketId, WebSocket socket, WsUserInfo info)
    {
        if (_sockets.TryAdd(socketId, (socket, info)))
            _socketIds.TryAdd(socket, socketId);

        return Task.CompletedTask;
    }

    public void ClearAllClients()
    {
        _ = ClearAllClientsAsync();
    }

    public async Task RemoveWebSocketBeforeReconnect(Guid socketId)
    {
        await RemoveSocket(socketId, "WebSocket reconnecting", notifyClient: false);
    }

    public async Task AbortConnection(WebSocket socket, ConnectionAborted abortStatus = ConnectionAborted.TokenExpiredOrInvalid)
    {
        try
        {
            await WebSocketHub.SendNotificationAsync(socket, new { status = abortStatus }, logger: _logger, method: Notification.ConnectionAborted);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "The connection closed by the server", CancellationToken.None);
        }
        catch
        {
            // ignored
        }
    }

    public async Task RemoveSocket(Guid socketId, string closeDescription = "The connection closed by the server", string becauseClause = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, bool notifyClient = true)
    {
        if (_sockets.TryRemove(socketId, out var socketInfo))
        {
            _socketIds.TryRemove(socketInfo.WS, out _);
            try
            {
                var description = becauseClause == null ? closeDescription : $"{closeDescription} because {becauseClause}";

                if (LogAllWsRequest && _logger != null)
                    _logger.LogInformation($"{description}, User Id: {socketId}, Connection state: {socketInfo.WS?.State}");

                if (socketInfo.WS?.State == WebSocketState.Open)
                {
                    if (notifyClient)
                        await WebSocketHub.SendNotificationAsync(socketInfo.WS, new { description }, method: Notification.UserUnAuth, logger: _logger);

                    await socketInfo.WS.CloseOutputAsync(closeStatus, description, CancellationToken.None);
                }
                socketInfo.WS?.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                socketInfo.Info?.Dispose();
            }
        }
    }

    private async Task ClearAllClientsAsync()
    {
        await Task.WhenAll(_sockets.Keys.Select(socketId => RemoveSocket(socketId)));
    }
}
