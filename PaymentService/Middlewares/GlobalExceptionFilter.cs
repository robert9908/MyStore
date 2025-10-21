using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using System.Net;

namespace PaymentService.Middlewares;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var correlationId = context.HttpContext.Items["X-Correlation-ID"]?.ToString() ?? "N/A";
        
        _logger.LogError(context.Exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

        var response = context.Exception switch
        {
            ValidationException validationEx => new ObjectResult(new
            {
                error = "Validation failed",
                details = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }),
                correlationId
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            
            UnauthorizedAccessException => new ObjectResult(new
            {
                error = "Access denied",
                message = "You don't have permission to access this resource",
                correlationId
            })
            {
                StatusCode = (int)HttpStatusCode.Forbidden
            },
            
            ArgumentException argEx => new ObjectResult(new
            {
                error = "Invalid argument",
                message = argEx.Message,
                correlationId
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            },
            
            KeyNotFoundException => new ObjectResult(new
            {
                error = "Resource not found",
                message = "The requested resource was not found",
                correlationId
            })
            {
                StatusCode = (int)HttpStatusCode.NotFound
            },
            
            _ => new ObjectResult(new
            {
                error = "Internal server error",
                message = "An unexpected error occurred",
                correlationId
            })
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            }
        };

        context.Result = response;
        context.ExceptionHandled = true;
    }
}
