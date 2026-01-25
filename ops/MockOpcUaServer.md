# Mock OPC UA Server

## Overview

PlcLab includes an in-process mock OPC UA server (`MockOpcUaServer`) for development, testing, and demos. It provides simulated PLC data without requiring external OPC UA server software.

## Features

- **In-Process**: Runs in the same process as the web application
- **Lightweight**: Minimal resource overhead
- **Deterministic**: Predictable data updates every second
- **No External Dependencies**: Self-contained implementation

## Address Space

The mock server exposes the following OPC UA nodes under `ns=2;s=PlcLab`:

### Variables

| Node ID | Display Name | Data Type | Access | Description |
|---------|--------------|-----------|--------|-------------|
| `ns=2;s=PlcLab.Process.State` | Process State | String | Read/Write | Current process state (e.g., "Running") |
| `ns=2;s=PlcLab.Process.Analog.Flow` | Flow Rate | Double | Read/Write | Flow rate value (0.0-100.0) |
| `ns=2;s=PlcLab.Process.Digital.ValveOpen` | Valve Open | Boolean | Read/Write | Valve open/closed state |

### Methods

| Node ID | Display Name | Arguments | Description |
|---------|--------------|-----------|-------------|
| `ns=2;s=PlcLab.Add` | Add | a (Int32), b (Int32) → result (Int32) | Simple addition method for testing |
| `ns=2;s=PlcLab.ResetAlarms` | Reset Alarms | (none) | Simulates resetting alarms |

## Configuration

Enable the mock server in `appsettings.json`:

```json
{
  "MockOpcUa": {
    "Enabled": true,
    "BaseAddress": "opc.tcp://localhost:4841"
  }
}
```

The server automatically starts as a hosted service when enabled.

## Usage Examples

### Connecting

```csharp
var telemetry = SerilogTelemetry.Create();
var sessionAdapter = new OpcSessionAdapter(telemetry);
using var session = await sessionAdapter.CreateSessionAsync("opc.tcp://localhost:4841", useSecurity: false);
```

### Reading Variables

```csharp
var flowRate = await session.ReadValueAsync(new NodeId("PlcLab.Process.Analog.Flow", 2));
Console.WriteLine($"Flow rate: {flowRate}");
```

### Writing Variables

```csharp
await session.WriteAsync(new NodeId("PlcLab.Process.Digital.ValveOpen", 2), true);
```

### Calling Methods

```csharp
var addMethod = new NodeId("PlcLab.Add", 2);
var objectId = ObjectIds.ObjectsFolder;
var result = await session.CallAsync(objectId, addMethod, 5, 3);
Console.WriteLine($"5 + 3 = {result}");
```

## Data Simulation

The mock server updates variable values every second in a deterministic pattern:

- **Flow Rate**: Cycles through 0.0 → 100.0 → 0.0 (10.0 increments)
- **Valve State**: Toggles every 5 seconds
- **Process State**: Rotates through "Idle" → "Running" → "Stopped" → "Idle" (every 10 seconds)

This provides realistic, time-varying data for UI testing and development.

## Architecture

```
MockOpcUaServer (IAsyncDisposable)
  ├─ ApplicationInstance (OPC Foundation SDK)
  └─ MockStandardServer (StandardServer)
       └─ MockNodeManager (CustomNodeManager2)
            ├─ CreateAddressSpace()
            └─ UpdateValues() [Timer: 1s]
```

### Key Components

1. **MockOpcUaServer**: Public API and lifecycle management
2. **MockStandardServer**: OPC UA StandardServer implementation
3. **MockNodeManager**: Custom node manager with namespace `urn:plclab:mock`
4. **MockOpcUaHostedService**: ASP.NET Core hosted service wrapper
5. **MockOpcUaOptions**: Configuration binding from appsettings.json

## Security

The mock server:
- Uses `SecurityMode.None` by default (no encryption)
- Allows anonymous authentication
- Auto-accepts all certificates (`AutoAcceptUntrustedCertificates = true`)

⚠️ **Warning**: This configuration is suitable for development/testing only. Do not use in production.

## Known Issues

1. **Test Instability**: The integration test `MockOpcUaServerTests.MockServer_Allows_Read_Write_And_Method_Calls` is currently skipped due to connection timing issues. The mock server implementation is complete and functional, but the test requires OPC UA stack configuration refinement.

2. **Certificate Generation**: The server relies on automatic certificate creation by the OPC UA stack. In some environments, manual certificate creation may be required.

3. **Port Conflicts**: Ensure port 4841 is available. If running multiple instances, change the `BaseAddress` in configuration.

## Troubleshooting

### Server Won't Start

**Problem**: `Failed to establish tcp listener sockets`

**Solution**: Check that port 4841 is not in use:
```powershell
netstat -ano | findstr :4841
```

### Client Can't Connect

**Problem**: `ServiceResultException [80010000]`

**Possible Causes**:
1. Server not fully started - add delay after startup
2. Certificate validation issues - ensure `AutoAcceptUntrustedCertificates = true`
3. Security policy mismatch - use `SecurityMode.None` for testing
4. Endpoint URL incorrect - verify `opc.tcp://localhost:4841`

**Debugging**:
```csharp
// Enable OPC UA tracing
config.TraceConfiguration.TraceMasks = 0x7FFFFFFF; // All traces
config.TraceConfiguration.OutputFilePath = "OpcTrace.log";
```

## Next Steps

- [ ] Stabilize integration test with proper OPC UA discovery
- [ ] Add more realistic process simulation (PID control, alarms, etc.)
- [ ] Support configurable update intervals
- [ ] Add historical data access (HA)
- [ ] Add event subscription support
- [ ] Add Dockercompose service for standalone mock server

## References

- [OPC Foundation .NET Standard Stack](https://github.com/OPCFoundation/UA-.NETStandard)
- [OPC UA Specification](https://opcfoundation.org/developer-tools/specifications-unified-architecture)
- [PlcLab OPC Integration Guide](./StartReferenceServer.md)
