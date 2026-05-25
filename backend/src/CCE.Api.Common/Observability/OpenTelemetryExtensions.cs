using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Registers OpenTelemetry tracing for ASP.NET Core and HttpClient,
/// exporting spans to Seq via OTLP. Disabled when Seq:EnableTracing is false
/// or Seq:OtlpEndpoint is missing.
/// </summary>
public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddCceOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = configuration["Seq:OtlpEndpoint"] ?? "http://localhost:5341/ingest/otlp";
        var enableTracing = configuration.GetValue<bool?>("Seq:EnableTracing") ?? true;

        if (!enableTracing || string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("CCE")
                    .AddOtlpExporter(opts =>
                    {
                        opts.Endpoint = new Uri(otlpEndpoint);
                    });
            });

        return services;
    }
}
