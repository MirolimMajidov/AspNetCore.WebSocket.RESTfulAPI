using System.Linq;
using System.Reflection;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using AspNetCore.WebSocket.RESTfulAPI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.WebSocket.RESTfulAPI.Configurations;

public static class WebSocketConfigurations
{
    public static WsHubMethodNameAttribute GetWsHubAttribute(this MethodInfo method)
    {
        return method.GetCustomAttribute(typeof(WsHubMethodNameAttribute)) as WsHubMethodNameAttribute;
    }

    public static IServiceCollection AddWebSocketServices(this IServiceCollection services)
    {
        services.AddSingleton<IWebSocketManager, WebSocketManager>();
        services.AddScoped<WebSocketHub>();

        var allAssemblies = Assembly.GetEntryAssembly()!.GetReferencedAssemblies().Select(name => Assembly.Load(name)).ToList();
        allAssemblies.Add(Assembly.GetEntryAssembly());
        foreach (var type in allAssemblies.SelectMany(a => a.ExportedTypes))
        {
            if (type.BaseType == typeof(WebSocketHub))
                services.AddScoped(type);
        }

        return services;
    }
}