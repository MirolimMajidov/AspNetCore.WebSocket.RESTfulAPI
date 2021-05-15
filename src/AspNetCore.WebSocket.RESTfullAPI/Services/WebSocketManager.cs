using AspNetCore.WebSocket.RESTfullAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.Services
{
    public class WebSocketManager : Disposable
    {
        private readonly ConcurrentDictionary<Guid, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)> sockets = new();

        public static bool LoggAllWSRequest { get; set; }
        public static int WebSocketBufferSize { get; set; }
        public static string CurrentAssemblyName { get; set; }

        /// <summary>
        /// Gets Socket from all active user list by ID
        /// </summary>
        /// <param name="socketId">Socket's Id</param>
        /// <returns>Socket</returns>
        public System.Net.WebSockets.WebSocket GetWebSocket(Guid socketId)
        {
            return sockets.FirstOrDefault(p => p.Key == socketId).Value.WS;
        }

        /// <summary>
        /// Gets Info from all active user list by WebSocket
        /// </summary>
        /// <param name="socket">WebSocket</param>
        /// <returns>Info</returns>
        public WSUserInfo GetInfo(System.Net.WebSockets.WebSocket socket)
        {
            return sockets.FirstOrDefault(p => p.Value.WS == socket).Value.Info;
        }

        /// <summary>
        /// Gets all active user WebSockets list
        /// </summary>
        /// <returns>Lsit of WebSockets</returns>
        public ConcurrentDictionary<Guid, (System.Net.WebSockets.WebSocket WS, WSUserInfo Info)> Clients()
        {
            return sockets;
        }

        /// <summary>
        /// Gets SocketID from all active users list by socket
        /// </summary>
        /// <param name="id">Socket to find Id</param>
        /// <returns>Socket Id</returns>
        public Guid GetId(System.Net.WebSockets.WebSocket socket)
        {
            return sockets.FirstOrDefault(p => p.Value.WS == socket).Key;
        }

        /// <summary>
        /// To check has connection or not 
        /// </summary>
        /// <param name="id">Socket to find Id</param>
        /// <returns>bool</returns>
        public bool HasConnection(Guid id)
        {
            return sockets.ContainsKey(id);
        }

        /// <summary>
        /// Add socket to active user WebSockets list
        /// </summary>
        /// <param name="socket">WebSocket to add</param>
        /// <param name="socketId">Adding WebSocket's Id</param>
        public async Task AddSocket(Guid socketId, System.Net.WebSockets.WebSocket socket, WSUserInfo Info)
        {
            await Task.Run(() => { sockets.TryAdd(socketId, (socket, Info)); });
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
        public async Task RemoveWebSocketIfExists(Guid socketId)
        {
            if (HasConnection(socketId))
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
                await WebSocketHub.SendNotificationAsync(socket, new { status = abortStatus }, logger: null, method: Notification.ConnectionAborted);
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "The connection closed by the server", CancellationToken.None);
            }
            catch
            { }
        }

        /// <summary>
        /// Disconnect WebSocket if it exists
        /// </summary>
        public async Task RemoveSocket(Guid socketId, string closeDescription = "The connection closed by the server", string becauseClause = null, WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, bool nitifyClient = true)
        {
            if (sockets.TryRemove(socketId, out (System.Net.WebSockets.WebSocket WS, WSUserInfo Info) socketInfo))
            {
                try
                {
                    var description = becauseClause == null ? closeDescription : $"{closeDescription} because {becauseClause}";

                    if (socketInfo.WS?.State == WebSocketState.Open)
                    {
                        if (nitifyClient)
                            await WebSocketHub.SendNotificationAsync(socketInfo.WS, new { description }, method: Notification.UserUnAuth, logger: null);

                        await socketInfo.WS.CloseOutputAsync(closeStatus, description, CancellationToken.None);
                    }
                    socketInfo.WS?.Dispose();
                }
                catch
                { }
                finally
                {
                    socketInfo.Info.Dispose();
                }
            }
        }
    }
}
