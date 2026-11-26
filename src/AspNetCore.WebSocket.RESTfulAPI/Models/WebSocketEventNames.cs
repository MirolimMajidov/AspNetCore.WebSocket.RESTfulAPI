namespace AspNetCore.WebSocket.RESTfulAPI.Models;

/// <summary>
/// Contains event names for server-sent WebSocket notifications.
/// </summary>
public class WebSocketEventNames
{
    /// <summary>
    /// Contains connection-related event names.
    /// </summary>
    public static class Connection
    {
        /// <summary>
        /// Sent after a WebSocket connection is registered.
        /// </summary>
        public const string Connected = "WSConnected";

        /// <summary>
        /// Sent before the server aborts a WebSocket connection.
        /// </summary>
        public const string Aborted = "ConnectionAborted";
    }

    /// <summary>
    /// Contains user-related event names.
    /// </summary>
    public static class User
    {
        /// <summary>
        /// Sent before the server disconnects an unauthorized user.
        /// </summary>
        public const string UnAuth = "User.UnAuth";
    }
}
