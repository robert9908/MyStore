using System.Security.Claims;

namespace PaymentService.Interfaces;

public interface ISecurityService
{
    string? GetCurrentUserId(ClaimsPrincipal user);
    bool IsAdmin(ClaimsPrincipal user);
    bool IsUser(ClaimsPrincipal user);
    Task<bool> CanAccessPaymentAsync(Guid paymentId, string userId, bool isAdmin);
    Task<bool> CanAccessRefundAsync(Guid refundId, string userId, bool isAdmin);
    bool HasValidRole(ClaimsPrincipal user);
    string GetUserRole(ClaimsPrincipal user);
}
