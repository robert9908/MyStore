using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Entities;
using PaymentService.Interfaces;

namespace PaymentService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(PaymentDbContext context, ILogger<PaymentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created payment {PaymentId} for user {UserId}", payment.Id, payment.UserId);
        return payment;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByIdAndUserIdAsync(Guid id, string userId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
    }

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated payment {PaymentId}", payment.Id);
        return payment;
    }

    public async Task DeleteAsync(Guid id)
    {
        var payment = await GetByIdAsync(id);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted payment {PaymentId}", id);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Payments.AnyAsync(p => p.Id == id);
    }

    public async Task<bool> IsOwnerAsync(Guid paymentId, string userId)
    {
        return await _context.Payments.AnyAsync(p => p.Id == paymentId && p.UserId == userId);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Payments.CountAsync();
    }

    public async Task<int> GetUserPaymentCountAsync(string userId)
    {
        return await _context.Payments.CountAsync(p => p.UserId == userId);
    }

    public async Task<decimal> GetTotalAmountByUserAsync(string userId)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // Refund operations
    public async Task<Refund> CreateRefundAsync(Refund refund)
    {
        _context.Refunds.Add(refund);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created refund {RefundId} for payment {PaymentId}", refund.Id, refund.PaymentId);
        return refund;
    }

    public async Task<Refund?> GetRefundByIdAsync(Guid id)
    {
        return await _context.Refunds
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Refund?> GetRefundByIdAndUserIdAsync(Guid id, string userId)
    {
        return await _context.Refunds
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id && r.Payment.UserId == userId);
    }

    public async Task<IEnumerable<Refund>> GetRefundsByPaymentIdAsync(Guid paymentId)
    {
        return await _context.Refunds
            .Where(r => r.PaymentId == paymentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Refund>> GetRefundsByUserIdAsync(string userId, int page = 1, int pageSize = 10)
    {
        return await _context.Refunds
            .Include(r => r.Payment)
            .Where(r => r.Payment.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Refund> UpdateRefundAsync(Refund refund)
    {
        refund.UpdatedAt = DateTime.UtcNow;
        _context.Refunds.Update(refund);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated refund {RefundId}", refund.Id);
        return refund;
    }

    public async Task<decimal> GetTotalRefundAmountByPaymentAsync(Guid paymentId)
    {
        return await _context.Refunds
            .Where(r => r.PaymentId == paymentId && r.Status == RefundStatus.Completed)
            .SumAsync(r => r.Amount);
    }
}
