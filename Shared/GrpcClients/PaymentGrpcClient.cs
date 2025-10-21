using Grpc.Net.Client;
using MyStore.Shared.Grpc.Payment;
using MyStore.Shared.ServiceDiscovery;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MyStore.Shared.Tracing;

namespace MyStore.Shared.GrpcClients;

public interface IPaymentGrpcClient
{
    Task<CreatePaymentResponse> CreatePaymentAsync(string orderId, string userId, double amount, string currency, string paymentMethod, string description);
    Task<GetPaymentByIdResponse> GetPaymentByIdAsync(string paymentId, string requestingUserId);
    Task<GetPaymentsByOrderIdResponse> GetPaymentsByOrderIdAsync(string orderId, string requestingUserId);
    Task<UpdatePaymentStatusResponse> UpdatePaymentStatusAsync(string paymentId, PaymentStatus status, string gatewayTransactionId, string gatewayResponse);
    Task<CreateRefundResponse> CreateRefundAsync(string paymentId, double amount, string reason, string requestingUserId);
    Task<ValidatePaymentOwnershipResponse> ValidatePaymentOwnershipAsync(string paymentId, string userId);
    Task<GetPaymentStatusResponse> GetPaymentStatusAsync(string orderId);
}

public class PaymentGrpcClient : IPaymentGrpcClient, IDisposable
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<PaymentGrpcClient> _logger;
    private GrpcChannel? _channel;
    private PaymentService.PaymentServiceClient? _client;
    private readonly object _lock = new();

    public PaymentGrpcClient(IServiceDiscovery serviceDiscovery, ILogger<PaymentGrpcClient> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(string orderId, string userId, double amount, string currency, string paymentMethod, string description)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.CreatePayment");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("amount", amount);
        
        try
        {
            var client = await GetClientAsync();
            var request = new CreatePaymentRequest 
            { 
                OrderId = orderId,
                UserId = userId,
                Amount = amount,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Description = description
            };
            
            return await client.CreatePaymentAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for order {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CreatePaymentResponse
            {
                Success = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<GetPaymentByIdResponse> GetPaymentByIdAsync(string paymentId, string requestingUserId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.GetPaymentById");
        activity?.SetTag("payment.id", paymentId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetPaymentByIdRequest 
            { 
                PaymentId = paymentId,
                RequestingUserId = requestingUserId
            };
            
            return await client.GetPaymentByIdAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId} via gRPC", paymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentByIdResponse
            {
                Found = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<GetPaymentsByOrderIdResponse> GetPaymentsByOrderIdAsync(string orderId, string requestingUserId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.GetPaymentsByOrderId");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetPaymentsByOrderIdRequest 
            { 
                OrderId = orderId,
                RequestingUserId = requestingUserId
            };
            
            return await client.GetPaymentsByOrderIdAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for order {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentsByOrderIdResponse
            {
                TotalCount = 0
            };
        }
    }

    public async Task<UpdatePaymentStatusResponse> UpdatePaymentStatusAsync(string paymentId, PaymentStatus status, string gatewayTransactionId, string gatewayResponse)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.UpdatePaymentStatus");
        activity?.SetTag("payment.id", paymentId);
        activity?.SetTag("new.status", status.ToString());
        
        try
        {
            var client = await GetClientAsync();
            var request = new UpdatePaymentStatusRequest 
            { 
                PaymentId = paymentId,
                Status = status,
                GatewayTransactionId = gatewayTransactionId,
                GatewayResponse = gatewayResponse
            };
            
            return await client.UpdatePaymentStatusAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for {PaymentId} via gRPC", paymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new UpdatePaymentStatusResponse
            {
                Success = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<CreateRefundResponse> CreateRefundAsync(string paymentId, double amount, string reason, string requestingUserId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.CreateRefund");
        activity?.SetTag("payment.id", paymentId);
        activity?.SetTag("amount", amount);
        
        try
        {
            var client = await GetClientAsync();
            var request = new CreateRefundRequest 
            { 
                PaymentId = paymentId,
                Amount = amount,
                Reason = reason,
                RequestingUserId = requestingUserId
            };
            
            return await client.CreateRefundAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for payment {PaymentId} via gRPC", paymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CreateRefundResponse
            {
                Success = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<ValidatePaymentOwnershipResponse> ValidatePaymentOwnershipAsync(string paymentId, string userId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.ValidatePaymentOwnership");
        activity?.SetTag("payment.id", paymentId);
        activity?.SetTag("user.id", userId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new ValidatePaymentOwnershipRequest 
            { 
                PaymentId = paymentId,
                UserId = userId
            };
            
            return await client.ValidatePaymentOwnershipAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating payment ownership for {PaymentId} via gRPC", paymentId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidatePaymentOwnershipResponse
            {
                IsOwner = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<GetPaymentStatusResponse> GetPaymentStatusAsync(string orderId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("PaymentGrpcClient.GetPaymentStatus");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetPaymentStatusRequest { OrderId = orderId };
            
            return await client.GetPaymentStatusAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for order {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetPaymentStatusResponse
            {
                Found = false
            };
        }
    }

    private async Task<PaymentService.PaymentServiceClient> GetClientAsync()
    {
        if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
            return _client;

        lock (_lock)
        {
            if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
                return _client;

            _channel?.Dispose();
            
            var endpoint = _serviceDiscovery.GetHealthyServiceAsync("paymentservice").Result;
            if (endpoint == null)
            {
                throw new InvalidOperationException("No healthy PaymentService instances available");
            }

            var address = $"http://{endpoint.Address}:{endpoint.Port}";
            _channel = GrpcChannel.ForAddress(address);
            _client = new PaymentService.PaymentServiceClient(_channel);
            
            _logger.LogDebug("Created gRPC client for PaymentService at {Address}", address);
        }

        return _client;
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
