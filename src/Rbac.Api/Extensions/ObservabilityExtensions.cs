using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Rbac.Api.Middleware;

namespace Rbac.Api.Extensions;

public static class ObservabilityExtensions
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("Rbac.Api");

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration.GetValue<string>("Service:Name") ?? "rbac-api";
        var serviceVersion = configuration.GetValue<string>("Service:Version") ?? "1.0.0";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk();

        services.AddSingleton(ActivitySource);

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource("Rbac.Api")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("rbac.security.metrics")
                    .AddMeter("rbac.api.metrics")
                    .AddPrometheusExporter();
            });

        // Logging integration
        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
