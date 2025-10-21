using Microsoft.Extensions.Diagnostics.HealthChecks;
using AuthService.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.HealthChecks;

public class AuthServiceHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthServiceHealthCheck> _logger;

    public AuthServiceHealthCheck(AppDbContext context, ILogger<AuthServiceHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Check if we can query users table
            var userCount = await _context.Users.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["database_status"] = "connected",
                ["user_count"] = userCount,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("AuthService is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var data = new Dictionary<string, object>
            {
                ["database_status"] = "failed",
                ["error"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Unhealthy("AuthService is unhealthy", ex, data);
        }
    }
}
