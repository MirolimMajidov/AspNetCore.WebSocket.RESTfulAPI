using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestServer.Hubs
{
    [Produces("application/json")]
    [Route("UserWS")]
    [ApiExplorerSettings(GroupName = "WS")]
    [SwaggerTag("All WebSocket APIs related to user")]
    public class UserController
    {
        private readonly WSHub _socketHub;
        private readonly WSUserInfo _wsUserInfo;
        private readonly ILogger _logger;

        public UserController(WSHub socketHub, WSUserInfo wsUserInfo, ILogger logger)
        {
            _socketHub = socketHub;
            _wsUserInfo = wsUserInfo;
            _logger = logger;
        }

        [HttpGet("User.Info")]
        [WSHubMethodName("User.Info")]
        [SwaggerOperation(Summary = "Gets current user's info")]
        [SwaggerResponse(0, "Return user info", typeof(WSUserInfo))]
        public async Task<ResponseModel> GetUserMyData()
        {
            return await ResponseModel.SuccessRequestAsync(_wsUserInfo);
        }
    }
}
