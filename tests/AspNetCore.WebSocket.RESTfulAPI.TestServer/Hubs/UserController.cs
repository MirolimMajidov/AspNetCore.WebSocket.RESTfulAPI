using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using AspNetCore.WebSocket.RESTfulAPI.Services;
using System;

namespace AspNetCore.WebSocket.RESTfulAPI.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("UserWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to user")]
    public class UserController
    {
        private readonly WebSocketHub _socketHub;
        private readonly WsUserInfo _wsUserInfo;

        public UserController(WebSocketHub socketHub, WsUserInfo wsUserInfo)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
        }

        [HttpGet("User.Info")]
        [WsHubMethodName("User.Info")]
        [SwaggerOperation(Summary = "Gets current user's info")]
        [SwaggerResponse(0, "Return user info", typeof(WsUserInfo))]
        public async Task<ResponseModel> GetUserMyData()
        {
            return await ResponseModel.SuccessRequestAsync(_wsUserInfo);
        }

        [WsHubMethodName("User.ScopedService")]
        public Task<ResponseModel> GetScopedService([FromServices] ScopedRequestMarker marker, [FromServices] ILogger<UserController> logger)
        {
            logger.LogInformation("The 'User.ScopedService' WS API is executing.");
            
            return Task.FromResult(ResponseModel.SuccessRequest(marker.Id));
        }
    }

    public sealed class ScopedRequestMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}
