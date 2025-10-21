using Consul;

namespace MyStore.Shared.ServiceDiscovery;

public interface IServiceDiscovery
{
    Task RegisterServiceAsync(ServiceRegistration registration);
    Task DeregisterServiceAsync(string serviceId);
    Task<List<ServiceEndpoint>> DiscoverServicesAsync(string serviceName);
    Task<ServiceEndpoint?> GetHealthyServiceAsync(string serviceName);
}

public class ServiceRegistration
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public AgentServiceCheck? HealthCheck { get; set; }
}

public class ServiceEndpoint
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IsHealthy { get; set; }
}
