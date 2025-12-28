# Certificates Guide (OPC UA)

The OPC UA client uses X.509 certificates for secure connections.

## Generate a self-signed client cert (example)
> Use platform tools (PowerShell, OpenSSL) or Opc.Ua configuration helpers in code.

## Trust workflow
- Client trusts server certificate.
- Server trusts client certificate (if mutual auth required).

## Best practices
- Keep `AutoAcceptUntrustedCertificates = false` outside dev.
- Rotate certificates periodically and revoke compromised ones.
