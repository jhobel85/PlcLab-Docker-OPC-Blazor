# Signing Guide

This document describes how PlcLab Docker images and .NET application binaries are signed and how to verify those signatures before deployment.

---

## Tools

| Tool | Purpose | Install |
|---|---|---|
| [`cosign`](https://docs.sigstore.dev/cosign/overview/) | Sign / verify Docker images | `brew install cosign` \| `winget install sigstore.cosign` |
| [`dotnet sign`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-sign) | Sign .NET DLLs and EXEs | Included with .NET SDK 8+ |

---

## Docker Image Signing (cosign)

### Keyless signing (GitHub Actions — recommended)

All images built and pushed by the CI pipeline are signed automatically using
[Sigstore keyless signing](https://docs.sigstore.dev/cosign/signing/overview/).
No long-lived keys are stored; the identity is bound to the GitHub Actions OIDC
token.

The CI step runs after every successful push on `main` or a semver tag:

```yaml
- uses: sigstore/cosign-installer@v3
- run: cosign sign --yes $IMAGE_DIGEST
  env:
    COSIGN_EXPERIMENTAL: 1
```

### Local signing with a key pair

```powershell
# Generate a key pair (once)
cosign generate-key-pair            # creates cosign.key / cosign.pub

# Sign
cosign sign --key cosign.key ghcr.io/yourorg/plclab@sha256:<digest>

# Verify
cosign verify --key cosign.pub ghcr.io/yourorg/plclab@sha256:<digest>
```

### Using the helper script

```powershell
# Sign both binaries and a Docker image (keyless CI mode)
./scripts/Sign-Artifacts.ps1 `
    -PublishDir ./out `
    -ImageRef "ghcr.io/yourorg/plclab@sha256:<digest>" `
    -KeylessOidc

# Sign with a PFX certificate (local dev)
./scripts/Sign-Artifacts.ps1 `
    -PublishDir ./out `
    -CertificateFile ./certs/codesign.pfx `
    -CertificatePassword (ConvertTo-SecureString "secret" -AsPlainText -Force) `
    -ImageRef ""
```

---

## .NET Binary Signing

The `scripts/Sign-Artifacts.ps1` script uses `dotnet sign` to sign all
`PlcLab*.dll` / `PlcLab*.exe` files in the publish output directory.

### Prerequisites

- .NET SDK 8+ (includes `dotnet sign`)
- A code-signing certificate (PFX file or one installed in the current-user
  certificate store, EKU `1.3.6.1.5.5.7.3.3`)

### Obtaining a certificate

| Environment | Recommended source |
|---|---|
| Development | Self-signed via `New-SelfSignedCertificate` (Windows) |
| Staging | Internal PKI / ADCS |
| Production | Trusted CA (DigiCert, Sectigo, etc.) |

### Verifying a signed binary

```powershell
Get-AuthenticodeSignature ./out/PlcLab.Web.dll | Select-Object Status, SignerCertificate
```

Expected output:

```
Status SignerCertificate
------ -----------------
  Valid CN=PlcLab CodeSign, ...
```

---

## Verifying Signatures Before Deployment

See the deployment-specific verification steps in:

- [DeployToAzure.md](DeployToAzure.md) — Azure App Service / Web App
- [DeployToK8s.md](DeployToK8s.md) — Kubernetes

### Quick check (keyless, GitHub Actions OIDC)

```bash
cosign verify \
  --certificate-identity-regexp "https://github.com/yourorg/PlcLab-Docker-OPC-Blazor/" \
  --certificate-oidc-issuer "https://token.actions.githubusercontent.com" \
  ghcr.io/yourorg/plclab@sha256:<digest>
```

A successful output lists the verified claims JSON — if the command fails the
signature is absent or invalid and the image **must not** be deployed.

---

## Rotating the Signing Key

For key-based signing, rotate the `cosign.key` / `cosign.pub` pair:

1. `cosign generate-key-pair` → generates new `cosign.key` and `cosign.pub`.
2. Store `cosign.key` in GitHub Secrets (`COSIGN_PRIVATE_KEY`).
3. Publish `cosign.pub` in the repository so consumers can verify.
4. Revoke and delete the old key material.
