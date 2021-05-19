# AspNetCore.WebSocket.RESTfullAPI 

AspNetCore.WebSocket.RESTfullAPI is a communication library with Web Socket like RESTfull API on ASP.NET Core applications. It is easy to set up, and runs on all recent .NET, .NET Framework and .NET Standard platforms. It's used in apps that benefit from fast, real-time communication, such as chat, dashboard, and game applications.


## List of NuGet packages

<table>
   <thead>
    <th>№</th>
    <th>Name</th>
    <th>Description</th>
    <th>Endpoints</th>
  </thead>
  <tbody>
    <tr>
        <td align="center">1.</td>
        <td> <a href="https://www.nuget.org/packages/AspNetCore.WebSocket.RESTfullAPI">AspNetCore.WebSocket.RESTfullAPI</a></td>
        <td>This for implementing and using Web Socket on ASP.Net Core to create real-time communication app for building chat, game or other.</td>
        <td> <a href="https://..">View</a> </td>
    </tr>
    <tr>
        <td align="center">2.</td>
        <td> <a href="https://www.nuget.org/packages/AspNetCore.WebSocket.RESTfullAPI.JWT/">AspNetCore.WebSocket.RESTfullAPI.JWT</a></td>
        <td>This library also for creating real-time communication app like AspNetCore.WebSocket.RESTfullAPI, but it is customized for using authorized user by JSON Web Token (JWT)</td>
        <td> <a href="https://..">View</a> </td>
    </tr>
  </tbody>  
</table>

## Getting Started

Make sure you have configured [Web Sockets for IIS](https://docs.microsoft.com/en-us/iis/configuration/system.webserver/websocket) in your machine. After that, you need to instal AspNetCore.WebSocket.RESTfullAPI NuGet.

```powershell
Install-Package AspNetCore.WebSocket.RESTfullAPI
```
Add the WebSocket's classes to the services in the ConfigureServices method of the Startup class:
```
services.AddWebSocketManager();
```
Add the `WebSocketRESTfullAPI` middleware in the Configure method of the Startup class:
```
app.WebSocketRESTfullAPI("/WSMessenger", receiveBufferSize: 5, keepAliveInterval: 30, loggAllWebSocketRequestAndResponse: false);
```
The following settings can be configured by passing to parameters of WebSocketRESTfullAPI method: <br/>
`Path` - Path to bind Web socket to listen client. Here path is "WSMessenger" and client should cannect to this path "ws://{BaseSiteURL}/WSMessenger". <br/>
`ReceiveBufferSize` - Gets or sets the size of the protocol buffer used to receive and parse frames. The default is 4 kb. Passing this parameter is nor required. <br/>
`KeepAliveInterval` - Gets or sets the frequency at which to send Ping/Pong keep-alive control frames. The default is 60 secunds. Passing this parameter is nor required. <br/>
`LoggAllWebSocketRequestAndResponse` - When you turn on it all request and response data of web socket will be logged to the your configurated file. By default it's false because it can be effect to performance. <br/>

All configuration is done. Now, you able to create your own Web Socket controllers with APIs.<br/> 

First you need to create a new folder which name `Hubs` on project and create new controller for Web socket APIs.

Example code:
```
public class ChatController
{
    private readonly WebSocketHub _socketHub;
    private readonly WSUserInfo _wsUserInfo;
    private readonly ILogger _logger;

    public ChatController(WebSocketHub socketHub, WSUserInfo wsUserInfo, ILogger logger)
    {
        _socketHub = socketHub;
        _wsUserInfo = wsUserInfo;
        _logger = logger;
    }

    //Your APIs  
}
```
 The controller name must be start from `{NameController}Controller` and it should have constructor parameters for `WebSocketHub`, `WSUserInfo` and `ILogger`: <br/>
`WebSocketHub` - This is will be use for sending data by notification to another needed users or for getting acces to the WebSocketManager to get all active client with users data. It can be sub class of WebSocketHub if you want to override (See example from AspNetCore.WebSocket.RESTfullAPI.JWT project to override).<br/>
`WSUserInfo` - To get needed user info which we attach on creating Web socket. It can be sub class of WSUserInfo if you want to override user data and add some other user information to this parameter (See example from AspNetCore.WebSocket.RESTfullAPI.JWT project to create sub class and to use it).<br/>
`ILogger` - This is just for adding logg if it's needed.

