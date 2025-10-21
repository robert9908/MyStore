using PaymentService.Entities;

namespace PaymentService.Interfaces;

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByIdAndUserIdAsync(Guid id, string userId);
    Task<IEnumerable<Payment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<Payment>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status, int page = 1, int pageSize = 10);
    Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId);
    Task<Payment> UpdateAsync(Payment payment);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> IsOwnerAsync(Guid paymentId, string userId);
    Task<int> GetTotalCountAsync();
    Task<int> GetUserPaymentCountAsync(string userId);
    Task<decimal> GetTotalAmountByUserAsync(string userId);
    Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10);
    
    // Refund operations
    Task<Refund> CreateRefundAsync(Refund refund);
    Task<Refund?> GetRefundByIdAsync(Guid id);
    Task<Refund?> GetRefundByIdAndUserIdAsync(Guid id, string userId);
    Task<IEnumerable<Refund>> GetRefundsByPaymentIdAsync(Guid paymentId);
    Task<IEnumerable<Refund>> GetRefundsByUserIdAsync(string userId, int page = 1, int pageSize = 10);
    Task<Refund> UpdateRefundAsync(Refund refund);
    Task<decimal> GetTotalRefundAmountByPaymentAsync(Guid paymentId);
}
