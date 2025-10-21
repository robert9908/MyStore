using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;
using OrderService.Interfaces;

namespace OrderService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await GetByIdAsync(id);
        if (order == null)
            return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }

    public async Task<bool> IsOwnerAsync(Guid orderId, string userId)
    {
        return await _context.Orders
            .AnyAsync(o => o.Id == orderId && o.UserId == userId);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Orders.CountAsync();
    }

    public async Task<int> GetUserOrderCountAsync(string userId)
    {
        return await _context.Orders
            .CountAsync(o => o.UserId == userId);
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 20)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 20)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
