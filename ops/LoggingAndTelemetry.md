### Logging & Observability Implementation

- **Serilog** is used for structured logging. It is configured in `Program.cs` (web app) and in `SerilogLoggerFactory.cs` (OPC client utilities). Logs are written to the console by default, and you can add more sinks (e.g., rolling file) by editing the Serilog configuration in either place.
  - To adjust logging, see the `Log.Logger = new LoggerConfiguration()...` section in both files.
  - The web app uses `builder.Host.UseSerilog()` for integration with ASP.NET Core logging.
  - The OPC client utilities use a custom `SerilogTelemetry` class to bridge Serilog with Microsoft.Extensions.Logging.

- **OpenTelemetry** is used for distributed tracing and metrics. It is configured in `Program.cs` of the web app using the OTLP exporter, which is compatible with most observability backends (e.g., Jaeger, Zipkin, Grafana Tempo).
  - Tracing is enabled for ASP.NET Core and outgoing HTTP requests.
  - You can adjust exporters and resource names in the `AddOpenTelemetryTracing` section.

**How to extend:**
- To add file logging, update the `.WriteTo.Console()` line to `.WriteTo.File(...)` in the Serilog configuration.
- To export traces to a specific backend, configure the OTLP exporter endpoint in `appsettings.json` or via environment variables.

**References:**
- [Serilog Documentation](https://serilog.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

## OpenTelemetry Example Usage

### 1. Custom Trace in C#
You can create custom spans in your code to trace specific operations:

```csharp
using OpenTelemetry.Trace;
using System.Diagnostics;

public class MyService
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("PlcLab.Web.MyService");

    public async Task MyTracedOperationAsync()
    {
        using (var activity = ActivitySource.StartActivity("MyTracedOperation"))
        {
            // Add custom attributes to the span
            activity?.SetTag("custom.key", "custom-value");

            // Your business logic here
            await Task.Delay(100); // Simulate work
        }
    }
}
```

### 2. Viewing Traces
- By default, traces are exported via OTLP. You can view them in Jaeger, Zipkin, or Grafana Tempo if you configure the exporter endpoint.
- Example Docker Compose for Jaeger:

```yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686" # Jaeger UI
      - "4317:4317"   # OTLP gRPC
```

Set the OTLP exporter endpoint in your environment or `appsettings.json`:

```json
"OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
```

### 3. More Info
- [OpenTelemetry .NET Manual Instrumentation](https://opentelemetry.io/docs/instrumentation/net/manual/)
- [Jaeger UI](http://localhost:16686)

---

## How to View OpenTelemetry Traces Locally

1. **Start Jaeger (all-in-one) with Docker:**

   ```sh
   docker run -d --name jaeger \
     -e COLLECTOR_OTLP_ENABLED=true \
     -p 16686:16686 -p 4317:4317 \
     jaegertracing/all-in-one:latest
   ```

2. **Configure your app to export traces to Jaeger:**
   - Set the environment variable in your shell or Docker Compose:
     - `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`
   - Or add to `appsettings.json`:
     ```json
     "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317"
     ```

3. **Open the Jaeger UI:**
   - Go to [http://localhost:16686](http://localhost:16686) in your browser.
   - Search for your service name (e.g., `PlcLab.Web`) to view traces.

4. **References:**
   - [Jaeger Documentation](https://www.jaegertracing.io/docs/)
   - [OpenTelemetry .NET Exporters](https://opentelemetry.io/docs/instrumentation/net/exporters/)