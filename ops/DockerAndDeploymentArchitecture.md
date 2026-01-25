# Docker and Deployment Architecture

## Overview

PlcLab uses Docker and GitHub Container Registry to provide consistent, repeatable deployments across all environments. This document describes the complete deployment architecture.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ GitHub Repository (jhobel85/PlcLab-Docker-OPC-Blazor)       │
└─────────────────────────────────────────────────────────────┘
         ↓ (push to main / tag push)
┌─────────────────────────────────────────────────────────────┐
│ GitHub Actions Workflow (.github/workflows/build.yml)       │
│  ├─ Restore NuGet packages (cached)                         │
│  ├─ Build solution (Release)                                │
│  ├─ Run tests (with Docker Reference Server)                │
│  ├─ Collect code coverage                                   │
│  ├─ Set up Docker Buildx (multi-platform)                   │
│  ├─ Login to GHCR                                           │
│  └─ Build & Push Docker Image                               │
└─────────────────────────────────────────────────────────────┘
         ↓ (main branch + tag: v1.0.0)
┌─────────────────────────────────────────────────────────────┐
│ GitHub Container Registry (ghcr.io)                         │
│  ├─ ghcr.io/.../plclab-docker-opc-blazor:latest            │
│  ├─ ghcr.io/.../plclab-docker-opc-blazor:main              │
│  ├─ ghcr.io/.../plclab-docker-opc-blazor:v1.0.0            │
│  ├─ ghcr.io/.../plclab-docker-opc-blazor:1.0               │
│  └─ ghcr.io/.../plclab-docker-opc-blazor:main-abc1234      │
└─────────────────────────────────────────────────────────────┘
         ↓ (docker pull / docker compose up)
┌─────────────────────────────────────────────────────────────┐
│ Local Development / Production Environment                  │
│  ├─ Docker Desktop (local)                                  │
│  ├─ Docker Compose (local + prod)                           │
│  ├─ Kubernetes (enterprise)                                 │
│  └─ Cloud providers (Azure, AWS, GCP)                       │
└─────────────────────────────────────────────────────────────┘
```

## Docker Image Structure

### Build Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln Directory.Packages.props ./
COPY src/ ./

# Build PlcLab.Web in Release mode
RUN dotnet build -c Release
RUN dotnet publish -c Release -o /app/publish
```

### Runtime Stage

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create PKI directories for OPC UA certificates
RUN mkdir -p /app/pki/trusted /app/pki/rejected

# Copy published application
COPY --from=build /app/publish .

# Expose web server port
EXPOSE 8080

# Run application
ENTRYPOINT ["dotnet", "PlcLab.Web.dll"]
```

**Result**: Single-layer runtime image (~368 MB)

## Deployment Models

### Model 1: Local Development

**Environment**: Windows/macOS/Linux with Docker Desktop

**Usage**:
```bash
git clone https://github.com/jhobel85/PlcLab-Docker-OPC-Blazor
cd PlcLab-Docker-OPC-Blazor
docker compose up -d
```

**Components**:
- PlcLab.Web (port 8080)
- PostgreSQL (port 5432)
- OPC UA Reference Server (port 4840)
- Jaeger Telemetry (port 16686)

**Startup**: `docker-compose.yml` + `docker-compose.override.yml`

### Model 2: Docker Compose (Staging)

**Environment**: Linux server with Docker Engine

**Usage**:
```bash
docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
docker compose -f docker-compose.prod.yml up -d
```

**Example docker-compose.prod.yml**:
```yaml
version: '3.8'
services:
  web:
    image: ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
    restart: always
    ports:
      - "80:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      OpcUa__Endpoint: opc.tcp://plc.example.com:4840
      Seed__Enabled: false
    volumes:
      - ./pki:/app/pki
      - ./logs:/app/logs
    depends_on:
      - postgres

  postgres:
    image: postgres:16
    restart: always
    environment:
      POSTGRES_DB: PlcLab
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

### Model 3: Kubernetes (Enterprise)

**Environment**: Kubernetes cluster (EKS, AKS, GKE, on-prem)

**Deployment**:
```bash
kubectl create namespace plclab
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=<username> \
  --docker-password=<pat>
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

**Example k8s/deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: plclab-web
  namespace: plclab
spec:
  replicas: 2
  selector:
    matchLabels:
      app: plclab-web
  template:
    metadata:
      labels:
        app: plclab-web
    spec:
      imagePullSecrets:
        - name: ghcr-secret
      containers:
        - name: web
          image: ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: 8080
              protocol: TCP
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: OpcUa__Endpoint
              value: opc.tcp://plc.example.com:4840
          resources:
            requests:
              memory: "256Mi"
              cpu: "250m"
            limits:
              memory: "512Mi"
              cpu: "500m"
          livenessProbe:
            httpGet:
              path: /health
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 5
            periodSeconds: 5
          volumeMounts:
            - name: pki
              mountPath: /app/pki
      volumes:
        - name: pki
          secret:
            secretName: plclab-pki
---
apiVersion: v1
kind: Service
metadata:
  name: plclab-web
  namespace: plclab
spec:
  type: LoadBalancer
  selector:
    app: plclab-web
  ports:
    - protocol: TCP
      port: 80
      targetPort: 8080
```

## Environment Configuration

### Configuration Priority

1. **Command line arguments** (highest)
2. **Environment variables**
3. **appsettings.{Environment}.json**
4. **appsettings.json** (lowest)

### Essential Environment Variables

| Variable | Values | Purpose |
|----------|--------|---------|
| `ASPNETCORE_ENVIRONMENT` | Development, Staging, Production | Runtime environment |
| `ASPNETCORE_URLS` | `http://+:8080` | HTTP binding |
| `OpcUa__Endpoint` | `opc.tcp://...` | OPC UA server endpoint |
| `OpcUa__UseSecurity` | true/false | TLS encryption |
| `Seed__Enabled` | true/false | Auto-seed demo data |
| `Logging__LogLevel__Default` | Debug, Information, Warning, Error | Log level |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | Telemetry collector |

## Security Considerations

### Image Security

1. **Use specific version tags** in production:
   ```bash
   # ✓ Good
   docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
   
   # ✗ Bad
   docker pull ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
   ```

2. **Sign images** (optional, recommended):
   ```bash
   docker trust signer add --key ~/.docker/notary/delegation_keys/root_keys/<key-id>.key \
     my-signer ghcr.io/jhobel85/plclab-docker-opc-blazor
   ```

3. **Scan for vulnerabilities**:
   ```bash
   trivy image ghcr.io/jhobel85/plclab-docker-opc-blazor:v1.0.0
   ```

### Runtime Security

1. **Run as non-root**:
   ```dockerfile
   RUN useradd -m plclab
   USER plclab
   ```

2. **Use read-only filesystems**:
   ```bash
   docker run --read-only --tmpfs /tmp <image>
   ```

3. **Enable AppArmor/SELinux**:
   ```bash
   docker run --security-opt apparmor=docker-default <image>
   ```

4. **Resource limits**:
   ```yaml
   docker run -m 512m --cpus 1 <image>
   ```

### Network Security

1. **Use TLS for OPC UA**:
   ```bash
   -e OpcUa__UseSecurity=true
   ```

2. **Mount certificates**:
   ```bash
   -v /secure/pki:/app/pki:ro
   ```

3. **Network policies**:
   ```yaml
   apiVersion: networking.k8s.io/v1
   kind: NetworkPolicy
   metadata:
     name: plclab-network-policy
   spec:
     podSelector:
       matchLabels:
         app: plclab-web
     policyTypes:
       - Ingress
       - Egress
   ```

## Scaling and High Availability

### Horizontal Scaling

**Docker Compose**:
```bash
docker compose up --scale web=3
```

**Kubernetes**:
```bash
kubectl scale deployment plclab-web --replicas=5
```

### Load Balancing

**Docker Compose**:
```yaml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - web

  web:
    deploy:
      replicas: 3
```

**Kubernetes**: Built-in via Service

### Database Persistence

**PostgreSQL Backup**:
```bash
docker exec <container> pg_dump -U postgres PlcLab > backup.sql
```

**Restore**:
```bash
docker exec -i <container> psql -U postgres PlcLab < backup.sql
```

## Monitoring and Observability

### Logs

**View logs**:
```bash
docker logs <container>
docker logs -f <container>  # Follow
```

**Log aggregation**:
```bash
docker logs <container> | grep ERROR
```

### Metrics

**Prometheus scraping**:
```yaml
scrape_configs:
  - job_name: 'plclab'
    static_configs:
      - targets: ['localhost:8080']
    metrics_path: '/metrics'
```

### Tracing

**Jaeger UI**: http://localhost:16686

**Query traces**:
```bash
curl http://localhost:16686/api/traces?service=PlcLab.Web
```

## Troubleshooting

### Image Pull Issues

```bash
# Check credentials
docker login ghcr.io

# Verify image exists
docker search ghcr.io/jhobel85/plclab-docker-opc-blazor

# Pull with verbose output
docker pull -v ghcr.io/jhobel85/plclab-docker-opc-blazor:latest
```

### Container Startup Issues

```bash
# Check container logs
docker logs <container-id>

# Inspect container
docker inspect <container-id>

# Execute diagnostic command
docker exec <container-id> curl -v http://localhost:8080/health
```

### Performance Issues

```bash
# Monitor resource usage
docker stats

# Check process list inside container
docker exec <container-id> ps aux
```

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [OCI Image Format](https://github.com/opencontainers/image-spec)
