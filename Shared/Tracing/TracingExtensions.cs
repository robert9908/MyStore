using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace MyStore.Shared.Tracing;

public static class TracingExtensions
{
    public static IServiceCollection AddDistributedTracing(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion = "1.0.0")
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName, serviceVersion)
                        .AddTelemetrySdk())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST") ?? "localhost";
                        options.AgentPort = int.Parse(Environment.GetEnvironmentVariable("JAEGER_AGENT_PORT") ?? "6831");
                    });
            });

        return services;
    }
}

public static class ActivitySourceProvider
{
    public static readonly ActivitySource ActivitySource = new("MyStore");
}
