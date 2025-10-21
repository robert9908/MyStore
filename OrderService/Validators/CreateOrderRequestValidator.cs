using FluentValidation;
using OrderService.DTOs;

namespace OrderService.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleFor(x => x.Items)
            .Must(items => items.Count <= 50)
            .WithMessage("Order cannot contain more than 50 items");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemDtoValidator());

        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage("Shipping address is required")
            .MaximumLength(200)
            .WithMessage("Shipping address cannot exceed 200 characters");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required")
            .MaximumLength(100)
            .WithMessage("Payment method cannot exceed 100 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Tax amount cannot be negative");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Shipping cost cannot be negative");

        // Validate total amount matches sum of items
        RuleFor(x => x)
            .Must(ValidateTotalAmount)
            .WithMessage("Total amount does not match the sum of item prices plus tax and shipping");
    }

    private static bool ValidateTotalAmount(CreateOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            return false;

        var itemsTotal = request.Items.Sum(item => item.Price * item.Quantity - item.DiscountAmount);
        var expectedTotal = itemsTotal + request.TaxAmount + request.ShippingCost;
        
        // Allow for small rounding differences
        return Math.Abs(request.TotalAmount - expectedTotal) < 0.01m;
    }
}

public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
{
    public OrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required")
            .MaximumLength(100)
            .WithMessage("Product ID cannot exceed 100 characters");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(999999.99m)
            .WithMessage("Price cannot exceed 999,999.99");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Discount amount cannot be negative");

        RuleFor(x => x)
            .Must(item => item.DiscountAmount <= item.Price * item.Quantity)
            .WithMessage("Discount amount cannot exceed the total item price");

        RuleFor(x => x.ProductSku)
            .MaximumLength(50)
            .WithMessage("Product SKU cannot exceed 50 characters");
    }
}
