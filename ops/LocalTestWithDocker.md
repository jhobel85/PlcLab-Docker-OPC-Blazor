# Local Test with Docker Compose

## Start Services
```bash
# Start both web app and OPC UA server
docker-compose up --build

# Or start only the OPC UA server on port 5000
docker-compose up opcua-refserver

# Or start only the web service on port 8080
docker-compose up --build web
```

## Services
- **`web`**: PlcLab Blazor web app (built from source), exposed on port 8080.
- **`opcua-refserver`**: Microsoft OPC PLC simulator (image: `mcr.microsoft.com/iotedge/opc-plc:latest`), exposed on port 4840.

## Configuration
- The web app's `OpcUa:Endpoint` is set via environment variable in `docker-compose.yml` to `opc.tcp://opcua-refserver:50000` (internal Docker network).
- For local testing outside Docker, update `appsettings.json` to `opc.tcp://localhost:4840`.

## Access
- Web app: http://localhost:8080
- OPC UA server: opc.tcp://localhost:4840

## Overrides
Create `docker-compose.override.yml` for local customizations if needed.

## Health Check - VeErify app is running (if available)
curl http://localhost:8080/healthz 
