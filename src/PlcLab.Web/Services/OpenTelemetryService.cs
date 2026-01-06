using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace PlcLab.Web.Services
{
    public static class OpenTelemetryService
    {
        public static IServiceCollection AddPlcLabOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://jaeger:4317";
            
            services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: "PlcLab.Web", serviceVersion: "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        { "environment", "development" }
                    }))
                .WithTracing(tp =>
                {
                    tp
                        // Sample 100% of traces for debugging/development
                        .SetSampler(new ParentBasedSampler(new AlwaysOnSampler()))
                        .AddSource("PlcLab.Web.IndexViewModel")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(endpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        });
                });

            Console.WriteLine($"OpenTelemetry configured with Jaeger endpoint: {endpoint}");
            return services;
        }
    }
}