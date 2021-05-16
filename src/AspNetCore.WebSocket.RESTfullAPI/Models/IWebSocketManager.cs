using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Models
{
    public interface IWebSocketManager
    {
        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public System.Net.WebSockets.WebSocket GetWebSocket(object socketId);

        /// <summary>
        /// Gets Info from all active user list by WebSocket
        /// </summary>
        /// <param name="socket">WebSocket</param>
        /// <returns>Info</returns>
        public WSUserInfo GetUserInfo(System.Net.WebSockets.WebSocket socket);

        /// <summary>
        /// Gets all active user WebSockets list
        /// </summary>
        /// <returns>List of WebSockets</returns>
        public ConcurrentDictionary<object, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)> Clients();

        /// <summary>
        /// Gets all active user info of clients
        /// </summary>
        /// <returns>List of Users Info</returns>
        public IEnumerable<WSUserInfo> UsersInfo();

        /// <summary>
        /// Gets all active Web sockets of clients
        /// </summary>
        /// <returns>List of WebSockets</returns>
        public IEnumerable<System.Net.WebSockets.WebSocket> WebSockets();

        /// <summary>
        /// Gets SocketID from all active users list by socket
        /// </summary>
        /// <param name="id">Socket to find Id</param>
        /// <returns>Socket Id</returns>
        public object GetId(System.Net.WebSockets.WebSocket socket);

        /// <summary>
        /// Add socket to active user WebSockets list
        /// </summary>
        /// <param name="socket">WebSocket to add</param>
        /// <param name="socketId">Adding WebSocket's Id</param>
        public Task AddSocket(object socketId, System.Net.WebSockets.WebSocket socket, WSUserInfo Info);

        /// <summary>
        /// To disconnected all active user WebSockets
        /// </summary>
        public void ClearAllClients();

        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public Task RemoveWebSocketIfExists(object socketId);

        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public Task AbortConnection(System.Net.WebSockets.WebSocket socket, ConnectionAborted abortStatus = ConnectionAborted.TokenExpiredOrInvalid);

        /// <summary>
        /// Disconnect WebSocket if it exists
        /// </summary>
        public Task RemoveSocket(object socketId, string closeDescription = "The connection closed by the server", string becauseClause = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, bool nitifyClient = true);
    }
}
