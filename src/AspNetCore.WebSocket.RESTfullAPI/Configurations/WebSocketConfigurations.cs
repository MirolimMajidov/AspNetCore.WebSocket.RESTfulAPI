using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace AspNetCore.WebSocket.RESTfullAPI
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

            var allAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(name => Assembly.Load(name)).ToList();
            foreach (var type in allAssemblies.SelectMany(a => a.ExportedTypes))
            {
                if (type.BaseType == typeof(WebSocketHub))
                    services.AddScoped(type);
            }

            return services;
        }
    }
}
