using PaymentService.DTOs;
using PaymentService.Entities;
using System.Text.Json;

namespace PaymentService.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(string userId, PaymentRequestDto request);
        Task HandleWebhookAsync(JsonDocument payload);
        Task<Payment?> GetPaymentByIdAsync(Guid id, string userId, bool isAdmin);
        Task RequestRefundAsync(Guid paymentId, string userId, RefundRequestDto request);
    }
}
