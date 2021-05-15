using System;

namespace AspNetCore.WebSocket.RESTfullAPI.Models
{
    public class WSUserInfo : Disposable
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; }
    }
}
