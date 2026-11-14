using System;

namespace AspNetCore.WebSocket.RESTfulAPI.Models;

public abstract class Disposable : IDisposable
{
    /// <summary>   
    /// This is need for Dispose this object from memory.
    /// </summary>    
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
