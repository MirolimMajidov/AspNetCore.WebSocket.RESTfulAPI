using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Models;

namespace AspNetCore.WebSocket.RESTfulAPI.Services;

using System.Net.WebSockets;

public interface IWebSocketManager
{
    public List<WsController> Controllers { get; }

    /// <summary>
    /// Gets Socket from all active user list by ID
    /// </summary>
    /// <param name="socketId">Socket's Id</param>
    /// <returns>Socket</returns>
    public WebSocket GetWebSocket(Guid socketId);

    /// <summary>
    /// Gets Info from all active user list by WebSocket
    /// </summary>
    /// <param name="socket">WebSocket</param>
    /// <returns>Info</returns>
    public WsUserInfo GetUserInfo(WebSocket socket);

    /// <summary>
    /// Gets all active user WebSockets list
    /// </summary>
    /// <returns>List of WebSockets</returns>
    public ConcurrentDictionary<Guid, (WebSocket WS, WsUserInfo Info)> Clients();

    /// <summary>
    /// Gets all active user info of clients
    /// </summary>
    /// <returns>List of Users Info</returns>
    public IEnumerable<WsUserInfo> UsersInfo();

    /// <summary>
    /// Gets all active Web sockets of clients
    /// </summary>
    /// <returns>List of WebSockets</returns>
    public IEnumerable<WebSocket> WebSockets();

    /// <summary>
    /// Gets Socket id by WebSocket
    /// </summary>
    /// <param name="socket">Socket to find id</param>
    /// <returns>Socket Id</returns>
    public Guid GetId(WebSocket socket);

    /// <summary>
    /// Add socket to active user WebSockets list
    /// </summary>
    /// <param name="socket">WebSocket to add</param>
    /// <param name="info">User info</param>
    /// <param name="socketId">Adding WebSocket's Id</param>
    public Task AddSocket(Guid socketId, WebSocket socket, WsUserInfo info);

    /// <summary>
    /// To disconnect all active user WebSockets
    /// </summary>
    public void ClearAllClients();

    /// <summary>
    /// Remove WebSocket before reconnect if it exists
    /// </summary>
    /// <param name="socketId">Socket's Id</param>
    public Task RemoveWebSocketBeforeReconnect(Guid socketId);

    /// <summary>
    /// Abort connection of WebSocket with specific status
    /// </summary>
    /// <param name="socket">Socket to abort</param>
    /// <param name="abortStatus">Abort status</param>
    public Task AbortConnection(WebSocket socket, ConnectionAborted abortStatus = ConnectionAborted.TokenExpiredOrInvalid);

    /// <summary>
    /// Disconnect WebSocket if it exists
    /// </summary>
    /// <param name="socketId">Socket id to remove</param>
    /// <param name="closeDescription">Description of close connection</param>
    /// <param name="becauseClause">Because clause for close connection</param>
    /// <param name="closeStatus">Close status</param>
    /// <param name="notifyClient">True to notify client, otherwise false</param>
    public Task RemoveSocket(Guid socketId, string closeDescription = "The connection closed by the server", string becauseClause = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, bool notifyClient = true);
}