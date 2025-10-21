using Grpc.Core;
using MyStore.Shared.Grpc.Auth;
using MyStore.AuthService.Interfaces;
using MyStore.AuthService.Services;
using System.Diagnostics;
using MyStore.Shared.Tracing;

namespace MyStore.AuthService.GrpcServices;

public class AuthGrpcService : Auth.AuthBase
{
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(
        IJwtService jwtService,
        IUserService userService,
        ILogger<AuthGrpcService> logger)
    {
        _jwtService = jwtService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task<ValidateTokenResponse> ValidateToken(
        ValidateTokenRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthService.ValidateToken");
        activity?.SetTag("token.length", request.Token?.Length ?? 0);

        try
        {
            _logger.LogDebug("Validating JWT token");

            if (string.IsNullOrEmpty(request.Token))
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "Token is required"
                };
            }

            var principal = _jwtService.ValidateToken(request.Token);
            if (principal == null)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid token"
                };
            }

            var userId = principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst("email")?.Value;
            var role = principal.FindFirst("role")?.Value;
            var expiresAt = _jwtService.GetTokenExpiry(request.Token);

            activity?.SetTag("user.id", userId);
            activity?.SetTag("user.role", role);

            _logger.LogDebug("Token validated successfully for user {UserId}", userId);

            return new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userId ?? string.Empty,
                Email = email ?? string.Empty,
                Role = role ?? string.Empty,
                ExpiresAt = expiresAt?.Ticks ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new ValidateTokenResponse
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }

    public override async Task<GetUserByIdResponse> GetUserById(
        GetUserByIdRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthService.GetUserById");
        activity?.SetTag("user.id", request.UserId);

        try
        {
            _logger.LogDebug("Getting user by ID: {UserId}", request.UserId);

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new GetUserByIdResponse
                {
                    Found = false,
                    ErrorMessage = "User not found"
                };
            }

            return new GetUserByIdResponse
            {
                Found = true,
                User = new User
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    CreatedAt = user.CreatedAt.Ticks,
                    UpdatedAt = user.UpdatedAt.Ticks
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetUserByIdResponse
            {
                Found = false,
                ErrorMessage = "Failed to retrieve user"
            };
        }
    }

    public override async Task<CheckUserRoleResponse> CheckUserRole(
        CheckUserRoleRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthService.CheckUserRole");
        activity?.SetTag("user.id", request.UserId);
        activity?.SetTag("required.role", request.RequiredRole);

        try
        {
            _logger.LogDebug("Checking role for user {UserId}, required role: {RequiredRole}", 
                request.UserId, request.RequiredRole);

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new CheckUserRoleResponse
                {
                    HasRole = false,
                    CurrentRole = string.Empty
                };
            }

            var hasRole = string.Equals(user.Role, request.RequiredRole, StringComparison.OrdinalIgnoreCase) ||
                         (request.RequiredRole == "User" && user.Role == "Admin"); // Admin has User permissions

            return new CheckUserRoleResponse
            {
                HasRole = hasRole,
                CurrentRole = user.Role
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user role for {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new CheckUserRoleResponse
            {
                HasRole = false,
                CurrentRole = string.Empty
            };
        }
    }

    public override async Task<GetUserPermissionsResponse> GetUserPermissions(
        GetUserPermissionsRequest request, 
        ServerCallContext context)
    {
        using var activity = ActivitySourceProvider.ActivitySource.StartActivity("AuthService.GetUserPermissions");
        activity?.SetTag("user.id", request.UserId);
        activity?.SetTag("resource.type", request.ResourceType);

        try
        {
            _logger.LogDebug("Getting permissions for user {UserId}, resource: {ResourceType}/{ResourceId}", 
                request.UserId, request.ResourceType, request.ResourceId);

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new GetUserPermissionsResponse
                {
                    CanRead = false,
                    CanWrite = false,
                    CanDelete = false
                };
            }

            // Simple role-based permissions
            var isAdmin = user.Role == "Admin";
            var isUser = user.Role == "User" || isAdmin;

            // Resource-specific logic
            var canRead = isUser;
            var canWrite = isAdmin || (isUser && IsResourceOwner(request.UserId, request.ResourceType, request.ResourceId));
            var canDelete = isAdmin || (isUser && IsResourceOwner(request.UserId, request.ResourceType, request.ResourceId));

            var permissions = new List<string>();
            if (canRead) permissions.Add("read");
            if (canWrite) permissions.Add("write");
            if (canDelete) permissions.Add("delete");

            return new GetUserPermissionsResponse
            {
                Permissions = { permissions },
                CanRead = canRead,
                CanWrite = canWrite,
                CanDelete = canDelete
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for {UserId}", request.UserId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new GetUserPermissionsResponse
            {
                CanRead = false,
                CanWrite = false,
                CanDelete = false
            };
        }
    }

    private static bool IsResourceOwner(string userId, string resourceType, string resourceId)
    {
        // This is a simplified check - in real implementation, you'd query the resource service
        // For now, assume user owns resources that contain their user ID
        return resourceId.Contains(userId, StringComparison.OrdinalIgnoreCase);
    }
}
