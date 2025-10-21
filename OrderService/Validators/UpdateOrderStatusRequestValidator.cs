using FluentValidation;
using OrderService.DTOs;
using OrderService.Entities;

namespace OrderService.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid order status");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters");

        // Validate status transitions
        RuleFor(x => x.Status)
            .Must(BeValidStatusTransition)
            .WithMessage("Invalid status transition")
            .When(x => x.Status != OrderStatus.Pending);
    }

    private static bool BeValidStatusTransition(OrderStatus newStatus)
    {
        // Define valid status transitions
        var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
        {
            [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new() { OrderStatus.Delivered },
            [OrderStatus.Delivered] = new(), // Final state
            [OrderStatus.Cancelled] = new() // Final state
        };

        return validTransitions.ContainsKey(newStatus);
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters");
    }
}
