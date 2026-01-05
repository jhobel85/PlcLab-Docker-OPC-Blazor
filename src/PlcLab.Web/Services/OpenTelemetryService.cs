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
            services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("PlcLab.Web"))
                .WithTracing(tp =>
                {
                    tp
                        .AddSource("PlcLab.Web.IndexViewModel")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://jaeger:4317";
                            o.Endpoint = new Uri(endpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        });
                });

            return services;
        }
    }
}