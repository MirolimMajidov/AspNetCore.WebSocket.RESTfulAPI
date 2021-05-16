using AspNetCore.WebSocket.RESTfullAPI.Models;
using AspNetCore.WebSocket.RESTfullAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AspNetCore.WebSocket.RESTfullAPI.Configurations
{
    public static class WebSocketConfigurations
    {
        public static WSHubMethodNameAttribute GetWSHubAttribute(this MethodInfo method)
        {
            return method.GetCustomAttribute(typeof(WSHubMethodNameAttribute)) as WSHubMethodNameAttribute;
        }

        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<IWebSocketManager, WebSocketManager>();
            services.AddScoped<WebSocketHub>();

            foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketHub))
                    services.AddScoped(type);
            }

            return services;
        }
    }
}
