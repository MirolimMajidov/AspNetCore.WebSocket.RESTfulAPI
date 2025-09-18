namespace AspNetCore.WebSocket.RESTfulAPI.Models;

internal class Notification
{
    public static string WSConnected => "WSConnected";

    public static string ConnectionAborted => "ConnectionAborted";

    public static string UserUnAuth => "User.UnAuth";
}