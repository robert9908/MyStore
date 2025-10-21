using Grpc.Core;
using MyStore.Shared.Grpc.Payment;
using MyStore.PaymentService.Interfaces;
using MyStore.PaymentService.DTOs;
using System.Diagnostics;
using MyStore.Shared.Tracing;
using AutoMapper;

namespace MyStore.PaymentService.GrpcServices;

public class PaymentGrpcService : Payment.PaymentBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ISecurityService _securityService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentGrpcService> _logger;

    public PaymentGrpcService(
        IPaymentService paymentService,
        IPaymentRepository paymentRepository,
        ISecurityService securityService,
        IMapper mapper,
        ILogger<PaymentGrpcService> logger)
    {
        _paymentService = paymentService;
        _paymentRepository = paymentRepository;
        _securityService = securityService;
        _mapper = mapper;
        _logger = logger;
    }

    public override async Task<CreatePaymentResponse> CreatePayment(
        CreatePaymentRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.CreatePayment");
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("user.id", request.UserId);
        activity?.SetTag("amount", request.Amount);

        try
        {
            _logger.LogInformation("Creating payment for order {OrderId}, user {UserId}, amount {Amount}",
                request.OrderId, request.UserId, request.Amount);

            var paymentRequest = new PaymentRequestDto
            {
                OrderId = Guid.Parse(request.OrderId),
                Amount = (decimal)request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Description = request.Description
            };

            var paymentResponse = await _paymentService.CreatePaymentAsync(request.UserId, paymentRequest);
            var grpcPayment = MapToGrpcPayment(paymentResponse);

            return new CreatePaymentResponse
            {
                Success = true,
                Payment = grpcPayment,
                PaymentUrl = paymentResponse.PaymentUrl ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for order {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CreatePaymentResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<GetPaymentByIdResponse> GetPaymentById(
        GetPaymentByIdRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.GetPaymentById");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("requesting.user.id", request.RequestingUserId);

        try
        {
            if (!Guid.TryParse(request.PaymentId, out var paymentId))
            {
                return new GetPaymentByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Invalid payment ID format"
                };
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return new GetPaymentByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Payment not found"
                };
            }

            // Check ownership
            if (!await _securityService.CanAccessPaymentAsync(request.RequestingUserId, paymentId))
            {
                return new GetPaymentByIdResponse
                {
                    Found = false,
                    ErrorMessage = "Access denied"
                };
            }

            var paymentResponse = _mapper.Map<PaymentResponseDto>(payment);
            var grpcPayment = MapToGrpcPayment(paymentResponse);

            return new GetPaymentByIdResponse
            {
                Found = true,
                Payment = grpcPayment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentByIdResponse
            {
                Found = false,
                ErrorMessage = "Failed to retrieve payment"
            };
        }
    }

    public override async Task<GetPaymentsByOrderIdResponse> GetPaymentsByOrderId(
        GetPaymentsByOrderIdRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.GetPaymentsByOrderId");
        activity?.SetTag("order.id", request.OrderId);

        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new GetPaymentsByOrderIdResponse
                {
                    TotalCount = 0
                };
            }

            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            var grpcPayments = payments.Select(p => MapToGrpcPayment(_mapper.Map<PaymentResponseDto>(p)));

            return new GetPaymentsByOrderIdResponse
            {
                Payments = { grpcPayments },
                TotalCount = payments.Count()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for order {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentsByOrderIdResponse
            {
                TotalCount = 0
            };
        }
    }

    public override async Task<UpdatePaymentStatusResponse> UpdatePaymentStatus(
        UpdatePaymentStatusRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.UpdatePaymentStatus");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("new.status", request.Status.ToString());

        try
        {
            if (!Guid.TryParse(request.PaymentId, out var paymentId))
            {
                return new UpdatePaymentStatusResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid payment ID format"
                };
            }

            var paymentStatus = MapFromGrpcPaymentStatus(request.Status);
            await _paymentService.UpdatePaymentStatusAsync(paymentId, paymentStatus, 
                request.GatewayTransactionId, request.GatewayResponse);

            return new UpdatePaymentStatusResponse
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new UpdatePaymentStatusResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<CreateRefundResponse> CreateRefund(
        CreateRefundRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.CreateRefund");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("amount", request.Amount);

        try
        {
            if (!Guid.TryParse(request.PaymentId, out var paymentId))
            {
                return new CreateRefundResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid payment ID format"
                };
            }

            var refundRequest = new RefundRequestDto
            {
                Amount = (decimal)request.Amount,
                Reason = request.Reason
            };

            var refundResponse = await _paymentService.CreateRefundAsync(request.RequestingUserId, paymentId, refundRequest);
            var grpcRefund = MapToGrpcRefund(refundResponse);

            return new CreateRefundResponse
            {
                Success = true,
                Refund = grpcRefund
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for payment {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CreateRefundResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<ValidatePaymentOwnershipResponse> ValidatePaymentOwnership(
        ValidatePaymentOwnershipRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.ValidatePaymentOwnership");
        activity?.SetTag("payment.id", request.PaymentId);
        activity?.SetTag("user.id", request.UserId);

        try
        {
            if (!Guid.TryParse(request.PaymentId, out var paymentId))
            {
                return new ValidatePaymentOwnershipResponse
                {
                    IsOwner = false,
                    ErrorMessage = "Invalid payment ID format"
                };
            }

            var isOwner = await _securityService.CanAccessPaymentAsync(request.UserId, paymentId);
            
            return new ValidatePaymentOwnershipResponse
            {
                IsOwner = isOwner
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment ownership for {PaymentId}", request.PaymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidatePaymentOwnershipResponse
            {
                IsOwner = false,
                ErrorMessage = "Validation failed"
            };
        }
    }

    public override async Task<GetPaymentStatusResponse> GetPaymentStatus(
        GetPaymentStatusRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentService.GetPaymentStatus");
        activity?.SetTag("order.id", request.OrderId);

        try
        {
            if (!Guid.TryParse(request.OrderId, out var orderId))
            {
                return new GetPaymentStatusResponse
                {
                    Found = false
                };
            }

            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            var latestPayment = payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();

            if (latestPayment == null)
            {
                return new GetPaymentStatusResponse
                {
                    Found = false
                };
            }

            return new GetPaymentStatusResponse
            {
                Found = true,
                Status = MapToGrpcPaymentStatus(latestPayment.Status),
                Amount = (double)latestPayment.Amount,
                Currency = latestPayment.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for order {OrderId}", request.OrderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentStatusResponse
            {
                Found = false
            };
        }
    }

    private static Shared.Grpc.Payment.Payment MapToGrpcPayment(PaymentResponseDto payment)
    {
        return new Shared.Grpc.Payment.Payment
        {
            Id = payment.Id.ToString(),
            OrderId = payment.OrderId.ToString(),
            UserId = payment.UserId,
            Amount = (double)payment.Amount,
            Currency = payment.Currency,
            Status = MapToGrpcPaymentStatus(payment.Status),
            PaymentMethod = payment.PaymentMethod ?? string.Empty,
            GatewayTransactionId = payment.GatewayTransactionId ?? string.Empty,
            GatewayResponse = payment.GatewayResponse ?? string.Empty,
            Description = payment.Description ?? string.Empty,
            Refunds = { payment.Refunds?.Select(MapToGrpcRefund) ?? Enumerable.Empty<Refund>() },
            CreatedAt = payment.CreatedAt.Ticks,
            UpdatedAt = payment.UpdatedAt.Ticks
        };
    }

    private static Refund MapToGrpcRefund(RefundResponseDto refund)
    {
        return new Refund
        {
            Id = refund.Id.ToString(),
            PaymentId = refund.PaymentId.ToString(),
            Amount = (double)refund.Amount,
            Status = MapToGrpcRefundStatus(refund.Status),
            Reason = refund.Reason ?? string.Empty,
            GatewayRefundId = refund.GatewayRefundId ?? string.Empty,
            CreatedAt = refund.CreatedAt.Ticks,
            UpdatedAt = refund.UpdatedAt.Ticks
        };
    }

    private static PaymentStatus MapToGrpcPaymentStatus(Entities.PaymentStatus status)
    {
        return status switch
        {
            Entities.PaymentStatus.Pending => PaymentStatus.PaymentStatusPending,
            Entities.PaymentStatus.Processing => PaymentStatus.PaymentStatusProcessing,
            Entities.PaymentStatus.Succeeded => PaymentStatus.PaymentStatusSucceeded,
            Entities.PaymentStatus.Failed => PaymentStatus.PaymentStatusFailed,
            Entities.PaymentStatus.Cancelled => PaymentStatus.PaymentStatusCancelled,
            Entities.PaymentStatus.Refunded => PaymentStatus.PaymentStatusRefunded,
            Entities.PaymentStatus.PartiallyRefunded => PaymentStatus.PaymentStatusPartiallyRefunded,
            _ => PaymentStatus.PaymentStatusUnspecified
        };
    }

    private static Entities.PaymentStatus MapFromGrpcPaymentStatus(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.PaymentStatusPending => Entities.PaymentStatus.Pending,
            PaymentStatus.PaymentStatusProcessing => Entities.PaymentStatus.Processing,
            PaymentStatus.PaymentStatusSucceeded => Entities.PaymentStatus.Succeeded,
            PaymentStatus.PaymentStatusFailed => Entities.PaymentStatus.Failed,
            PaymentStatus.PaymentStatusCancelled => Entities.PaymentStatus.Cancelled,
            PaymentStatus.PaymentStatusRefunded => Entities.PaymentStatus.Refunded,
            PaymentStatus.PaymentStatusPartiallyRefunded => Entities.PaymentStatus.PartiallyRefunded,
            _ => Entities.PaymentStatus.Pending
        };
    }

    private static RefundStatus MapToGrpcRefundStatus(Entities.RefundStatus status)
    {
        return status switch
        {
            Entities.RefundStatus.Pending => RefundStatus.RefundStatusPending,
            Entities.RefundStatus.Processing => RefundStatus.RefundStatusProcessing,
            Entities.RefundStatus.Succeeded => RefundStatus.RefundStatusSucceeded,
            Entities.RefundStatus.Failed => RefundStatus.RefundStatusFailed,
            Entities.RefundStatus.Cancelled => RefundStatus.RefundStatusCancelled,
            _ => RefundStatus.RefundStatusUnspecified
        };
    }
}
