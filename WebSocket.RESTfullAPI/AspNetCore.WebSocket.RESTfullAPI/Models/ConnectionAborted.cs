using System.ComponentModel.DataAnnotations;

namespace AspNetCore.WebSocket.RESTfullAPI.Models
{
    public enum ConnectionAborted
    {
        None = 0,

        [Display(Name = "The token cannot be empty")]
        TokenIsEmpty = 1,

        [Display(Name = "The token already is expired or invalid")]
        TokenExpiredOrInvalid = 2,

        [Display(Name = "The user id not found from header of request")]
        UserIdNotFound = 3,

        [Display(Name = "The user name not found from header of request")]
        UserNameNotFound = 4,
    }
}
