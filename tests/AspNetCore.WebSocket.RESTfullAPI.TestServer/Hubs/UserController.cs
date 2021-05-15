using AspNetCore.WebSocket.RESTfullAPI.Models;
using AspNetCore.WebSocket.RESTfullAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("UserWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to user")]
    public class UserController
    {
        private readonly WebSocketHub _socketHub;
        private readonly WSUserInfo _wsUserInfo;

        public UserController(WebSocketHub socketHub, WSUserInfo wsUserInfo)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
        }

        [HttpGet("User.Info")]
        [WSHubMethodName("User.Info")]
        [SwaggerOperation(Summary = "Gets current user's info")]
        [SwaggerResponse(0, "Return user info", typeof(WSUserInfo))]
        public async Task<WSRequestModel> GetUserMyData()
        {
            return await WSRequestModel.SuccessAsync(_wsUserInfo);
        }
    }
}
