using System.Security.Claims;
using PaymentService.Interfaces;

namespace PaymentService.Services;

public class SecurityService : ISecurityService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IPaymentRepository paymentRepository, ILogger<SecurityService> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public string? GetCurrentUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value ??
               user.FindFirst("user_id")?.Value;
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || 
               user.HasClaim(ClaimTypes.Role, "Admin") ||
               user.HasClaim("role", "Admin");
    }

    public bool IsUser(ClaimsPrincipal user)
    {
        return user.IsInRole("User") || 
               user.HasClaim(ClaimTypes.Role, "User") ||
               user.HasClaim("role", "User");
    }

    public async Task<bool> CanAccessPaymentAsync(Guid paymentId, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            _logger.LogDebug("Admin access granted for payment {PaymentId}", paymentId);
            return true;
        }

        var canAccess = await _paymentRepository.IsOwnerAsync(paymentId, userId);
        
        if (!canAccess)
        {
            _logger.LogWarning("Access denied for user {UserId} to payment {PaymentId}", userId, paymentId);
        }
        else
        {
            _logger.LogDebug("Owner access granted for user {UserId} to payment {PaymentId}", userId, paymentId);
        }

        return canAccess;
    }

    public async Task<bool> CanAccessRefundAsync(Guid refundId, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            _logger.LogDebug("Admin access granted for refund {RefundId}", refundId);
            return true;
        }

        var refund = await _paymentRepository.GetRefundByIdAndUserIdAsync(refundId, userId);
        var canAccess = refund != null;

        if (!canAccess)
        {
            _logger.LogWarning("Access denied for user {UserId} to refund {RefundId}", userId, refundId);
        }
        else
        {
            _logger.LogDebug("Owner access granted for user {UserId} to refund {RefundId}", userId, refundId);
        }

        return canAccess;
    }

    public bool HasValidRole(ClaimsPrincipal user)
    {
        return IsAdmin(user) || IsUser(user);
    }

    public string GetUserRole(ClaimsPrincipal user)
    {
        if (IsAdmin(user)) return "Admin";
        if (IsUser(user)) return "User";
        return "Unknown";
    }
}
