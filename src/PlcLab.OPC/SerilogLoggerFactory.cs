using Microsoft.Extensions.Logging;
using Opc.Ua;
using Serilog;
using Serilog.Extensions.Logging;

namespace PlcLab.OPC
{
public sealed class SerilogTelemetry : TelemetryContextBase
{
    private SerilogTelemetry(ILoggerFactory loggerFactory) : base(loggerFactory) {}

    public static SerilogTelemetry Create()
    {
        // 1) Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()            // add more sinks as needed
            .CreateLogger();

        // 2) Bridge Serilog -> Microsoft.Extensions.Logging
        var msLoggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: false);
        return new SerilogTelemetry(msLoggerFactory);
    }
}}
