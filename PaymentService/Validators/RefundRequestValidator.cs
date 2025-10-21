using FluentValidation;
using PaymentService.DTOs;

namespace PaymentService.Validators;

public class RefundRequestValidator : AbstractValidator<RefundRequestDto>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Refund amount must be greater than 0")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("Refund amount cannot exceed 999,999.99");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Refund reason is required")
            .MinimumLength(10)
            .WithMessage("Refund reason must be at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Refund reason cannot exceed 500 characters");
    }
}
