using OrderService.DTOs;
using OrderService.Entities;

namespace OrderService.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(string userId, CreateOrderRequest request);
    Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, string? userId = null);
    Task<List<OrderResponse>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 20);
    Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, string? adminUserId = null);
    Task<bool> CancelOrderAsync(Guid orderId, string userId);
    Task<decimal> CalculateOrderTotalAsync(List<OrderItemDto> items, decimal taxAmount, decimal shippingCost);
    Task<bool> ValidateOrderOwnershipAsync(Guid orderId, string userId);
}
