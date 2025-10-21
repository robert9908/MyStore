using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyStore.Shared.ServiceDiscovery;

public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;
    private readonly ConsulOptions _options;

    public ConsulServiceDiscovery(
        IConsulClient consulClient,
        ILogger<ConsulServiceDiscovery> logger,
        IOptions<ConsulOptions> options)
    {
        _consulClient = consulClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RegisterServiceAsync(ServiceRegistration registration)
    {
        var agentServiceRegistration = new AgentServiceRegistration
        {
            ID = registration.ServiceId,
            Name = registration.ServiceName,
            Address = registration.Address,
            Port = registration.Port,
            Tags = registration.Tags,
            Check = registration.HealthCheck
        };

        try
        {
            await _consulClient.Agent.ServiceRegister(agentServiceRegistration);
            _logger.LogInformation("Service {ServiceName} registered with ID {ServiceId} at {Address}:{Port}",
                registration.ServiceName, registration.ServiceId, registration.Address, registration.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName} with ID {ServiceId}",
                registration.ServiceName, registration.ServiceId);
            throw;
        }
    }

    public async Task DeregisterServiceAsync(string serviceId)
    {
        try
        {
            await _consulClient.Agent.ServiceDeregister(serviceId);
            _logger.LogInformation("Service {ServiceId} deregistered successfully", serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service {ServiceId}", serviceId);
            throw;
        }
    }

    public async Task<List<ServiceEndpoint>> DiscoverServicesAsync(string serviceName)
    {
        try
        {
            var queryResult = await _consulClient.Health.Service(serviceName, string.Empty, true);
            
            var services = queryResult.Response.Select(entry => new ServiceEndpoint
            {
                ServiceId = entry.Service.ID,
                ServiceName = entry.Service.Service,
                Address = entry.Service.Address,
                Port = entry.Service.Port,
                Tags = entry.Service.Tags ?? Array.Empty<string>(),
                IsHealthy = entry.Checks.All(check => check.Status == HealthStatus.Passing)
            }).ToList();

            _logger.LogDebug("Discovered {Count} instances of service {ServiceName}", services.Count, serviceName);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover services for {ServiceName}", serviceName);
            return new List<ServiceEndpoint>();
        }
    }

    public async Task<ServiceEndpoint?> GetHealthyServiceAsync(string serviceName)
    {
        var services = await DiscoverServicesAsync(serviceName);
        var healthyServices = services.Where(s => s.IsHealthy).ToList();

        if (!healthyServices.Any())
        {
            _logger.LogWarning("No healthy instances found for service {ServiceName}", serviceName);
            return null;
        }

        // Simple round-robin selection
        var random = new Random();
        var selectedService = healthyServices[random.Next(healthyServices.Count)];
        
        _logger.LogDebug("Selected service instance {ServiceId} for {ServiceName}",
            selectedService.ServiceId, serviceName);
        
        return selectedService;
    }
}

public class ConsulOptions
{
    public string Address { get; set; } = "http://localhost:8500";
    public string DataCenter { get; set; } = "dc1";
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
