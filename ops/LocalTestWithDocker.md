# Local Test with Docker Compose

Create `docker-compose.override.yml` for local overrides if needed, then:

```bash
docker compose up --build
```

Services to consider:
- `web` — the Blazor app
- `opcua-refserver` — OPC UA reference server (expose 4840)

Configure `OpcUa:Endpoint` to point to the reference server.
