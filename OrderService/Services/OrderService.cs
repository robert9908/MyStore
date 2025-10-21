using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using OrderService.DTOs;
using OrderService.Entities;
using OrderService.Interfaces;
using System.Text.Json;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IValidator<CreateOrderRequest> _createOrderValidator;

    public OrderService(
        IOrderRepository orderRepository,
        IMapper mapper,
        ILogger<OrderService> logger,
        IDistributedCache cache,
        IValidator<CreateOrderRequest> createOrderValidator)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
        _createOrderValidator = createOrderValidator;
    }

    public async Task<OrderResponse> CreateOrderAsync(string userId, CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order for user {UserId}", userId);

        // Validate request
        var validationResult = await _createOrderValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Order validation failed for user {UserId}: {Errors}", userId, errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Calculate and verify total
        var calculatedTotal = await CalculateOrderTotalAsync(request.Items, request.TaxAmount, request.ShippingCost);
        if (Math.Abs(request.TotalAmount - calculatedTotal) > 0.01m)
        {
            _logger.LogWarning("Order total mismatch for user {UserId}. Expected: {Expected}, Provided: {Provided}", 
                userId, calculatedTotal, request.TotalAmount);
            throw new ArgumentException("Order total does not match calculated amount");
        }

        // Create order entity
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = request.TotalAmount,
            ShippingCost = request.ShippingCost,
            TaxAmount = request.TaxAmount,
            ShippingAddress = request.ShippingAddress,
            PaymentMethod = request.PaymentMethod,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(item => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductDescription = item.ProductDescription,
                ProductImageUrl = item.ProductImageUrl,
                ProductSku = item.ProductSku,
                Quantity = item.Quantity,
                Price = item.Price,
                DiscountAmount = item.DiscountAmount
            }).ToList()
        };

        var createdOrder = await _orderRepository.CreateAsync(order);
        
        // Clear user orders cache
        await InvalidateUserOrdersCacheAsync(userId);
        
        _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", createdOrder.Id, userId);
        
        return _mapper.Map<OrderResponse>(createdOrder);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, string? userId = null)
    {
        _logger.LogDebug("Getting order {OrderId} for user {UserId}", orderId, userId);

        var cacheKey = $"order:{orderId}";
        var cachedOrder = await _cache.GetStringAsync(cacheKey);
        
        if (cachedOrder != null)
        {
            var orderResponse = JsonSerializer.Deserialize<OrderResponse>(cachedOrder);
            if (orderResponse != null && (userId == null || orderResponse.UserId == userId))
            {
                return orderResponse;
            }
        }

        var order = await _orderRepository.GetByIdWithItemsAsync(orderId);
        if (order == null || (userId != null && order.UserId != userId))
        {
            return null;
        }

        var response = _mapper.Map<OrderResponse>(order);
        
        // Cache for 5 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), cacheOptions);

        return response;
    }

    public async Task<List<OrderResponse>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20)
    {
        _logger.LogDebug("Getting orders for user {UserId}, page {Page}, pageSize {PageSize}", userId, page, pageSize);

        var cacheKey = $"user_orders:{userId}:page:{page}:size:{pageSize}";
        var cachedOrders = await _cache.GetStringAsync(cacheKey);
        
        if (cachedOrders != null)
        {
            var orderResponses = JsonSerializer.Deserialize<List<OrderResponse>>(cachedOrders);
            if (orderResponses != null)
            {
                return orderResponses;
            }
        }

        var orders = await _orderRepository.GetByUserIdAsync(userId, page, pageSize);
        var responses = _mapper.Map<List<OrderResponse>>(orders);
        
        // Cache for 2 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(responses), cacheOptions);

        return responses;
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 20)
    {
        _logger.LogDebug("Getting all orders, page {Page}, pageSize {PageSize}", page, pageSize);

        var orders = await _orderRepository.GetAllAsync(page, pageSize);
        return _mapper.Map<List<OrderResponse>>(orders);
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, string? adminUserId = null)
    {
        _logger.LogInformation("Updating order {OrderId} status to {Status} by {AdminUserId}", orderId, newStatus, adminUserId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", orderId);
            return false;
        }

        // Validate status transition
        if (!IsValidStatusTransition(order.Status, newStatus))
        {
            _logger.LogWarning("Invalid status transition from {OldStatus} to {NewStatus} for order {OrderId}", 
                order.Status, newStatus, orderId);
            return false;
        }

        order.Status = newStatus;
        await _orderRepository.UpdateAsync(order);
        
        // Clear caches
        await InvalidateOrderCacheAsync(orderId);
        await InvalidateUserOrdersCacheAsync(order.UserId);
        
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);
        return true;
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, string userId)
    {
        _logger.LogInformation("Cancelling order {OrderId} for user {UserId}", orderId, userId);

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId)
        {
            _logger.LogWarning("Order {OrderId} not found or not owned by user {UserId}", orderId, userId);
            return false;
        }

        if (!order.CanBeCancelled)
        {
            _logger.LogWarning("Order {OrderId} cannot be cancelled in status {Status}", orderId, order.Status);
            return false;
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order);
        
        // Clear caches
        await InvalidateOrderCacheAsync(orderId);
        await InvalidateUserOrdersCacheAsync(userId);
        
        _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
        return true;
    }

    public async Task<decimal> CalculateOrderTotalAsync(List<OrderItemDto> items, decimal taxAmount, decimal shippingCost)
    {
        if (items == null || !items.Any())
            return 0;

        var itemsTotal = items.Sum(item => (item.Price * item.Quantity) - item.DiscountAmount);
        return itemsTotal + taxAmount + shippingCost;
    }

    public async Task<bool> ValidateOrderOwnershipAsync(Guid orderId, string userId)
    {
        return await _orderRepository.IsOwnerAsync(orderId, userId);
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
        {
            [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new() { OrderStatus.Delivered },
            [OrderStatus.Delivered] = new(), // Final state
            [OrderStatus.Cancelled] = new() // Final state
        };

        return validTransitions.ContainsKey(currentStatus) && 
               validTransitions[currentStatus].Contains(newStatus);
    }

    private async Task InvalidateOrderCacheAsync(Guid orderId)
    {
        await _cache.RemoveAsync($"order:{orderId}");
    }

    private async Task InvalidateUserOrdersCacheAsync(string userId)
    {
        // In a real implementation, you might want to use a more sophisticated cache invalidation strategy
        // For now, we'll just remove some common cache keys
        for (int page = 1; page <= 10; page++)
        {
            await _cache.RemoveAsync($"user_orders:{userId}:page:{page}:size:20");
        }
    }
}
