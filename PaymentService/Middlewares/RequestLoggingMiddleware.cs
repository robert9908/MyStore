using Serilog.Context;

namespace PaymentService.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            _logger.LogInformation("Processing request {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            await _next(context);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Completed request {Method} {Path} with status {StatusCode} in {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
