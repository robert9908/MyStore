using OrderService.DTOs;

namespace OrderService.Interfaces
{
    public interface IOrderService
    {
        Task<Guid> CreateOrderAsync(string userId, CreateOrderRequest request);
        Task<List<OrderResponse>> GetUserOrdersAsync(string userId);
        Task<List<OrderResponse>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(Guid orderId, string newStsatus);
    }
}
