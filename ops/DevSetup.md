# Developer Setup

1. Install **.NET 9 SDK**.
2. Install **Docker Desktop** or compatible engine.
3. Clone the repo and restore:
   ```bash
   dotnet restore
   ```
4. PlcLab.OPC - Install required package 
dotnet add package Opc.Ua.Core
dotnet add package Opc.Ua.Client
dotnet add package Opc.Ua.Configuration 
dotnet add package Opc.Ua.Security.Certificates 

dotnet clean
dotnet nuget locals all --clear
dotnet restore

5. Run the app:
   ```bash
   dotnet run --project src/PlcLab.Web
   ```

## Swagger UI

In development, the app exposes interactive API documentation at:

- `GET /swagger`
- `GET /swagger/index.html`
- OpenAPI JSON: `GET /swagger/v1/swagger.json`

Typical local URL:

```text
http://localhost:8080/swagger
```

Swagger includes all `/api/*` endpoints plus `/health` and `/healthz`.
The `SeedInfo` endpoint is marked with JWT bearer security. Use the **Authorize**
button in Swagger UI and provide a token as:

```text
Bearer <your-jwt-token>
```

## Health Check

The app exposes two health endpoints:

| URL | Description |
|---|---|
| `GET /health` | Lightweight JSON ping (`{"status":"ok"}`) |
| `GET /healthz` | ASP.NET Core Health Checks endpoint (returns `Healthy` / `Degraded` / `Unhealthy`) |

Use `/healthz` for Kubernetes readiness/liveness probes and Docker `HEALTHCHECK`.
The `Dockerfile` already contains:

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/healthz || exit 1
```

