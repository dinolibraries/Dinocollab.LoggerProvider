using Dinocollab.LoggerProvider.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Dinocollab.LoggerProvider.QuestDB
{
    public static class HttpContextExtractExtension
    {
        public static (string? Controller, string? Action) GetControllerAndAction(this HttpContext context)
        {
            if (context == null)
                return (null, null);

            var routeValues = context.Request.RouteValues;

            string? controller = routeValues.TryGetValue("controller", out var c) ? c?.ToString() : null;
            string? action = routeValues.TryGetValue("action", out var a) ? a?.ToString() : null;

            return (controller, action);
        }
        public static HttpContextMessageLog CreateContextData(this HttpContext context,string level = "Information")
        {
            var (controller, action) = context.GetControllerAndAction();
            var claims = context.User.Claims.Select(c => new { c.Type, c.Value });
            var contextData = new HttpContextMessageLog
            {
                RequestId = context.TraceIdentifier,
                Method = context.Request.Method,
                Controller = controller,
                Action = action,
                Level = level,
                TraceId = context.TraceIdentifier,
                RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
                Host = context.Request.Headers.Host.ToString(),
                Path = context.Request.Path.ToString(),
                Query = context.Request.QueryString.ToString(),
                StatusCode = context.Response.StatusCode,
                IsAuthenticated = context.User.Identity?.IsAuthenticated,
                UserId = context.GetUserId(),
                UserName = context.GetDisplayName(),
                Referer = context.Request.Headers.Referer.ToString(),
                Assembly = Assembly.GetEntryAssembly()?.FullName,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                XForwardedFor = context.Request.Headers.FirstOrDefault(x => x.Key == "X-Forwarded-For").Value.ToString(),
                Claims = JsonConvert.SerializeObject(claims),
            };
            return contextData;
        }
        private static string? GetDisplayName(this HttpContext context)
        {
            return context.User.Claims.FirstOrDefault(x => x.Type == "name")?.Value
                ?? context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        }
        private static string? GetUserId(this HttpContext context)
        {
            return context.User.Claims.FirstOrDefault(x => x.Type == "sub")?.Value
                ?? context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
