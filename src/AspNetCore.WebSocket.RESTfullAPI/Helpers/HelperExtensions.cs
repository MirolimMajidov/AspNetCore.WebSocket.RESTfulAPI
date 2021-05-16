using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AspNetCore.WebSocket.RESTfullAPI
{
    public static class HelperExtensions
    {
        public static string GetBearerToken(this HttpContext context)
        {
            return context.Request.GetHeaderValue("Authorization").Replace("Bearer ", "");
        }

        /// <summary>
        /// This method for get all properties of Header.
        /// </summary>
        /// <param name="request">Request for Header properties.</param>
        /// <returns>Return gets list of all Header properties</returns>
        public static List<KeyValuePair<string, StringValues>> GetHeaders(this HttpRequest request) => request.Headers.ToList();

        /// <summary>
        /// This method for get value one of Header properties.
        /// </summary>
        /// <param name="request">Request for Header properties.</param>
        /// <param name="key">Key of header property.</param>
        /// <returns>Return gets value of header property</returns>
        public static string GetHeaderValue(this HttpRequest request, string key = "x-auth-token") => request.Headers[key].ToString().Trim() ?? string.Empty;

        /// <summary>
        /// This method for check value from Headers.
        /// </summary>
        /// <param name="request">Request for Header properties.</param>
        /// <param name="key">Key of header property.</param>
        /// <param name="value">Value of header property.</param>
        /// <returns>Return true when header exist in headers with this key and value</returns>
        public static bool HasHeader(this HttpRequest request, string key, string value) => request.Headers[key].ToString() == value;

        /// <summary>
        /// This is for get user name from User of Context
        /// </summary>
        /// <param name="user">User of Context</param>
        /// <returns>User NAme</returns>
        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user.Claims.SingleOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// This is for get user name from User of Context
        /// </summary>
        /// <param name="user">User of Context</param>
        /// <returns>User NAme</returns>
        public static string GetUserId(this ClaimsPrincipal user, string claimName = "UserId")
        {
            return user.Claims.SingleOrDefault(c => c.Type == claimName)?.Value;
        }

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
