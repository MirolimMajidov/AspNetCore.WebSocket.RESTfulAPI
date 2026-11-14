using System.Collections.Generic;
using System.Text.Json;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

internal sealed class WebSocketRequest
{
    public string Method { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public Dictionary<string, JsonElement> Params { get; init; }
}
