﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfulAPI.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("ChatWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to chat")]
    public class ChatController
    {
        private readonly WebSocketHub _socketHub;
        private readonly WSUserInfo _wsUserInfo;
        private readonly ILogger _logger;

        public ChatController(WebSocketHub socketHub, WSUserInfo wsUserInfo, ILogger logger)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
            _logger = logger;
        }

        [HttpPost("Chat.Message")]
        [WSHubMethodName("Chat.Message")]
        [SwaggerOperation(Summary = "This is only for messaging one user with own friend")]
        [SwaggerResponse(0, "Return info when request successfully completed", typeof(string))]
        public async Task<ResponseModel> DirectWithFriend([SwaggerParameter("This is must be another user's Id", Required = true)] Guid userId, [SwaggerParameter(Required = true)] string message)
        {
            await _socketHub.SendNotificationAsync(userId, $"{_wsUserInfo.Name} user send '{message}' message", "Chat.Message");
            return await ResponseModel.SuccessRequestAsync($"'{message}' message sended to '{userId}' user");
        }

        [HttpPost("Chat.MessageToAll")]
        [WSHubMethodName("Chat.MessageToAll")]
        [SwaggerOperation(Summary = "This is for sending message to all another user")]
        [SwaggerResponse(0, "Return info when request successfully completed", typeof(string))]
        public async Task<ResponseModel> MessageToAll([SwaggerParameter(Required = true)] string message)
        {
            var allConnecttedUserIds = _socketHub.WSManager.UsersInfo().Where(user => user.Id != _wsUserInfo.Id).Select(v => v.Id);
            await _socketHub.SendNotificationAsync(allConnecttedUserIds, $"{_wsUserInfo.Name} user send '{message}' message", "Chat.MessageToAll");
            return await ResponseModel.SuccessRequestAsync($"'{message}' message sended to all active users");
        }
    }
}
