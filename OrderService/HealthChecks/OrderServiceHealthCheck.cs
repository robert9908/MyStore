using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace OrderService.HealthChecks;

public class OrderServiceHealthCheck : IHealthCheck
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderServiceHealthCheck> _logger;

    public OrderServiceHealthCheck(OrderDbContext context, ILogger<OrderServiceHealthCheck> logger)
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
            
            // Check if we can query orders table
            var orderCount = await _context.Orders.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["database_status"] = "connected",
                ["order_count"] = orderCount,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("OrderService is healthy", data);
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

            return HealthCheckResult.Unhealthy("OrderService is unhealthy", ex, data);
        }
    }
}
