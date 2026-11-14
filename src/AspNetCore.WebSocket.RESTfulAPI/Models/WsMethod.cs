using System.Collections.Generic;
using System.Reflection;
using System;
using System.Threading.Tasks;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsMethod
{
    public string Name { get; set; }

    public MethodInfo Method { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = [];

    internal Func<object, object[], Task<ResponseModel>> InvokeAsync { get; set; }

    internal WsParameterBinding[] Bindings { get; set; }

    #region Nested Types

    internal readonly record struct WsParameterBinding(
        string Name,
        Type Type,
        object DefaultValue,
        bool HasDefaultValue,
        bool FromServices);

    #endregion
}
