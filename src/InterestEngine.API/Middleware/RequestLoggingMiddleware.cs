namespace InterestEngine.API.Middleware;

/// <summary>
/// Middleware that logs every incoming request and its response status code.
/// Demonstrates custom ASP.NET Core middleware pipeline usage.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = DateTime.UtcNow;

        _logger.LogInformation(
            "[REQUEST]  {Method} {Path} at {Time}",
            context.Request.Method,
            context.Request.Path,
            start.ToString("HH:mm:ss.fff"));

        await _next(context);

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

        _logger.LogInformation(
            "[RESPONSE] {Method} {Path} → {StatusCode} ({Elapsed}ms)",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsed.ToString("F1"));
    }
}
