# Deploy to Kubernetes (optional)

## Verify Docker Image Signature Before Deployment

Before applying any K8s manifests, verify the image signatures so only
CI-signed images are admitted into the cluster:

```bash
cosign verify \
  --certificate-identity-regexp "https://github.com/yourorg/PlcLab-Docker-OPC-Blazor/" \
  --certificate-oidc-issuer "https://token.actions.githubusercontent.com" \
  ghcr.io/yourorg/plclab@sha256:<digest>
```

For automated enforcement, integrate cosign with
[Kyverno](https://kyverno.io/docs/writing-policies/verify-images/) or
[Connaisseur](https://sse-secure-systems.github.io/connaisseur/) admission
controllers to reject unsigned images cluster-wide.

See [SigningGuide.md](SigningGuide.md) for full details.

---

- Verify image signature (see above).
- Create a Deployment for the web app.
- Create a Service (LoadBalancer/Ingress) for HTTP.
- Use ConfigMaps/Secrets for configuration and certificates.
- Add a readiness probe pointing to `/healthz` (the built-in health check endpoint).
- Optional: add the OPC UA reference server as a separate Deployment for demo.
