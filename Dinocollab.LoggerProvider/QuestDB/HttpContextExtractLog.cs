using Dinocollab.LoggerProvider.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Dinocollab.LoggerProvider.QuestDB
{
    public class HttpContextExtractLog
    {
        private QuestDBLoggerOption _options;
        private readonly IHttpContextAccessor httpContextAccesory;
        private readonly ILogger _logger;
        private readonly QuestDbLogWorker<HttpContextMessageLog> _logWorker;
        public HttpContextExtractLog(
            IOptions<QuestDBLoggerOption> options,
            IHttpContextAccessor httpContextAccessor,
            ILogger<HttpContextExtractLog> logger,
            QuestDbLogWorker<HttpContextMessageLog> questDbLogWorker)
        {
            _options = options.Value;
            httpContextAccesory = httpContextAccessor;
            _logger = logger;
            Validate().Any();
            _logWorker = questDbLogWorker;
        }
        public HttpContext? HttpContext { get => httpContextAccesory.HttpContext; }
        public const long MaxBodySize = (long)(0.5 * 1024 * 1024);
        public  IEnumerable<bool> Validate()
        {
            if (_options == null)
            {
                _logger.LogWarning($"{nameof(QuestDBLoggerOption)} is empty!");
                yield return false;
            }
            else
            {
                if (!_options.IsResponseBody)
                {
                    _logger.LogWarning($"LokiLogger ResponseBody is disable!");
                }

                if (!_options.IsRequestBody)
                {
                    _logger.LogWarning($"LokiLogger RequestBody is disabled!");
                }
            }
            yield return true;
        }
        public async Task LogAsync(Func<Task>? next = null, Exception? error = null, long maxBodySize = MaxBodySize)
        {
            if (HttpContext == null)
            {
                _logger.LogWarning("HttpContext is null, cannot log request/response.");
                return;
            }
            try
            {
                //get request body
                var bodyRequest = null as string;
                if (_options.IsRequestBody)
                {
                    HttpContext.Request.EnableBuffering(); // Enable buffering so the request body can be read multiple times
                    bodyRequest = await ReadBodyAsync(HttpContext.Request.Body, maxBodySize);

                    if (HttpContext.Request.Method == "POST"
                        && (bodyRequest.Contains("password", StringComparison.OrdinalIgnoreCase)
                        || bodyRequest.Contains("username") || bodyRequest.Contains("email")))
                    {
                        bodyRequest = null;
                    }
                }

                //get response body
                var bodyResponse = null as string;
                if (next != null)
                {
                    if (_options.IsResponseBody)
                    {
                        bodyResponse = await ReadResponseBodyAsync(HttpContext, next);
                    }
                    else
                    {
                        await next();
                    }
                }

                var contextData = HttpContext.CreateContextData();
                contextData.RequestBody = bodyRequest;
                contextData.ResponseBody = bodyResponse;

                if (error != null)
                {
                    contextData.Level = "Error";
                    contextData.ErrorMessage = error.Message;
                    contextData.Trace = error.StackTrace;
                }
                else
                {
                    contextData.Level = "Information";
                }
                _logWorker.TryEnqueue(contextData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
                {
                    throw new Exception("error when log http context");
                }
                else
                {
                    throw;
                }
            }
        }
        public const string ResponseBody = "ResponseBody";
        public const string RequestBody = "RequestBody";

        // Optional method to read and include the request body separately
        private async Task<string> ReadBodyAsync(Stream stream, long maxSize = MaxBodySize)
        {
            // Buffer for reading chunks
            var buffer = new char[4096];
            var totalRead = 0;
            var responseBodyBuilder = new System.Text.StringBuilder();
            stream.Seek(0, SeekOrigin.Begin);
            // Create a StreamReader for the stream
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                int bytesRead;
                while ((bytesRead = await reader.ReadBlockAsync(buffer, 0, buffer.Length)) > 0)
                {
                    totalRead += bytesRead;

                    // If maxSize is specified and the totalRead exceeds it, truncate the response body
                    if (maxSize > 0 && totalRead > maxSize)
                    {
                        // Append only the allowed portion and break the loop
                        responseBodyBuilder.Append(buffer, 0, bytesRead - (totalRead - (int)maxSize));
                        break;
                    }
                    else
                    {
                        responseBodyBuilder.Append(buffer, 0, bytesRead);
                    }
                }

                // Rewind the stream for further processing
                stream.Seek(0, SeekOrigin.Begin);
                //stream.Position = 0;

                return responseBodyBuilder.ToString();
            }
        }
        // Optional method to read and include the request body separately
        public  async Task<string?> ReadResponseBodyAsync( HttpContext context, Func<Task> next, long maxSize = MaxBodySize)
        {
            // Store the original response body stream
            var originalBodyStream = context.Response.Body;
            string? responseBody = null;
            // Create a new memory stream to capture the response body
            using var newBodyStream = new MemoryStream();
            context.Response.Body = newBodyStream;

            await next();

            try
            {
                responseBody = await ReadBodyAsync(newBodyStream, maxSize);

                // Copy the content of the memory stream to the original response stream
                await newBodyStream.CopyToAsync(originalBodyStream);
                // Optionally, modify the response body here
            }
            catch { }
            finally
            {
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
            }
            return responseBody;
        }
        public ClaimsPrincipal CreateAuthenticatedUser(string name, string authenticationType = "Custom")
        {
            // Define claims
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                // Add more claims as needed
            };

            // Create an identity with the claims and authentication type
            var identity = new ClaimsIdentity(claims, authenticationType);

            // Create a ClaimsPrincipal with the identity
            var principal = new ClaimsPrincipal(identity);

            return principal;
        }
    }
}
