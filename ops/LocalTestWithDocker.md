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

The logic automatically detects the environment:
If discovery URL contains `localhost` → uses localhost hostname
If discovery URL contains `opcua-refserver` → uses opcua-refserver hostname
This is the correct approach and cannot be simplified further because of how Docker networking works. The hostname() resolution is fundamentally different between the two environments.

## Access
- Web app: http://localhost:8080
- OPC UA server: opc.tcp://localhost:4840

## Overrides
Create `docker-compose.override.yml` for local customizations if needed.

## Health Check - VeErify app is running (if available)
curl http://localhost:8080/healthz 

## Disable Demo Data Seeding
By default the repository `appsettings.json` enables demo seeding (`Seed:Enabled = true`). To disable seeding when running the Docker web service, add an environment override in `docker-compose.yml` or create an override file. Example (already set in the provided compose):

```yaml
services:
	web:
		environment:
			- Seed__Enabled=false
```

You can also pass `--Seed:Enabled=false` as a command argument or mount a custom `appsettings.json` into the container if you need more complex overrides.
