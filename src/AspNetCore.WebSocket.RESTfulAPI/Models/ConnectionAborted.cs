using System.ComponentModel.DataAnnotations;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public enum ConnectionAborted
{
    None = 0,

    [Display(Name = "The token updated")]
    TokenUpdated = 1,

    [Display(Name = "The token already is expired or invalid")]
    TokenExpiredOrInvalid = 2,

    [Display(Name = "The server not working")]
    ServerNotWorking = 3,

    [Display(Name = "User not exist")]
    UserNotExist = 4,

    [Display(Name = "User does not have access")]
    UserDoesNotHaveAccess = 5,

    [Display(Name = "The user id not found from header of request")]
    UserIdNotFound = 6,

    [Display(Name = "The user name not found from header of request")]
    UserNameNotFound = 7,
}