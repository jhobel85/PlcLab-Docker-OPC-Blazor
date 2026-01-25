# Publishing PlcLab.Web as a Container Image

## Overview

The PlcLab.Web application is automatically built and published as a Docker container image through GitHub Actions. The image is published to the GitHub Container Registry (GHCR).

## Automated Publishing (GitHub Actions)

### When Images Are Published

- **On push to `main` branch**: Image tagged with:
  - `latest` (default branch)
  - Branch name (e.g., `main`)
  - Short commit SHA (e.g., `main-abc1234`)

- **On version tags** (e.g., `v1.0.0`): Image tagged with:
  - Semantic version (e.g., `v1.0.0`)
  - Major.minor version (e.g., `1.0`)
  - Commit SHA

- **On pull requests**: Image is built but not pushed to registry (test build only)

### Publishing Workflow

The GitHub Actions workflow (`.github/workflows/build.yml`) performs the following steps:

1. Checkout code
2. Setup .NET 9 SDK
3. Restore NuGet packages (cached)
4. Build solution (`dotnet build -c Release`)
5. Start OPC UA Reference Server for integration tests
6. Run all tests with code coverage
7. Stop reference server
8. Publish .NET app
9. **Set up Docker Buildx** (multi-platform support)
10. **Login to GitHub Container Registry** (if not a PR)
11. **Build and push Docker image** with proper tagging
12. **Cache Docker layers** for faster rebuilds

### Image Metadata

- **Registry**: `ghcr.io`
- **Repository**: `${{ github.repository }}` (e.g., `jhobel85/plclab-docker-opc-blazor`)
- **Image Name**: `ghcr.io/jhobel85/plclab-docker-opc-blazor`

## Pulling and Running the Image

### Pull the Latest Image

```bash
docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
```

### Run Container Standalone

```bash
docker run -d \
  --name plclab-web \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e OpcUa__Endpoint=opc.tcp://host.docker.internal:4840 \
  ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
```

### Run with Docker Compose

Update your `docker-compose.override.yml` to reference the published image:

```yaml
services:
  web:
    image: ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
    # ... rest of configuration
```

Then start:

```bash
docker compose up -d
```

## Building Images Locally

### Build with Docker CLI

```bash
docker build -t plclab-web:local .
```

### Build with Docker Compose

```bash
docker compose build --no-cache
```

### Build with Buildx (Multi-platform)

```bash
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t ghcr.io/jhobel85/plclab-docker-opc-blazor:local \
  .
```

## Image Configuration

### Environment Variables

When running the container, configure the following environment variables:

| Variable | Default | Purpose |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Execution environment (Development/Staging/Production) |
| `ASPNETCORE_URLS` | `http://+:8080` | HTTP endpoints |
| `OpcUa__Endpoint` | `opc.tcp://opcua-refserver:50000` | OPC UA server endpoint |
| `OpcUa__UseSecurity` | `false` | Enable TLS/security |
| `Seed__Enabled` | `true` | Auto-seed demo data on startup |
| `Seed__SkipExistingData` | `true` | Skip seeding if data exists |
| `Logging__LogLevel__Default` | `Information` | Default log level |
| `MockOpcUa__Enabled` | `false` | Enable in-process mock OPC UA server |
| `MockOpcUa__BaseAddress` | `opc.tcp://localhost:4841` | Mock server endpoint |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | OpenTelemetry collector endpoint |

### Example: Production Configuration

```bash
docker run -d \
  --name plclab-web-prod \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e OpcUa__Endpoint=opc.tcp://plc.example.com:4840 \
  -e Seed__Enabled=false \
  -e Logging__LogLevel__Default=Warning \
  ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
```

## Mounting Certificates and PKI

The container expects OPC UA certificates in the `/app/pki` directory:

```bash
docker run -d \
  --name plclab-web \
  -p 8080:8080 \
  -v /path/to/pki/trusted:/app/pki/trusted \
  -v /path/to/pki/own:/app/pki/own \
  -e OpcUa__UseSecurity=true \
  ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
```

**Directory Structure:**

```
pki/
├── own/              # Application certificate and private key
├── trusted/          # Trusted certificates (OPC UA servers)
├── issuers/          # Trusted issuer certificates
└── rejected/         # Rejected certificates (optional)
```

## Publishing to Other Registries

### Docker Hub

```bash
docker tag ghcr.io/jhobel85/plclab-docker-opc-blazor:latest \
  jhobel85/plclab-web:latest

docker login -u your-username
docker push jhobel85/plclab-web:latest
```

### Azure Container Registry

```bash
az acr login --name myregistry

docker tag ghcr.io/jhobel85/plclab-docker-opc-blazor:latest \
  myregistry.azurecr.io/plclab-web:latest

docker push myregistry.azurecr.io/plclab-web:latest
```

### Private Registry

```bash
docker tag ghcr.io/jhobel85/plclab-docker-opc-blazor:latest \
  private-registry.example.com/plclab-web:latest

docker login private-registry.example.com
docker push private-registry.example.com/plclab-web:latest
```

## Health Checks

The container includes health check support for Kubernetes/orchestration platforms. Add to docker-compose:

```yaml
services:
  web:
    image: ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## Troubleshooting

### Image Pull Issues

**Error**: `image not found` or authentication error

**Solution**:
1. Ensure repository is public or you're logged in with valid credentials:
   ```bash
   echo $CR_PAT | docker login ghcr.io -u your-username --password-stdin
   ```

2. Verify image exists:
   ```bash
   docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
   ```

### Container Won't Start

**Error**: `ASPNETCORE_ENVIRONMENT` not recognized or configuration binding fails

**Solution**: Check environment variables are set correctly:
```bash
docker inspect plclab-web | grep -A 20 "Env"
```

### Cannot Connect to OPC UA Server

**Error**: Connection timeout or refused

**Solution**:
1. Verify OPC UA server is running and accessible from container:
   ```bash
   docker exec plclab-web curl -v opc.tcp://host.docker.internal:4840
   ```

2. If using `host.docker.internal`, ensure Docker Desktop is configured to expose it

3. For Docker on Linux, use container network or expose host network:
   ```bash
   docker run --network host ...
   ```

## Security Considerations

- **Always use specific version tags** in production (`v1.0.0`), not `latest`
- **Enable TLS** by setting `OpcUa__UseSecurity=true` with proper certificates
- **Use secrets management** for sensitive environment variables (Kubernetes Secrets, AWS Secrets Manager, etc.)
- **Scan images for vulnerabilities**:
  ```bash
  trivy image ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
  ```
- **Keep base images updated** (monitor `mcr.microsoft.com/dotnet/aspnet:8.0` for security patches)

## CI/CD Integration

### GitHub Actions (Current Setup)

The workflow automatically publishes on every push to `main`:

```yaml
# Automatic on main branch push
git push origin main  # Triggers publish to ghcr.io

# Tag-based releases
git tag v1.0.0
git push origin v1.0.0  # Publishes v1.0.0, 1.0, latest tags
```

### Manual Publishing

To manually publish from local machine:

```bash
# Build and tag
docker build -t ghcr.io/jhobel85/plclab-docker-opc-blazor:manual-v1 .

# Login
echo $CR_PAT | docker login ghcr.io -u your-username --password-stdin

# Push
docker push ghcr.io/jhobel85/plclab-docker-opc-blazor:manual-v1
```

## Next Steps

- [ ] Configure private registry for your organization
- [ ] Set up image signing and verification
- [ ] Add image scanning to CI/CD pipeline
- [ ] Deploy to Kubernetes with Helm charts
- [ ] Set up artifact retention and cleanup policies
- [ ] Monitor image size and optimize build cache

## References

- [GitHub Container Registry Documentation](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [Docker Build Action](https://github.com/docker/build-push-action)
- [Buildx Multi-platform Builds](https://docs.docker.com/build/building/multi-platform/)
- [OCI Image Spec](https://github.com/opencontainers/image-spec)
