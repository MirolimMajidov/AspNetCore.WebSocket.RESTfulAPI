using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public class WsController
{
    public string Name { get; set; }

    public Type Controller { get; set; }

    public List<WsMethod> Methods { get; set; } = [];

    internal ObjectFactory Factory { get; set; }
}
