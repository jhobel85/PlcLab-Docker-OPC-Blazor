# Start OPC UA Reference Server (virtual PLC)

## Docker (recommended)
```bash
# Using Microsoft OPC PLC simulator
docker run --rm -p 4840:50000 mcr.microsoft.com/iotedge/opc-plc:latest
```

## Docker Compose (for local development)
```bash
# Start only the OPC UA server
docker-compose up opcua-refserver
```

## Configure client endpoint
Set `OpcUa:Endpoint` to `opc.tcp://localhost:4840` (for local Docker or direct run).

For Docker Compose, the web app uses `opc.tcp://opcua-refserver:50000` internally.


## Test Port Connectivity (TCP Ping)
External port (localhost:4840):
`Test-NetConnection -ComputerName localhost -Port 4840`

Internal port (opcua-refserver:50000) (from another container or host with Docker network access):
`docker exec -it <web_container_id> nc -zv opcua-refserver 50000`



