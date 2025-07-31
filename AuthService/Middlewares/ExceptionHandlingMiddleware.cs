using AuthService.Exceptions;
using System.Net;
using System.Text.Json;

namespace AuthService.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Unhanled Exception");
                
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;
            string errorCode = "SERVER_ERROR";
            string message = "An unexpected error occurred";
            object errors = null;

            if (exception is ApiException apiEx)
            {
                statusCode = apiEx.StatusCode;
                errorCode = apiEx.ErrorCode;
                message = apiEx.Message;

                if (apiEx is ValidationException ve)
                {
                    errors = ve.Errors;
                }
            }

            context.Response.StatusCode = statusCode;

            var response = new
            {
                error = new
                {
                    code = errorCode,
                    message,
                    errors
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
