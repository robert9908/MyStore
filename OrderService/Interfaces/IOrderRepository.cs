using OrderService.Entities;

namespace OrderService.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdWithItemsAsync(Guid id);
    Task<List<Order>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<Order>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<Order> UpdateAsync(Order order);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> IsOwnerAsync(Guid orderId, string userId);
    Task<int> GetTotalCountAsync();
    Task<int> GetUserOrderCountAsync(string userId);
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 20);
    Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20);
}
