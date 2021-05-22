using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace AspNetCore.WebSocket.RESTfulAPI
{
    public static class HelperExtensions
    {
        /// <summary>
        /// This method for get value one of Header properties.
        /// </summary>
        /// <param name="request">Request for Header properties.</param>
        /// <param name="key">Key of header property.</param>
        /// <returns>Return gets value of header property</returns>
        internal static string GetHeaderValue(this HttpRequest request, string key) => request.Headers[key].ToString().Trim() ?? string.Empty;

        /// <summary>
        /// Changing enum and other object value's type
        /// </summary>
        public static object ConvertTo(this object value, Type typeTo)
        {
            try
            {
                if (value == null || value is DBNull) return null;

                if (typeTo.IsEnum)
                {
                    return Enum.ToObject(typeTo, value);
                }
                else if (typeTo.IsArray)
                {
                    Type type = typeTo.GetElementType();
                    var imputValue = value.ToString().TrimStart('[').TrimEnd(']');
                    if (type.Name == "String")
                    {
                        var elements = Regex.Replace(imputValue, @"\t|\n|\r| |""", string.Empty).Split(',');
                        return elements.Select(s => (string)Convert.ChangeType(s, type)).ToArray();
                    }
                    else
                    {
                        var elements = imputValue.Split(',');
                        return elements.Select(s => (int)Convert.ChangeType(s, type)).ToArray();
                    }
                }
                else if (typeTo.Name == nameof(Guid))
                {
                    return Guid.Parse(value.ToString());
                }
                else
                {
                    if (typeTo.IsGenericType && typeTo.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return Convert.ChangeType(value, Nullable.GetUnderlyingType(typeTo));
                    else
                        return Convert.ChangeType(value, typeTo);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"The server couldn't cast '{value}' object to '{typeTo.Name}' type. Error: {ex.Message}");
            }
        }
    }
}
