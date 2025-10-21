using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OrderService.Interfaces;

namespace OrderService.Services;

public interface ISecurityService
{
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, string resource, string action);
    Task<bool> CanAccessOrderAsync(ClaimsPrincipal user, Guid orderId);
    Task<bool> CanModifyOrderAsync(ClaimsPrincipal user, Guid orderId);
    string GetCurrentUserId(ClaimsPrincipal user);
    bool IsAdmin(ClaimsPrincipal user);
    bool IsCustomer(ClaimsPrincipal user);
}

public class SecurityService : ISecurityService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IOrderRepository orderRepository, ILogger<SecurityService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, string resource, string action)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Unauthorized access attempt to {Resource} for action {Action}", resource, action);
            return false;
        }

        var userId = GetCurrentUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID not found in claims for resource {Resource} and action {Action}", resource, action);
            return false;
        }

        // Admin can access everything
        if (IsAdmin(user))
        {
            _logger.LogInformation("Admin user {UserId} authorized for {Resource}:{Action}", userId, resource, action);
            return true;
        }

        // Resource-specific authorization
        return resource.ToLower() switch
        {
            "orders" => await AuthorizeOrdersAsync(user, action),
            _ => false
        };
    }

    public async Task<bool> CanAccessOrderAsync(ClaimsPrincipal user, Guid orderId)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
            return false;

        // Admin can access any order
        if (IsAdmin(user))
            return true;

        // Customer can only access their own orders
        var userId = GetCurrentUserId(user);
        if (string.IsNullOrEmpty(userId))
            return false;

        return await _orderRepository.IsOwnerAsync(orderId, userId);
    }

    public async Task<bool> CanModifyOrderAsync(ClaimsPrincipal user, Guid orderId)
    {
        if (!user.Identity?.IsAuthenticated ?? true)
            return false;

        // Admin can modify any order
        if (IsAdmin(user))
            return true;

        // Customer can only cancel their own orders if they're in a cancellable state
        var userId = GetCurrentUserId(user);
        if (string.IsNullOrEmpty(userId))
            return false;

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId)
            return false;

        // Check if order can be cancelled
        return order.CanBeCancelled;
    }

    public string GetCurrentUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               user.FindFirst("sub")?.Value ?? 
               user.FindFirst("userId")?.Value ?? 
               string.Empty;
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || 
               user.HasClaim("role", "Admin") ||
               user.HasClaim(ClaimTypes.Role, "Admin");
    }

    public bool IsCustomer(ClaimsPrincipal user)
    {
        return user.IsInRole("Customer") || 
               user.HasClaim("role", "Customer") ||
               user.HasClaim(ClaimTypes.Role, "Customer");
    }

    private async Task<bool> AuthorizeOrdersAsync(ClaimsPrincipal user, string action)
    {
        return action.ToLower() switch
        {
            "read" => IsCustomer(user) || IsAdmin(user),
            "create" => IsCustomer(user) || IsAdmin(user),
            "update" => IsAdmin(user),
            "delete" => IsAdmin(user),
            "cancel" => IsCustomer(user) || IsAdmin(user),
            _ => false
        };
    }
}
