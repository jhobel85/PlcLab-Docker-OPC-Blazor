## How DI resolves it:

When Blazor creates Index.razor, it sees @inject IOpcUaClientFactory
DI looks up IOpcUaClientFactory → finds OpcUaClientFactory
DI sees OpcUaClientFactory constructor needs ITelemetryContext
DI provides the registered SerilogTelemetry instance
DI creates OpcUaClientFactory with the telemetry instance and injects it into the page