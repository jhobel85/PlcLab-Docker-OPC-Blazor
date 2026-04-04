# Deploy to Azure (optional)

## Verify Docker Image Signature Before Deployment

Before pulling the image into Azure, verify its cosign signature to confirm the
image was built by the trusted CI pipeline:

```bash
cosign verify \
  --certificate-identity-regexp "https://github.com/yourorg/PlcLab-Docker-OPC-Blazor/" \
  --certificate-oidc-issuer "https://token.actions.githubusercontent.com" \
  ghcr.io/yourorg/plclab@sha256:<digest>
```

A successful result prints the verified claims. **Do not deploy** if this command
fails — the image may have been tampered with.

See [SigningGuide.md](SigningGuide.md) for full signing and key-rotation details.

---

## Azure App Service (container)
1. Verify image signature (see above).
2. Build and push image to registry.
3. Create Web App for Containers.
4. Configure env vars: `ASPNETCORE_URLS`, `OpcUa__Endpoint`.

## Azure Web App (code-based)
1. `dotnet publish` the web project.
2. Verify binary signatures: `Get-AuthenticodeSignature ./out/PlcLab.Web.dll`
3. Deploy with `az webapp deploy` or GitHub Actions.
