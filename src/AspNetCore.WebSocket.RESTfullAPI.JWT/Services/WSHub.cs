using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT
{
    public class WSHub : WebSocketHub
    {
        public WSHub(IWebSocketManager _manager, ILogger<WSHub> logger) : base(_manager, logger) { }

        /// <summary>
        /// To connect new created WebSocket to the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socket">New created socket</param>
        /// <param name="userInfo">Current User info</param>
        public override async Task OnConnectedAsync(System.Net.WebSockets.WebSocket socket, WSUserInfo userInfo)
        {
            await base.OnConnectedAsync(socket, userInfo);
        }

        /// <summary>
        /// To disconnect exists WebSocket from the WebSocketManager's sockets list
        /// </summary>
        /// <param name="socketId">Id of current WebSocket to disconnect</param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(object socketId)
        {
            await base.OnDisconnectedAsync(socketId);
        }
    }
}
