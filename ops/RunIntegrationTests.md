# Run Integration Tests

1. Start the OPC UA reference server (see StartReferenceServer.md).
2. Set `OpcUa:Endpoint` for tests via environment variable.
3. Run tests:
   ```bash
   dotnet test tests/PlcLab.IntegrationTests -c Release
   ```

For CI, add the reference server as a service container in your workflow and run the same command.
