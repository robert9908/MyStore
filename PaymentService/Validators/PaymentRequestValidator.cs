using FluentValidation;
using PaymentService.DTOs;

namespace PaymentService.Validators;

public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("Amount cannot exceed 999,999.99");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters")
            .Must(BeValidCurrency)
            .WithMessage("Invalid currency code");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .MaximumLength(50)
            .WithMessage("Payment method cannot exceed 50 characters")
            .Must(BeValidPaymentMethod)
            .WithMessage("Invalid payment method");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Metadata)
            .MaximumLength(2000)
            .WithMessage("Metadata cannot exceed 2000 characters");
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "USD", "EUR", "GBP", "RUB", "CAD", "AUD", "JPY", "CHF" };
        return validCurrencies.Contains(currency.ToUpper());
    }

    private static bool BeValidPaymentMethod(string paymentMethod)
    {
        var validMethods = new[] { "credit_card", "debit_card", "paypal", "stripe", "bank_transfer", "apple_pay", "google_pay" };
        return validMethods.Contains(paymentMethod.ToLower());
    }
}
