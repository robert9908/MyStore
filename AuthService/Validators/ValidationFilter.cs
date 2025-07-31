using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Validators
{
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var arg in context.ActionArguments)
            {
                if (arg.Value is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(arg.Value.GetType());
                var validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator == null) continue;

                var validationContext = new ValidationContext<object>(arg.Value);
                var result = await validator.ValidateAsync(validationContext);

                if (!result.IsValid)
                {
                    var errors = result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    var problemDetails = new ValidationProblemDetails(errors)
                    {
                        Title = "Validation failed",
                        Status = StatusCodes.Status400BadRequest,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    };

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }

            await next();
        }
    }
}
