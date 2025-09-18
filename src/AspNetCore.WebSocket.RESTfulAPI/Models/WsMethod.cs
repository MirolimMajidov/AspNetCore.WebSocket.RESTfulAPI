using System.Collections.Generic;
using System.Reflection;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsMethod
{
    public string Name { get; set; }

    public MethodInfo Method { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
}