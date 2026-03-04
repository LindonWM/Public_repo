using System.Text;

namespace ManagementApp.Middleware;

public class HttpLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpLoggingMiddleware> _logger;

    public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log incoming request
        await LogRequestAsync(context);

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                // Call the next middleware
                await _next(context);
            }
            finally
            {
                // Log outgoing response
                await LogResponseAsync(context, responseBody);

                // Copy the response body back to the original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
    }

    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var hasBody = request.ContentLength.HasValue && request.ContentLength > 0;

        var requestLog = new StringBuilder();
        requestLog.AppendLine($"[REQUEST] {request.Method} {request.Path}{request.QueryString}");
        requestLog.AppendLine($"  Scheme: {request.Scheme}");
        requestLog.AppendLine($"  Host: {request.Host}");
        requestLog.AppendLine($"  Content-Type: {request.ContentType}");
        requestLog.AppendLine($"  Content-Length: {request.ContentLength}");

        // Log headers (excluding sensitive ones)
        if (request.Headers.Count > 0)
        {
            requestLog.AppendLine("  Headers:");
            foreach (var header in request.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    requestLog.AppendLine($"    {header.Key}: {string.Join(", ", header.Value.ToArray())}");
                }
            }
        }

        // Log request body for small payloads
        if (hasBody && request.ContentType?.Contains("application/json") == true && request.ContentLength < 10240)
        {
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    requestLog.AppendLine($"  Body: {body}");
                }
            }
            request.Body.Position = 0;
        }

        _logger.LogInformation(requestLog.ToString());
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream responseBody)
    {
        var response = context.Response;
        responseBody.Seek(0, SeekOrigin.Begin);

        var responseLog = new StringBuilder();
        responseLog.AppendLine($"[RESPONSE] {response.StatusCode} {context.Request.Method} {context.Request.Path}");
        responseLog.AppendLine($"  Content-Type: {response.ContentType}");
        responseLog.AppendLine($"  Content-Length: {response.ContentLength}");

        // Log headers
        if (response.Headers.Count > 0)
        {
            responseLog.AppendLine("  Headers:");
            foreach (var header in response.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    responseLog.AppendLine($"    {header.Key}: {string.Join(", ", header.Value.ToArray())}");
                }
            }
        }

        // Log response body for small payloads
        if (response.ContentLength.HasValue && response.ContentLength < 10240 && response.ContentType?.Contains("application/json") == true)
        {
            using (var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    responseLog.AppendLine($"  Body: {body}");
                }
            }
            responseBody.Seek(0, SeekOrigin.Begin);
        }

        _logger.LogInformation(responseLog.ToString());
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[] 
        { 
            "Authorization", 
            "Cookie", 
            "X-API-Key", 
            "X-Token",
            "X-Auth-Token",
            "Password"
        };

        return sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}
