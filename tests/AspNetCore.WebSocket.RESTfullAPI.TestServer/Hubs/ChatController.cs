using AspNetCore.WebSocket.RESTfullAPI.Models;
using AspNetCore.WebSocket.RESTfullAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("ChatWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to chat")]
    public class ChatController
    {
        private readonly WebSocketHub _socketHub;
        private readonly WSUserInfo _wsUserInfo;

        public ChatController(WebSocketHub socketHub, WSUserInfo wsUserInfo)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
        }

        [HttpPost("Chat.DirectWithFriend")]
        [WSHubMethodName("Chat.DirectWithFriend")]
        [SwaggerOperation(Summary = "This is only for messaging one user with own friend")]
        [SwaggerResponse(0, "Return true when request successfully completed", typeof(string))]
        public async Task<WSRequestModel> DirectWithFriend([SwaggerParameter("This is must be another user's Id", Required = true)] Guid userId, [SwaggerParameter(Required = true)] string message)
        {
            await _socketHub.SendNotificationAsync(userId, $"{_wsUserInfo.UserName} user send '{message}' message", "Chat.DirectWithFriend");
            return await WSRequestModel.SuccessAsync();
        }

        [HttpPost("Chat.MessageToAll")]
        [WSHubMethodName("Chat.MessageToAll")]
        [SwaggerOperation(Summary = "This is for sending message to all another user")]
        [SwaggerResponse(0, "Return true when request successfully completed", typeof(bool))]
        public async Task<WSRequestModel> MessageToAll([SwaggerParameter(Required = true)] string message)
        {
            var allConnecttedUserIds = _socketHub.WebSocketManager.Clients().Values.Select(v => v.Info.UserId);
            await _socketHub.SendNotificationAsync(allConnecttedUserIds.Where(id => id != _wsUserInfo.UserId), $"{_wsUserInfo.UserName} user send '{message}' message", "Chat.MessageToAll");
            return await WSRequestModel.SuccessAsync();
        }
    }
}
