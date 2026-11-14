namespace AspNetCore.WebSocket.RESTfulAPI.Models;

/// <summary>Stable protocol error codes returned to WebSocket clients.</summary>
public static class WebSocketErrorCodes
{
    public const int InvalidJson = 101;
    public const int MissingRequestProperty = 102;
    public const int InvalidMethodFormat = 103;
    public const int ControllerNotFound = 104;
    public const int ConnectionNotRegistered = 105;
    public const int InvalidParameters = 106;
    public const int MethodNotFound = 107;
    public const int ControllerActivationFailed = 108;
    public const int InvalidEndpointConfiguration = 109;
    public const int InternalServerError = 110;
}
