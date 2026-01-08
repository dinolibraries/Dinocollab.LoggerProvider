using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dinocollab.LoggerProvider.Models
{
    public sealed class HttpContextMessageLog
    {
        [Timestamp]
        public DateTime TimeStamp { get; set; }
        // trace
        public string? TraceId { get; set; }

        // request
        public string? Method { get; set; }
        public string? Host { get; set; }
        public string? RemoteIp { get; set; }
        public string? Path { get; set; }
        public string? Query { get; set; }

        // response
        public int StatusCode { get; set; }

        // auth
        public bool? IsAuthenticated { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        // headers
        public string? Referer { get; set; }
        public string? UserAgent { get; set; }
        public string? XForwardedFor { get; set; }

        // app
        public string? Assembly { get; set; }

        // custom claims
        public string? Claims { get; set; }

        // log
        public string Level { get; set; } = "Information";
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? RequestId { get; set; }

        //route 
        public string? Controller { get; set; }
        public string? Action { get; set; }

        //error
        public string? ErrorMessage { get; set; }
        public string? Trace { get; set; }
    }
}
