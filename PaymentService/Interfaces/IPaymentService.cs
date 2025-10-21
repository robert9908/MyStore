using PaymentService.DTOs;
using PaymentService.Entities;
using System.Text.Json;

namespace PaymentService.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(string userId, PaymentRequestDto request);
    Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid id, string userId);
    Task<IEnumerable<PaymentResponseDto>> GetUserPaymentsAsync(string userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync(int page = 1, int pageSize = 10);
    Task<PaymentResponseDto> UpdatePaymentStatusAsync(Guid id, PaymentStatus status, string? transactionId = null);
    Task<RefundResponseDto> CreateRefundAsync(Guid paymentId, string userId, RefundRequestDto request);
    Task<RefundResponseDto?> GetRefundByIdAsync(Guid id, string userId);
    Task<IEnumerable<RefundResponseDto>> GetPaymentRefundsAsync(Guid paymentId, string userId);
    Task HandleWebhookAsync(JsonDocument payload);
    Task<bool> ValidatePaymentOwnershipAsync(Guid paymentId, string userId);
    Task<decimal> CalculateRefundAmountAsync(Guid paymentId, decimal requestedAmount);
}
