namespace PaymentService.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Add to response headers
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        // Add to HttpContext items for use in controllers/services
        context.Items[CorrelationIdHeader] = correlationId;
        
        await _next(context);
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        return context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
    }
}
