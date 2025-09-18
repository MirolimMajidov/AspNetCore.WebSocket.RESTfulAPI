using System;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

// Summary:
//     Customizes the name of a hub method.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class WsHubMethodNameAttribute : Attribute
{
    /// <summary>
    /// The customized name of the hub method.
    /// </summary>
    /// <param name="name">Name of hub</param>
    public WsHubMethodNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The customized name of the hub method
    /// </summary>
    public string Name { get; }
}