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
            // Make OTLP optional: only wire exporter when endpoint is provided.
            var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

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
                        .AddHttpClientInstrumentation();

                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        tp.AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(endpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }
                });

            Console.WriteLine(string.IsNullOrWhiteSpace(endpoint)
                ? "OpenTelemetry configured without OTLP exporter (set OTEL_EXPORTER_OTLP_ENDPOINT to enable)."
                : $"OpenTelemetry configured with OTLP endpoint: {endpoint}");
            return services;
        }
    }
}