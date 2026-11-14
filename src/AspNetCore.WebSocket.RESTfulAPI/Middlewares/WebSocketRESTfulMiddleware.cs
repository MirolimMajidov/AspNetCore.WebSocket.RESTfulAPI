using System;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.WebSocket.RESTfulAPI.Configurations;
using AspNetCore.WebSocket.RESTfulAPI.Helpers;
using AspNetCore.WebSocket.RESTfulAPI.Models;
using AspNetCore.WebSocket.RESTfulAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSocketManager = AspNetCore.WebSocket.RESTfulAPI.Services.WebSocketManager;

namespace AspNetCore.WebSocket.RESTfulAPI.Middlewares;

public class WebSocketRESTfulMiddleware
{
    private WebSocketHub WebSocketHub { get; }
    private ILogger Logger { get;}

    public WebSocketRESTfulMiddleware(RequestDelegate next, WebSocketHub webSocketHub, ILogger<WebSocketRESTfulMiddleware> logger)
    {
        WebSocketHub = webSocketHub;
        Logger = logger;
    }

    public virtual async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var userId = context.Request.GetHeaderValue("UserId");
            var userName = context.Request.GetHeaderValue("UserName");
            ConnectionAborted abortStatus = ConnectionAborted.None;

            if (!Guid.TryParse(userId, out var userIdAsGuid))
                abortStatus = ConnectionAborted.UserIdNotFound;
            else if (string.IsNullOrEmpty(userName))
                abortStatus = ConnectionAborted.UserNameNotFound;

            var info = new WsUserInfo
            {
                Id = userIdAsGuid,
                Name = userName
            };

            await InvokeWsAsync(context, info, abortStatus);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error on opening connection of WebSocket to the server");
        }
    }

    protected async Task InvokeWsAsync(HttpContext context, WsUserInfo userInfo, ConnectionAborted abortStatus = ConnectionAborted.None)
    {
        try
        {
            await WebSocketHub.Manager.RemoveWebSocketBeforeReconnect(userInfo.Id);
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            if (abortStatus == ConnectionAborted.None)
            {
                await WebSocketHub.OnConnectedAsync(socket, userInfo);

                await Receive(socket, async (messageType, buffer, count) =>
                {
                    if (messageType == WebSocketMessageType.Binary)
                        await WebSocketHub.ReceiveMessageAsync(socket, Encoding.UTF8.GetString(buffer, 0, count));
                    else
                        await WebSocketHub.OnDisconnectedAsync(userInfo.Id);
                });
            }
            else
                await WebSocketHub.Manager.AbortConnection(socket, abortStatus);
        }
        catch
        {
            throw;
        }
    }

    private async Task Receive(System.Net.WebSockets.WebSocket socket,
        Func<WebSocketMessageType, byte[], int, Task> handleMessage)
    {
        try
        {
            var buffer = new byte[WebSocketManager.WebSocketBufferSize];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close || result.EndOfMessage)
                {
                    await handleMessage(result.MessageType, buffer, result.Count);
                    continue;
                }

                using var message = new MemoryStream(result.Count + buffer.Length);
                message.Write(buffer, 0, result.Count);
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var completeMessage = message.ToArray();
                await handleMessage(result.MessageType, completeMessage, completeMessage.Length);
            }
        }
        catch
        {
            var userId = WebSocketHub.Manager?.GetId(socket);
            await WebSocketHub.OnDisconnectedAsync(userId);
        }
    }
}

public static class WebSocketHubExtensions
{
    /// <summary>
    /// For mapping path to WebSocketHub and set some Web Socket configurations
    /// </summary>
    /// <param name="path">Path to bind Web socket</param>
    /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
    /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 seconds</param>
    /// <param name="logAllWebSocketRequestAndResponse">When you turn on it, all request and response data of web sockets will be logged to log files. By default, it's false because it can be effected to performance</param>
    public static IApplicationBuilder MapWebSocket<TMiddleware, TWebSocketHub>(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool logAllWebSocketRequestAndResponse = false) where TMiddleware : WebSocketRESTfulMiddleware where TWebSocketHub : WebSocketHub
    {
        WebSocketManager.LogAllWsRequest = logAllWebSocketRequestAndResponse;
        WebSocketManager.WebSocketBufferSize = 1024 * receiveBufferSize;
        builder.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(keepAliveInterval) });

        var serviceScopeFactory = builder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
        var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
        var wsHub = serviceProvider.GetService<TWebSocketHub>();

        var controllerAssemblies = new[]
            {
                Assembly.GetEntryAssembly(),
                typeof(TMiddleware).Assembly,
                typeof(TWebSocketHub).Assembly
            }
            .Where(assembly => assembly != null)
            .Distinct();
        var controllers = controllerAssemblies
            .SelectMany(assembly => assembly!.ExportedTypes)
            .Where(type => type.IsClass &&
                           !type.IsAbstract &&
                           type.Name.EndsWith("Controller") &&
                           type.GetMethods().Any(method => method.GetWsHubAttribute() != null))
            .Distinct()
            .ToList();
        foreach (var controller in controllers)
            CreateController(controller);
        wsHub.InitializeEndpoints();

        return builder.Map(path, (app) => app.UseMiddleware<TMiddleware>(serviceProvider.GetService<TWebSocketHub>()));

        void CreateController(Type controller)
        {
            var constructor = controller.GetConstructors().SingleOrDefault(candidate =>
            {
                var parameters = candidate.GetParameters();
                return parameters.Length == 3 &&
                       parameters[0].ParameterType.IsAssignableFrom(typeof(TWebSocketHub)) &&
                       typeof(WsUserInfo).IsAssignableFrom(parameters[1].ParameterType) &&
                       parameters[2].ParameterType.IsAssignableFrom(typeof(ILogger));
            }) ?? throw new InvalidOperationException(
                $"WebSocket controller '{controller.FullName}' must have exactly one public constructor " +
                $"compatible with ({typeof(TWebSocketHub).Name}, {nameof(WsUserInfo)}, {nameof(ILogger)}).");

            var wsController = new WsController
            {
                Name = controller.Name[..^10],
                Controller = controller,
                Factory = CompileControllerFactory(constructor)
            };
            var methods = controller.GetMethods().Where(m => m.GetWsHubAttribute() != null);
            foreach (var method in methods)
                CreateMethod(wsController, method);

            wsHub.Manager.Controllers.Add(wsController);
        }

        void CreateMethod(WsController controller, MethodInfo method)
        {
            if (method.ReturnType != typeof(Task<ResponseModel>))
                throw new InvalidOperationException(
                    $"WebSocket endpoint '{method.DeclaringType?.FullName}.{method.Name}' must return Task<ResponseModel>.");

            var parameters = method.GetParameters();
            var wsMethod = new WsMethod
            {
                Name = method.GetWsHubAttribute().Name,
                Method = method,
                InvokeAsync = CompileMethodInvoker(method),
                ParameterNames = parameters.Select(parameter => parameter.Name!).ToArray(),
                ParameterTypes = parameters.Select(parameter => parameter.ParameterType).ToArray(),
                DefaultValues = parameters.Select(parameter => parameter.DefaultValue).ToArray(),
                HasDefaultValues = parameters.Select(parameter => parameter.HasDefaultValue).ToArray()
            };
            foreach (var parameter in parameters)
                wsMethod.Parameters.Add(parameter);

            controller.Methods.Add(wsMethod);
        }

        static Func<WebSocketHub, WsUserInfo, ILogger, object> CompileControllerFactory(ConstructorInfo constructor)
        {
            var hub = Expression.Parameter(typeof(WebSocketHub), "hub");
            var user = Expression.Parameter(typeof(WsUserInfo), "user");
            var logger = Expression.Parameter(typeof(ILogger), "logger");
            var parameters = constructor.GetParameters();
            var create = Expression.New(constructor,
                Expression.Convert(hub, parameters[0].ParameterType),
                Expression.Convert(user, parameters[1].ParameterType),
                Expression.Convert(logger, parameters[2].ParameterType));
            return Expression.Lambda<Func<WebSocketHub, WsUserInfo, ILogger, object>>(
                Expression.Convert(create, typeof(object)), hub, user, logger).Compile();
        }

        static Func<object, object[], Task<ResponseModel>> CompileMethodInvoker(MethodInfo method)
        {
            var target = Expression.Parameter(typeof(object), "target");
            var arguments = Expression.Parameter(typeof(object[]), "arguments");
            var convertedArguments = method.GetParameters()
                .Select((parameter, index) => Expression.Convert(
                    Expression.ArrayIndex(arguments, Expression.Constant(index)), parameter.ParameterType));
            var call = Expression.Call(Expression.Convert(target, method.DeclaringType!), method,
                convertedArguments);
            return Expression.Lambda<Func<object, object[], Task<ResponseModel>>>(call, target, arguments).Compile();
        }
    }

    /// <summary>
    /// For mapping path to WebSocket RESTful API and set some Web Socket configurations
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">Path to bind Web socket</param>
    /// <param name="receiveBufferSize">Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb</param>
    /// <param name="keepAliveInterval">Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 seconds</param>
    /// <param name="logAllWebSocketRequestAndResponse">When you turn on it, all request and response data of web sockets will be logged to log files. By default, it's false because it can be effected to performance</param>
    public static IApplicationBuilder WebSocketRESTfulAPI(this IApplicationBuilder builder, PathString path, int receiveBufferSize = 4, int keepAliveInterval = 60, bool logAllWebSocketRequestAndResponse = false)
    {
        return builder.MapWebSocket<WebSocketRESTfulMiddleware, WebSocketHub>(path, keepAliveInterval: keepAliveInterval, receiveBufferSize: receiveBufferSize, logAllWebSocketRequestAndResponse: logAllWebSocketRequestAndResponse);
    }
}
