using System;
using System.Collections.Generic;
using AspNetCore.WebSocket.RESTfulAPI.Services;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsController
{
    public string Name { get; set; }

    public Type Controller { get; set; }

    public List<WsMethod> Methods { get; set; } = [];

    internal Func<WebSocketHub, WsUserInfo, object> Factory { get; set; }
}
