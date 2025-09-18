using System;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsUserInfo : Disposable
{
    /// <summary>
    /// The user's Id
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The user's Name
    /// </summary>
    public required string Name { get; set; }
}