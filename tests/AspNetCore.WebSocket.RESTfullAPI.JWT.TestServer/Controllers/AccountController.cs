using AspNetCore.WebSocket.RESTfullAPI.JWT.TestServer.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfullAPI.JWT.TestServer.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "APIs")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("Authorization")]
        [SwaggerOperation(Summary = "For authorization user to jobs services")]
        [SwaggerResponse(200, "Return token when authorization finished successfully", typeof(string))]
        public async Task<WSRequestModel> Authorization([FromForm, SwaggerParameter("Name of user", Required = true)] string userName, [FromForm, SwaggerParameter("Id of user", Required = true)] int userId)
        {
            if (string.IsNullOrEmpty(userName) || userId == 0)
                return await WSRequestModel.NotAccessAsync();

            var token = await GenerateToken(userName, userId);
            return await WSRequestModel.SuccessAsync(token);
        }

        [HttpGet("UserInfo")]
        public async Task<WSRequestModel> UserInfo()
        {
            return await WSRequestModel.SuccessAsync(new { User.Identity?.IsAuthenticated, UserName = User.GetUserName(), UserId = User.GetUserId()});
        }

        /// <summary>
        /// This is for creating new token by new identity
        /// </summary>
        /// <param name="user">User info</param>
        /// <returns>Return new generated token, refresh token and hash token</returns>
        private static async Task<string> GenerateToken(string userName, int userId)
        {
            return await Task.Run(async () =>
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var newIdentity = await GenerateIdentity(userName, userId);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Issuer = AuthOptions.ISSUER,
                    Audience = AuthOptions.AUDIENCE,
                    Subject = newIdentity,
                    Expires = DateTime.Now.Add(TimeSpan.FromMinutes(AuthOptions.TokenLIFETIME)),
                    SigningCredentials = new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);

                return tokenHandler.WriteToken(token);
            });
        }

        /// <summary>
        /// This is for creating new Identity by existent old identity or user and claims (IP address, mobile model, mobile id)
        /// </summary>
        /// <returns>Return new created ClaimsIdentity</returns>
        private static async Task<ClaimsIdentity> GenerateIdentity(string userName, int userId)
        {
            return await Task.Run(() =>
            {
                List<Claim> claims = new();
                claims.Add(CreateClaim(ClaimTypes.Name, userName));
                claims.Add(CreateClaim("UserId", userId.ToString()));

                static Claim CreateClaim(string key, string value) => new(key, value);

                ClaimsIdentity claimsIdentity = new(claims, JwtBearerDefaults.AuthenticationScheme, ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

                return claimsIdentity;
            });
        }
    }
}
