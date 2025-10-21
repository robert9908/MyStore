using Grpc.Core;
using MyStore.Shared.Grpc.Order;
using MyStore.OrderService.Interfaces;
using MyStore.OrderService.Entities;
using System.Diagnostics;
using MyStore.Shared.Tracing;
using AutoMapper;

namespace MyStore.OrderService.GrpcServices;

public class OrderGrpcService : Order.OrderBase
{
    private readonly IOrderService _orderService;
    private readonly IOrderRepository _orderRepository;
    private readonly ISecurityService _securityService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(
        IOrderService orderService,
        IOrderRepository orderRepository,
        ISecurityService securityService,
        IMapper mapper,
        ILogger<OrderGrpcService> logger)
    {
        _orderService = orderService;
        _orderRepository = orderRepository;
        _securityService = securityService;
        _mapper = mapper;
        _logger = logger;
    }

    public override async Task<GetOrderByIdResponse> GetOrderById(
        GetOrderByIdRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderService.GetOrderById");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("requesting.user.id", request.RequestingUserId);

        try
        {
            _logger.LogDebug("Getting order {OrderId} for user {UserId}", request.OrderId, request.RequestingUserId);

            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new GetOrderByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Invalid order ID format"
                };
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return new GetOrderByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Order not found"
                };
            }

            // Check ownership
            if (!await _securityService.CanAccessOrderAsync(request.RequestingUserId, orderId))
            {
                return new GetOrderByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Access denied"
                };
            }

            var grpcOrder = MapToGrpcOrder(order);
            return new GetOrderByIdResponse
            {
                Found = true,
                Order = grpcOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetOrderByIdResponse
            {
                Found = false,
                ErrorMessage = "Failed to retrieve order"
            };
        }
    }

    public override async Task<UpdateOrderStatusResponse> UpdateOrderStatus(
        UpdateOrderStatusRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderService.UpdateOrderStatus");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("new.status", request.Status.ToString());

        try
        {
            _logger.LogInformation("Updating order {OrderId} status to {Status}", request.OrderId, request.Status);

            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new UpdateOrderStatusResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid order ID format"
                };
            }

            var orderStatus = MapFromGrpcOrderStatus(request.Status);
            await _orderService.UpdateOrderStatusAsync(orderId, orderStatus, request.PaymentId, request.Notes);

            _logger.LogInformation("Order {OrderId} status updated successfully", request.OrderId);
            
            return new UpdateOrderStatusResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new UpdateOrderStatusResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<ValidateOrderOwnershipResponse> ValidateOrderOwnership(
        ValidateOrderOwnershipRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderService.ValidateOrderOwnership");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("user.id", request.UserId);

        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new ValidateOrderOwnershipResponse
                {
                    IsOwner = false,
                    ErrorMessage = "Invalid order ID format"
                };
            }

            var isOwner = await _securityService.CanAccessOrderAsync(request.UserId, orderId);
            
            return new ValidateOrderOwnershipResponse
            {
                IsOwner = isOwner
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order ownership for {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidateOrderOwnershipResponse
            {
                IsOwner = false,
                ErrorMessage = "Validation failed"
            };
        }
    }

    public override async Task<GetOrderTotalResponse> GetOrderTotal(
        GetOrderTotalRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderService.GetOrderTotal");
        activity?.SetTag("order.id", request.OrderId);

        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new GetOrderTotalResponse
                {
                    Found = false
                };
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return new GetOrderTotalResponse
                {
                    Found = false
                };
            }

            return new GetOrderTotalResponse
            {
                Found = true,
                TotalAmount = (double)order.TotalAmount,
                Currency = "USD" // Default currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order total for {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetOrderTotalResponse
            {
                Found = false
            };
        }
    }

    public override async Task<CancelOrderResponse> CancelOrder(
        CancelOrderRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderService.CancelOrder");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("requesting.user.id", request.RequestingUserId);

        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new CancelOrderResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid order ID format"
                };
            }

            // Check ownership
            if (!await _securityService.CanAccessOrderAsync(request.RequestingUserId, orderId))
            {
                return new CancelOrderResponse
                {
                    Success = false,
                    ErrorMessage = "Access denied"
                };
            }

            await _orderService.CancelOrderAsync(orderId, request.Reason);
            
            return new CancelOrderResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CancelOrderResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static Order MapToGrpcOrder(Entities.Order order)
    {
        return new Order
        {
            Id = order.Id.ToString(),
            UserId = order.UserId,
            Status = MapToGrpcOrderStatus(order.Status),
            TotalAmount = (double)order.TotalAmount,
            ShippingCost = (double)order.ShippingCost,
            TaxAmount = (double)order.TaxAmount,
            ShippingAddress = order.ShippingAddress ?? string.Empty,
            PaymentMethod = order.PaymentMethod ?? string.Empty,
            Notes = order.Notes ?? string.Empty,
            Items = { order.Items.Select(MapToGrpcOrderItem) },
            CreatedAt = order.CreatedAt.Ticks,
            UpdatedAt = order.UpdatedAt.Ticks
        };
    }

    private static OrderItem MapToGrpcOrderItem(Entities.OrderItem item)
    {
        return new OrderItem
        {
            Id = item.Id.ToString(),
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductDescription = item.ProductDescription ?? string.Empty,
            ProductImageUrl = item.ProductImageUrl ?? string.Empty,
            ProductSku = item.ProductSku ?? string.Empty,
            Quantity = item.Quantity,
            Price = (double)item.Price,
            DiscountAmount = (double)item.DiscountAmount
        };
    }

    private static OrderStatus MapToGrpcOrderStatus(Entities.OrderStatus status)
    {
        return status switch
        {
            Entities.OrderStatus.Pending => OrderStatus.OrderStatusPending,
            Entities.OrderStatus.Confirmed => OrderStatus.OrderStatusConfirmed,
            Entities.OrderStatus.Processing => OrderStatus.OrderStatusProcessing,
            Entities.OrderStatus.Shipped => OrderStatus.OrderStatusShipped,
            Entities.OrderStatus.Delivered => OrderStatus.OrderStatusDelivered,
            Entities.OrderStatus.Cancelled => OrderStatus.OrderStatusCancelled,
            Entities.OrderStatus.Refunded => OrderStatus.OrderStatusRefunded,
            _ => OrderStatus.OrderStatusUnspecified
        };
    }

    private static Entities.OrderStatus MapFromGrpcOrderStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.OrderStatusPending => Entities.OrderStatus.Pending,
            OrderStatus.OrderStatusConfirmed => Entities.OrderStatus.Confirmed,
            OrderStatus.OrderStatusProcessing => Entities.OrderStatus.Processing,
            OrderStatus.OrderStatusShipped => Entities.OrderStatus.Shipped,
            OrderStatus.OrderStatusDelivered => Entities.OrderStatus.Delivered,
            OrderStatus.OrderStatusCancelled => Entities.OrderStatus.Cancelled,
            OrderStatus.OrderStatusRefunded => Entities.OrderStatus.Refunded,
            _ => Entities.OrderStatus.Pending
        };
    }
}
