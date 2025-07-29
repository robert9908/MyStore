using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Entities;
using PaymentService.Interfaces;
using System.Text.Json;

namespace PaymentService.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _context;
        private readonly IPaymentProvider _paymentProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentService(PaymentDbContext context, IPaymentProvider provider, IHttpClientFactory clientFactory)
        {
            _context = context;
            _paymentProvider = provider;
            _httpClientFactory = clientFactory;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(string userId, PaymentRequestDto request)
        {
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                PaidAt = DateTime.UtcNow,
                UserId = userId,
                Status = PaymentStatus.Pending
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var url = await _paymentProvider.CreatePaymentAsync(payment.Id, request.Amount, $"Оплата заказа {request.OrderId}");
            return new PaymentResponseDto { Id = payment.Id, ConfirmationUrl = url };
        }

        public async Task HandleWebhookAsync(JsonDocument payload)
        {
            var type = payload.RootElement.GetProperty("event").GetString();
            if (type != "payment.succeeded") return;

            var paymentId = payload.RootElement.GetProperty("object").GetProperty("metadata").GetProperty("paymentId").GetGuid();
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null || payment.Status == PaymentStatus.Completed) return;

            payment.Status = PaymentStatus.Completed;
            await _context.SaveChangesAsync();

            var client = _httpClientFactory.CreateClient();
            await client.PostAsync($"https://orderservice/api/orders/{payment.OrderId}/mark-paid", null);
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid id, string userId, bool isAdmin)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);
            if (payment == null) return null;
            if (payment.UserId != userId && !isAdmin) return null;
            return payment;
        }

        public async Task RequestRefundAsync(Guid paymentId, string userId, RefundRequestDto dto)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null || payment.UserId != userId || payment.Status != PaymentStatus.Completed)
                throw new InvalidOperationException("Refund not allowed");

            var refund = new RefundRequest
            {
                PaymentId = paymentId,
                Amount = dto.Amount,
                Reason = dto.Reason
            };

            _context.RefundRequests.Add(refund);
            payment.Status = PaymentStatus.Refunded;
            await _paymentProvider.RefundPaymentAsync(paymentId, dto.Amount, dto.Reason);
            await _context.SaveChangesAsync();
        }
    }
}
