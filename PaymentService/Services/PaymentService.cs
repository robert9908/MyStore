using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Distributed;
using PaymentService.DTOs;
using PaymentService.Entities;
using PaymentService.Interfaces;
using System.Text.Json;

namespace PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IValidator<PaymentRequestDto> _paymentValidator;
    private readonly IValidator<RefundRequestDto> _refundValidator;
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IMapper mapper,
        ILogger<PaymentService> logger,
        IDistributedCache cache,
        IValidator<PaymentRequestDto> paymentValidator,
        IValidator<RefundRequestDto> refundValidator,
        IHttpClientFactory httpClientFactory)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
        _paymentValidator = paymentValidator;
        _refundValidator = refundValidator;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(string userId, PaymentRequestDto request)
    {
        _logger.LogInformation("Creating payment for user {UserId}, order {OrderId}", userId, request.OrderId);

        // Validate request
        var validationResult = await _paymentValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Payment validation failed for user {UserId}: {Errors}", userId, errors);
            throw new ValidationException(validationResult.Errors);
        }

        // Create payment entity
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = userId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdPayment = await _paymentRepository.CreateAsync(payment);
        
        // Clear user payments cache
        await InvalidateUserPaymentsCacheAsync(userId);
        
        _logger.LogInformation("Payment {PaymentId} created successfully for user {UserId}", createdPayment.Id, userId);
        
        return _mapper.Map<PaymentResponseDto>(createdPayment);
    }

    public async Task<PaymentResponseDto?> GetPaymentByIdAsync(Guid id, string userId)
    {
        var cacheKey = $"payment:{id}:{userId}";
        var cachedPayment = await GetFromCacheAsync<PaymentResponseDto>(cacheKey);
        if (cachedPayment != null)
        {
            return cachedPayment;
        }

        var payment = await _paymentRepository.GetByIdAndUserIdAsync(id, userId);
        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found for user {UserId}", id, userId);
            return null;
        }

        var response = _mapper.Map<PaymentResponseDto>(payment);
        await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(15));
        
        return response;
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetUserPaymentsAsync(string userId, int page = 1, int pageSize = 10)
    {
        var cacheKey = $"user_payments:{userId}:page:{page}:size:{pageSize}";
        var cachedPayments = await GetFromCacheAsync<IEnumerable<PaymentResponseDto>>(cacheKey);
        if (cachedPayments != null)
        {
            return cachedPayments;
        }

        var payments = await _paymentRepository.GetByUserIdAsync(userId, page, pageSize);
        var response = _mapper.Map<IEnumerable<PaymentResponseDto>>(payments);
        
        await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return response;
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetAllPaymentsAsync(int page = 1, int pageSize = 10)
    {
        var payments = await _paymentRepository.GetAllAsync(page, pageSize);
        return _mapper.Map<IEnumerable<PaymentResponseDto>>(payments);
    }

    public async Task<PaymentResponseDto> UpdatePaymentStatusAsync(Guid id, PaymentStatus status, string? transactionId = null)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment {id} not found");
        }

        if (!IsValidStatusTransition(payment.Status, status))
        {
            throw new InvalidOperationException($"Invalid status transition from {payment.Status} to {status}");
        }

        payment.Status = status;
        payment.UpdatedAt = DateTime.UtcNow;
        
        if (status == PaymentStatus.Completed)
        {
            payment.ProcessedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(transactionId))
        {
            payment.TransactionId = transactionId;
        }

        var updatedPayment = await _paymentRepository.UpdateAsync(payment);
        
        // Clear caches
        await InvalidatePaymentCacheAsync(id);
        await InvalidateUserPaymentsCacheAsync(payment.UserId);
        
        _logger.LogInformation("Payment {PaymentId} status updated to {Status}", id, status);
        
        return _mapper.Map<PaymentResponseDto>(updatedPayment);
    }

    public async Task<RefundResponseDto> CreateRefundAsync(Guid paymentId, string userId, RefundRequestDto request)
    {
        _logger.LogInformation("Creating refund for payment {PaymentId} by user {UserId}", paymentId, userId);

        // Validate request
        var validationResult = await _refundValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Refund validation failed for payment {PaymentId}: {Errors}", paymentId, errors);
            throw new ValidationException(validationResult.Errors);
        }

        var payment = await _paymentRepository.GetByIdAndUserIdAsync(paymentId, userId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment {paymentId} not found for user {userId}");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException("Can only refund completed payments");
        }

        // Check refund amount
        var totalRefunded = await _paymentRepository.GetTotalRefundAmountByPaymentAsync(paymentId);
        var availableAmount = payment.Amount - totalRefunded;
        
        if (request.Amount > availableAmount)
        {
            throw new InvalidOperationException($"Refund amount {request.Amount} exceeds available amount {availableAmount}");
        }

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = RefundStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdRefund = await _paymentRepository.CreateRefundAsync(refund);
        
        // Clear caches
        await InvalidatePaymentCacheAsync(paymentId);
        await InvalidateUserPaymentsCacheAsync(userId);
        
        _logger.LogInformation("Refund {RefundId} created for payment {PaymentId}", createdRefund.Id, paymentId);
        
        return _mapper.Map<RefundResponseDto>(createdRefund);
    }

    public async Task<RefundResponseDto?> GetRefundByIdAsync(Guid id, string userId)
    {
        var refund = await _paymentRepository.GetRefundByIdAndUserIdAsync(id, userId);
        if (refund == null)
        {
            _logger.LogWarning("Refund {RefundId} not found for user {UserId}", id, userId);
            return null;
        }

        return _mapper.Map<RefundResponseDto>(refund);
    }

    public async Task<IEnumerable<RefundResponseDto>> GetPaymentRefundsAsync(Guid paymentId, string userId)
    {
        // Verify user owns the payment
        var payment = await _paymentRepository.GetByIdAndUserIdAsync(paymentId, userId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment {paymentId} not found for user {userId}");
        }

        var refunds = await _paymentRepository.GetRefundsByPaymentIdAsync(paymentId);
        return _mapper.Map<IEnumerable<RefundResponseDto>>(refunds);
    }

    public async Task HandleWebhookAsync(JsonDocument payload)
    {
        try
        {
            var eventType = payload.RootElement.GetProperty("event").GetString();
            _logger.LogInformation("Processing webhook event: {EventType}", eventType);

            switch (eventType)
            {
                case "payment.succeeded":
                    await HandlePaymentSucceededAsync(payload);
                    break;
                case "payment.failed":
                    await HandlePaymentFailedAsync(payload);
                    break;
                case "refund.succeeded":
                    await HandleRefundSucceededAsync(payload);
                    break;
                default:
                    _logger.LogWarning("Unknown webhook event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            throw;
        }
    }

    public async Task<bool> ValidatePaymentOwnershipAsync(Guid paymentId, string userId)
    {
        return await _paymentRepository.IsOwnerAsync(paymentId, userId);
    }

    public async Task<decimal> CalculateRefundAmountAsync(Guid paymentId, decimal requestedAmount)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment {paymentId} not found");
        }

        var totalRefunded = await _paymentRepository.GetTotalRefundAmountByPaymentAsync(paymentId);
        var availableAmount = payment.Amount - totalRefunded;
        
        return Math.Min(requestedAmount, availableAmount);
    }

    private async Task HandlePaymentSucceededAsync(JsonDocument payload)
    {
        var paymentId = payload.RootElement.GetProperty("object").GetProperty("metadata").GetProperty("paymentId").GetGuid();
        var transactionId = payload.RootElement.GetProperty("object").GetProperty("id").GetString();
        
        await UpdatePaymentStatusAsync(paymentId, PaymentStatus.Completed, transactionId);
        
        // Notify OrderService
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment != null)
        {
            await NotifyOrderServiceAsync(payment.OrderId, "payment_completed");
        }
    }

    private async Task HandlePaymentFailedAsync(JsonDocument payload)
    {
        var paymentId = payload.RootElement.GetProperty("object").GetProperty("metadata").GetProperty("paymentId").GetGuid();
        await UpdatePaymentStatusAsync(paymentId, PaymentStatus.Failed);
    }

    private async Task HandleRefundSucceededAsync(JsonDocument payload)
    {
        var refundId = payload.RootElement.GetProperty("object").GetProperty("metadata").GetProperty("refundId").GetGuid();
        var refund = await _paymentRepository.GetRefundByIdAsync(refundId);
        
        if (refund != null)
        {
            refund.Status = RefundStatus.Completed;
            refund.ProcessedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateRefundAsync(refund);
        }
    }

    private async Task NotifyOrderServiceAsync(Guid orderId, string eventType)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrderService");
            var payload = new { orderId, eventType, timestamp = DateTime.UtcNow };
            
            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/payment-events", payload);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Notified OrderService about {EventType} for order {OrderId}", eventType, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify OrderService about {EventType} for order {OrderId}", eventType, orderId);
        }
    }

    private static bool IsValidStatusTransition(PaymentStatus currentStatus, PaymentStatus newStatus)
    {
        var validTransitions = new Dictionary<PaymentStatus, List<PaymentStatus>>
        {
            [PaymentStatus.Pending] = new() { PaymentStatus.Completed, PaymentStatus.Failed, PaymentStatus.Cancelled },
            [PaymentStatus.Completed] = new() { PaymentStatus.Refunded },
            [PaymentStatus.Failed] = new(),
            [PaymentStatus.Cancelled] = new(),
            [PaymentStatus.Refunded] = new()
        };

        return validTransitions.ContainsKey(currentStatus) && 
               validTransitions[currentStatus].Contains(newStatus);
    }

    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            return cached != null ? JsonSerializer.Deserialize<T>(cached) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get from cache: {Key}", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache: {Key}", key);
        }
    }

    private async Task InvalidatePaymentCacheAsync(Guid paymentId)
    {
        await _cache.RemoveAsync($"payment:{paymentId}");
    }

    private async Task InvalidateUserPaymentsCacheAsync(string userId)
    {
        // In a real implementation, you might want to use a more sophisticated cache invalidation strategy
        for (int page = 1; page <= 10; page++)
        {
            await _cache.RemoveAsync($"user_payments:{userId}:page:{page}:size:10");
            await _cache.RemoveAsync($"user_payments:{userId}:page:{page}:size:20");
        }
    }
}
