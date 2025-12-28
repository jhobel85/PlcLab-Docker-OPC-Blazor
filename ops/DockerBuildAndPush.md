# Docker Build & Push

## Build locally
```bash
docker build -t yourrepo/plclab:dev -f src/PlcLab.Web/Dockerfile .
```

## Run locally
```bash
docker run --rm -p 8080:8080   -e ASPNETCORE_URLS=http://+:8080   -e OpcUa__Endpoint=opc.tcp://host.docker.internal:4840   yourrepo/plclab:dev
```

## Push to registry
```bash
docker tag yourrepo/plclab:dev yourrepo/plclab:v0.1.0
docker push yourrepo/plclab:v0.1.0
```

> Add authentication to your registry first (Docker Hub or GHCR).
