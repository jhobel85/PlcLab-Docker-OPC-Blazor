# Certificates Guide (OPC UA)

The OPC UA client uses X.509 certificates for secure connections.

## Generate a self-signed client cert (example)
> Use platform tools (PowerShell, OpenSSL) or Opc.Ua configuration helpers in code.

## Trust workflow
- Client trusts server certificate.
- Server trusts client certificate (if mutual auth required).

## Docker OPC UA Reference Server: Trust List Steps

When running the OPC UA Reference Server in Docker, certificate trust is managed via mounted volumes or by copying certs into the container. Typical workflow:

1. **Start the reference server** (see `docker-compose.yml`).
2. **Connect with your client** (e.g., PlcLab.Web). The server will generate a certificate for the client in its `pki/rejected` folder.
3. **Access the container**:
	- `docker compose exec opcua-refserver sh`
	- Navigate to `/app/pki/rejected` and `/app/pki/trusted`.
4. **Move the client certificate** from `rejected` to `trusted` to allow secure connections.
5. **Restart the server** if needed for changes to take effect.

> For persistent trust, mount a host directory to `/app/pki` in `docker-compose.yml`.

**Note:** The client (PlcLab.Web) should also maintain its own trust list for the server certificate. See the client app's documentation for details.

## Best practices
- Keep `AutoAcceptUntrustedCertificates = false` outside dev.
- Rotate certificates periodically and revoke compromised ones.
