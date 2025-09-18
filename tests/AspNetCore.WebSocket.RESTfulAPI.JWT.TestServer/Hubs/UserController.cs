using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.JWT.Services;
using AspNetCore.WebSocket.RESTfulAPI.Models;

namespace AspNetCore.WebSocket.RESTfulAPI.JWT.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("UserWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to user")]
    public class UserController
    {
        private readonly WsHub _socketHub;
        private readonly WsUserInfo _wsUserInfo;
        private readonly ILogger _logger;

        public UserController(WsHub socketHub, WsUserInfo wsUserInfo, ILogger logger)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
            _logger = logger;
        }

        [HttpGet("User.Info")]
        [WsHubMethodName("User.Info")]
        [SwaggerOperation(Summary = "Gets current user's info")]
        [SwaggerResponse(0, "Return user info", typeof(WsUserInfo))]
        public async Task<ResponseModel> GetUserMyData()
        {
            return await ResponseModel.SuccessRequestAsync(_wsUserInfo);
        }
    }
}
