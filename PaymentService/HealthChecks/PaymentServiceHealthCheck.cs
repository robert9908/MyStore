using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentService.Data;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.HealthChecks;

public class PaymentServiceHealthCheck : IHealthCheck
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentServiceHealthCheck> _logger;

    public PaymentServiceHealthCheck(PaymentDbContext context, ILogger<PaymentServiceHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Check if we can query the database
            var paymentCount = await _context.Payments.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["database_status"] = "connected",
                ["total_payments"] = paymentCount,
                ["timestamp"] = DateTime.UtcNow
            };

            _logger.LogInformation("Health check passed. Database connected with {PaymentCount} payments", paymentCount);
            
            return HealthCheckResult.Healthy("Payment service is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Payment service health check failed", ex);
        }
    }
}
