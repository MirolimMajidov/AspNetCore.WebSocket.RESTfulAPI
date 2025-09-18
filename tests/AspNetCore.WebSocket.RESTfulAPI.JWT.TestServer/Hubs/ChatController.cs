using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.JWT.Services;
using AspNetCore.WebSocket.RESTfulAPI.Models;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("ChatWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to chat")]
    public class ChatController
    {
        private readonly WsHub _socketHub;
        private readonly WsUserInfo _wsUserInfo;
        private readonly ILogger _logger;

        public ChatController(WsHub socketHub, WsUserInfo wsUserInfo, ILogger logger)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
            _logger = logger;
        }

        [HttpPost("Chat.Message")]
        [WsHubMethodName("Chat.Message")]
        [SwaggerOperation(Summary = "This is only for messaging one user with own friend")]
        [SwaggerResponse(0, "Return info when request successfully completed", typeof(string))]
        public async Task<ResponseModel> DirectWithFriend([SwaggerParameter("This is must be another user's Id", Required = true)] Guid userId, [SwaggerParameter(Required = true)] string message)
        {
            await _socketHub.SendNotificationAsync(userId, $"{_wsUserInfo.Name} user sent '{message}' message", "Chat.Message");
            return await ResponseModel.SuccessRequestAsync($"'{message}' message sent to '{userId}' user");
        }

        [HttpPost("Chat.MessageToAll")]
        [WsHubMethodName("Chat.MessageToAll")]
        [SwaggerOperation(Summary = "This is for sending message to all another user")]
        [SwaggerResponse(0, "Return info when request successfully completed", typeof(string))]
        public async Task<ResponseModel> MessageToAll([SwaggerParameter(Required = true)] string message)
        {
            var allConnecttedUserIds = _socketHub.Manager.UsersInfo().Where(user => user.Id != _wsUserInfo.Id).Select(v => v.Id);
            await _socketHub.SendNotificationAsync(allConnecttedUserIds, $"{_wsUserInfo.Name} user sent '{message}' message", "Chat.MessageToAll");
            return await ResponseModel.SuccessRequestAsync($"'{message}' message sent to all active users");
        }
    }
}
