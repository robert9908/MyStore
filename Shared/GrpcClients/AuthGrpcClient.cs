using Grpc.Net.Client;
using MyStore.Shared.Grpc.Auth;
using MyStore.Shared.ServiceDiscovery;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using MyStore.Shared.Tracing;

namespace MyStore.Shared.GrpcClients;

public interface IAuthGrpcClient
{
    Task<ValidateTokenResponse> ValidateTokenAsync(string token);
    Task<GetUserByIdResponse> GetUserByIdAsync(string userId);
    Task<CheckUserRoleResponse> CheckUserRoleAsync(string userId, string requiredRole);
    Task<GetUserPermissionsResponse> GetUserPermissionsAsync(string userId, string resourceType, string resourceId);
}

public class AuthGrpcClient : IAuthGrpcClient, IDisposable
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<AuthGrpcClient> _logger;
    private GrpcChannel? _channel;
    private AuthService.AuthServiceClient? _client;
    private readonly object _lock = new();

    public AuthGrpcClient(IServiceDiscovery serviceDiscovery, ILogger<AuthGrpcClient> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(string token)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthGrpcClient.ValidateToken");
        
        try
        {
            var client = await GetClientAsync();
            var request = new ValidateTokenRequest { Token = token };
            
            return await client.ValidateTokenAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via gRPC");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidateTokenResponse
            {
                IsValid = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<GetUserByIdResponse> GetUserByIdAsync(string userId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthGrpcClient.GetUserById");
        activity?.SetTag("user.id", userId);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetUserByIdRequest { UserId = userId };
            
            return await client.GetUserByIdAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} via gRPC", userId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetUserByIdResponse
            {
                Found = false,
                ErrorMessage = "Service unavailable"
            };
        }
    }

    public async Task<CheckUserRoleResponse> CheckUserRoleAsync(string userId, string requiredRole)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthGrpcClient.CheckUserRole");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("required.role", requiredRole);
        
        try
        {
            var client = await GetClientAsync();
            var request = new CheckUserRoleRequest 
            { 
                UserId = userId, 
                RequiredRole = requiredRole 
            };
            
            return await client.CheckUserRoleAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role for {UserId} via gRPC", userId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CheckUserRoleResponse
            {
                HasRole = false,
                CurrentRole = string.Empty
            };
        }
    }

    public async Task<GetUserPermissionsResponse> GetUserPermissionsAsync(string userId, string resourceType, string resourceId)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthGrpcClient.GetUserPermissions");
        activity?.SetTag("user.id", userId);
        activity?.SetTag("resource.type", resourceType);
        
        try
        {
            var client = await GetClientAsync();
            var request = new GetUserPermissionsRequest 
            { 
                UserId = userId, 
                ResourceType = resourceType,
                ResourceId = resourceId
            };
            
            return await client.GetUserPermissionsAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for {UserId} via gRPC", userId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetUserPermissionsResponse
            {
                CanRead = false,
                CanWrite = false,
                CanDelete = false
            };
        }
    }

    private async Task<AuthService.AuthServiceClient> GetClientAsync()
    {
        if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
            return _client;

        lock (_lock)
        {
            if (_client != null && _channel?.State != Grpc.Core.ConnectivityState.Shutdown)
                return _client;

            _channel?.Dispose();
            
            var endpoint = _serviceDiscovery.GetHealthyServiceAsync("authservice").Result;
            if (endpoint == null)
            {
                throw new InvalidOperationException("No healthy AuthService instances available");
            }

            var address = $"http://{endpoint.Address}:{endpoint.Port}";
            _channel = GrpcChannel.ForAddress(address);
            _client = new AuthService.AuthServiceClient(_channel);
            
            _logger.LogDebug("Created gRPC client for AuthService at {Address}", address);
        }

        return _client;
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
