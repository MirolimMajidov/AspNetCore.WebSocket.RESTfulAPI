using System;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsUserInfo : Disposable
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }
}