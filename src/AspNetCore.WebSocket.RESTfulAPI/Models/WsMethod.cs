using System.Collections.Generic;
using System.Reflection;
using System;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsMethod
{
    public string Name { get; set; }

    public MethodInfo Method { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();

    internal Func<object, object[], Task<ResponseModel>> InvokeAsync { get; set; }

    internal string[] ParameterNames { get; set; }

    internal Type[] ParameterTypes { get; set; }

    internal object[] DefaultValues { get; set; }

    internal bool[] HasDefaultValues { get; set; }
}
