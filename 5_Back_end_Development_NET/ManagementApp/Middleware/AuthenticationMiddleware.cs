using System.Text.Json;
using ManagementApp.Services;

namespace ManagementApp.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        RequestDelegate next,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the endpoint requires authentication
        var endpoint = context.GetEndpoint();
        var requiresAuth = endpoint?.Metadata.GetOrderedMetadata<RequireAuthenticationAttribute>().Any() ?? false;

        // Skip authentication for endpoints that don't require it
        if (!requiresAuth)
        {
            await _next(context);
            return;
        }

        // Get token validator from request scope
        var tokenValidator = context.RequestServices.GetRequiredService<ITokenValidator>();

        // Validate token
        var token = ExtractToken(context);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Missing authorization token for {Method} {Path}", context.Request.Method, context.Request.Path);
            RespondWithUnauthorized(context, "Missing authorization token.");
            return;
        }

        var isValid = await tokenValidator.ValidateTokenAsync(token);

        if (!isValid)
        {
            _logger.LogWarning("Invalid authorization token for {Method} {Path}", context.Request.Method, context.Request.Path);
            RespondWithUnauthorized(context, "Invalid authorization token.");
            return;
        }

        // Token is valid, proceed
        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return null;
        }

        const string bearerScheme = "Bearer ";

        if (!authHeader.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring(bearerScheme.Length).Trim();
    }

    private static void RespondWithUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var errorResponse = new { message };
        var json = JsonSerializer.Serialize(errorResponse);
        context.Response.WriteAsJsonAsync(errorResponse);
    }
}

/// <summary>
/// Attribute to mark endpoints that require authentication.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthenticationAttribute : Attribute
{
}
