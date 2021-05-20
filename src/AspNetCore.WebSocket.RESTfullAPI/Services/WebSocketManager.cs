using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public class WebSocketManager : Disposable, IWebSocketManager
    {
        private readonly ConcurrentDictionary<string, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)> sockets = new ConcurrentDictionary<string, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)>();

        public static bool LoggAllWSRequest { get; set; }
        public static int WebSocketBufferSize { get; set; }
        public static string CurrentAssemblyName { get; set; }
        private readonly ILogger _logger;

        public WebSocketManager(ILogger<WebSocketManager> logger)
        {
            _logger = logger;
        }

        /// <summary/>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public System.Net.WebSockets.WebSocket GetWebSocket(object socketId)
        {
            return sockets.FirstOrDefault(p => p.Key == socketId.ToString()).Value.WS;
        }

        /// <summary>
        /// Gets Info from all active user list by WebSocket
        /// </summary>
        /// <param name="socket">WebSocket</param>
        /// <returns>Info</returns>
        public WSUserInfo GetUserInfo(System.Net.WebSockets.WebSocket socket)
        {
            return sockets.FirstOrDefault(p => p.Value.WS == socket).Value.Info;
        }

        /// <summary>
        /// Gets all active user WebSockets list
        /// </summary>
        /// <returns>List of WebSockets</returns>
        public ConcurrentDictionary<string, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)> Clients()
        {
            return sockets;
        }

        /// <summary>
        /// Gets all active user info of clients
        /// </summary>
        /// <returns>List of Users Info</returns>
        public IEnumerable<WSUserInfo> UsersInfo()
        {
            return sockets.Select(s => s.Value.Info);
        }

        /// <summary>
        /// Gets all active Web sockets of clients
        /// </summary>
        /// <returns>List of WebSockets</returns>
        public IEnumerable<System.Net.WebSockets.WebSocket> WebSockets()
        {
            return sockets.Select(s => s.Value.WS);
        }

        /// <summary>
        /// Gets SocketID from all active users list by socket
        /// </summary>
        /// <param name="id">Socket to find Id</param>
        /// <returns>Socket Id</returns>
        public object GetId(System.Net.WebSockets.WebSocket socket)
        {
            return sockets.FirstOrDefault(p => p.Value.WS == socket).Key;
        }

        /// <summary>
        /// Add socket to active user WebSockets list
        /// </summary>
        /// <param name="socket">WebSocket to add</param>
        /// <param name="socketId">Adding WebSocket's Id</param>
        public async Task AddSocket(object socketId, System.Net.WebSockets.WebSocket socket, WSUserInfo Info)
        {
            await Task.Run(() => { sockets.TryAdd(socketId.ToString(), (socket, Info)); });
        }

        /// <summary>
        /// To disconnected all active user WebSockets
        /// </summary>
        public void ClearAllClients()
        {
            Task.Run(async () =>
            {
                foreach (var client in sockets)
                    await RemoveSocket(client.Key);
            });
        }

        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public async Task RemoveWebSocketIfExists(object socketId)
        {
            if (sockets.ContainsKey(socketId.ToString()))
                await RemoveSocket(socketId, "WebSocket reconnecting", nitifyClient: false);
        }

        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public async Task AbortConnection(System.Net.WebSockets.WebSocket socket, ConnectionAborted abortStatus = ConnectionAborted.TokenExpiredOrInvalid)
        {
            try
            {
                await WebSocketHub.SendNotificationAsync(socket, new { status = abortStatus }, logger: _logger, method: Notification.ConnectionAborted);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "The connection closed by the server", CancellationToken.None);
            }
            catch
            { }
        }

        /// <summary>
        /// Disconnect WebSocket if it exists
        /// </summary>
        public async Task RemoveSocket(object socketId, string closeDescription = "The connection closed by the server", string becauseClause = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, bool nitifyClient = true)
        {
            if (this == null || socketId == null) return;

            if (sockets.TryRemove(socketId.ToString(), out (System.Net.WebSockets.WebSocket WS, WSUserInfo Info) socketInfo))
            {
                try
                {
                    var description = becauseClause == null ? closeDescription : $"{closeDescription} because {becauseClause}";

                    if (LoggAllWSRequest && _logger != null)
                        _logger.LogInformation($"{description}, User Id: {socketId}, Connection state: {socketInfo.WS?.State}");

                    if (socketInfo.WS?.State == WebSocketState.Open)
                    {
                        if (nitifyClient)
                            await WebSocketHub.SendNotificationAsync(socketInfo.WS, new { description }, method: Notification.UserUnAuth, logger: _logger);

                        await socketInfo.WS.CloseOutputAsync(closeStatus, description, CancellationToken.None);
                    }
                    socketInfo.WS?.Dispose();
                }
                catch
                { }
                finally
                {
                    socketInfo.Info?.Dispose();
                }
            }
        }
    }
}
