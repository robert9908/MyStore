using Grpc.Net.Client;
using MyStore.Shared.Grpc.Order;
using MyStore.Shared.ServiceDiscovery;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MyStore.Shared.Tracing;

namespace MyStore.Shared.GrpcClients;

public interface IOrderGrpcClient
{
    Task<GetOrderByIdResponse> GetOrderByIdAsync(string orderId, string requestingUserId);
    Task<UpdateOrderStatusResponse> UpdateOrderStatusAsync(string orderId, OrderStatus status, string? paymentId = null, string? notes = null);
    Task<ValidateOrderOwnershipResponse> ValidateOrderOwnershipAsync(string orderId, string userId);
    Task<GetOrderTotalResponse> GetOrderTotalAsync(string orderId);
    Task<CancelOrderResponse> CancelOrderAsync(string orderId, string reason, string requestingUserId);
}

public class OrderGrpcClient : IOrderGrpcClient, IDisposable
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<OrderGrpcClient> _logger;
    private GrpcChannel? _channel;
    private OrderService.OrderServiceClient? _client;
    private readonly object _lock = new();

    public OrderGrpcClient(IServiceDiscovery serviceDiscovery, ILogger<OrderGrpcClient> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    public async Task<GetOrderByIdResponse> GetOrderByIdAsync(string orderId, string requestingUserId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderGrpcClient.GetOrderById");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetOrderByIdRequest 
            { 
                OrderId = orderId,
                RequestingUserId = requestingUserId
            };
            
            return await client.GetOrderByIdAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetOrderByIdResponse
            {
                Found = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<UpdateOrderStatusResponse> UpdateOrderStatusAsync(string orderId, OrderStatus status, string? paymentId = null, string? notes = null)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderGrpcClient.UpdateOrderStatus");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("new.status", status.ToString());
        
        try
        {
            var client = await GetClientAsync();
            var request = new UpdateOrderStatusRequest 
            { 
                OrderId = orderId,
                Status = status,
                PaymentId = paymentId ?? string.Empty,
                Notes = notes ?? string.Empty
            };
            
            return await client.UpdateOrderStatusAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new UpdateOrderStatusResponse
            {
                Success = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<ValidateOrderOwnershipResponse> ValidateOrderOwnershipAsync(string orderId, string userId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderGrpcClient.ValidateOrderOwnership");
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("user.id", userId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new ValidateOrderOwnershipRequest 
            { 
                OrderId = orderId,
                UserId = userId
            };
            
            return await client.ValidateOrderOwnershipAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order ownership for {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidateOrderOwnershipResponse
            {
                IsOwner = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<GetOrderTotalResponse> GetOrderTotalAsync(string orderId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderGrpcClient.GetOrderTotal");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetOrderTotalRequest { OrderId = orderId };
            
            return await client.GetOrderTotalAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order total for {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetOrderTotalResponse
            {
                Found = false
            };
        }
    }

    public async Task<CancelOrderResponse> CancelOrderAsync(string orderId, string reason, string requestingUserId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("OrderGrpcClient.CancelOrder");
        activity?.SetTag("order.id", orderId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new CancelOrderRequest 
            { 
                OrderId = orderId,
                Reason = reason,
                RequestingUserId = requestingUserId
            };
            
            return await client.CancelOrderAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId} via gRPC", orderId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CancelOrderResponse
            {
                Success = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    private async Task<OrderService.OrderServiceClient> GetClientAsync()
    {
        if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
            return _client;

        lock (_lock)
        {
            if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
                return _client;

            _channel?.Dispose();
            
            var endpoint = _serviceDiscovery.GetHealthyServiceAsync("orderservice").Result;
            if (endpoint == null)
            {
                throw new InvalidOperationException("No healthy OrderService instances available");
            }

            var address = $"http://{endpoint.Address}:{endpoint.Port}";
            _channel = GrpcChannel.ForAddress(address);
            _client = new OrderService.OrderServiceClient(_channel);
            
            _logger.LogDebug("Created gRPC client for OrderService at {Address}", address);
        }

        return _client;
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
