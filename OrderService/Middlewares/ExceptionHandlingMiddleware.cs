using System.Net;
using System.Text.Json;
using FluentValidation;

namespace OrderService.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.Message = "Validation failed";
                response.Details = validationEx.Errors.Select(e => e.ErrorMessage).ToArray();
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException:
                response.Message = "Unauthorized access";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case KeyNotFoundException:
                response.Message = "Resource not found";
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case ArgumentException argEx:
                response.Message = argEx.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case InvalidOperationException invalidOpEx:
                response.Message = invalidOpEx.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                response.Message = "An internal server error occurred";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        response.StatusCode = context.Response.StatusCode;
        response.Timestamp = DateTime.UtcNow;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Details { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; }
}
