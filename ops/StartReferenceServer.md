# Start OPC UA Reference Server (virtual PLC)

## Docker (recommended)
```bash
# Example image/tag placeholder — replace with official image when chosen
# ghcr.io/opcfoundation/ua-reference-server:latest

docker run --rm -p 4840:4840 ghcr.io/opcfoundation/ua-reference-server:latest
```

## Configure client endpoint
Set `OpcUa:Endpoint` to `opc.tcp://localhost:4840` (or the container hostname in compose).
