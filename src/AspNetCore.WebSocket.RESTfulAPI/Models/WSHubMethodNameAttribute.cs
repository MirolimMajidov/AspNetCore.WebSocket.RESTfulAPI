using System;

namespace AspNetCore.WebSocket.RESTfulAPI
{
    // Summary:
    //     Customizes the name of a hub method.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WSHubMethodNameAttribute : Attribute
    {
        /// <summary>
        /// The customized name of the hub method.
        /// </summary>
        /// <param name="name">Name of hub</param>
        public WSHubMethodNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The customized name of the hub method
        /// </summary>
        public string Name { get; }
    }
}
