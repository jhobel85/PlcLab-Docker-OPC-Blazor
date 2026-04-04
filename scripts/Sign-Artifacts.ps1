<#
.SYNOPSIS
    Signs PlcLab application binaries and/or Docker images.

.DESCRIPTION
    - .NET binaries: uses `dotnet sign` (cross-platform) to sign DLLs and EXEs
      published under a given directory.
    - Docker images: uses `cosign` to sign a container image by digest using
      keyless signing (GitHub Actions OIDC) or a key file.

.PARAMETER PublishDir
    Path to the directory containing published .NET binaries. Defaults to ./out.

.PARAMETER ImageRef
    Fully-qualified image reference (registry/image@sha256:...) to sign with
    cosign. Pass an empty string to skip Docker image signing.

.PARAMETER CertificateFile
    Path to a PFX certificate file for binary signing. If omitted, dotnet sign
    uses the first available code-signing certificate from the current user store.

.PARAMETER CertificatePassword
    Password for the PFX certificate file (SecureString). Can be piped.

.PARAMETER KeylessOidc
    When set, cosign signs the Docker image using keyless OIDC (Sigstore). This
    is the recommended mode for GitHub Actions environments.

.EXAMPLE
    # Sign binaries only
    ./scripts/Sign-Artifacts.ps1 -PublishDir ./out -ImageRef ""

.EXAMPLE
    # Sign both binaries and a Docker image (keyless, for CI)
    ./scripts/Sign-Artifacts.ps1 `
        -PublishDir ./out `
        -ImageRef "ghcr.io/myorg/plclab@sha256:abc123" `
        -KeylessOidc

.EXAMPLE
    # Sign with a PFX certificate
    ./scripts/Sign-Artifacts.ps1 `
        -PublishDir ./out `
        -CertificateFile ./certs/codesign.pfx `
        -CertificatePassword (ConvertTo-SecureString "secret" -AsPlainText -Force) `
        -ImageRef ""
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $PublishDir = "./out",
    [string] $ImageRef = "",
    [string] $CertificateFile = "",
    [SecureString] $CertificatePassword,
    [switch] $KeylessOidc
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Helpers ────────────────────────────────────────────────────────────────────

function Assert-Command([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Required tool not found: '$name'. Please install it and ensure it is on PATH."
    }
}

function Write-Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

# ── Sign .NET Binaries ─────────────────────────────────────────────────────────

function Sign-DotNetBinaries {
    Write-Step "Signing .NET binaries in '$PublishDir'"

    Assert-Command "dotnet"

    $resolvedDir = Resolve-Path $PublishDir -ErrorAction Stop

    $binaries = @(
        Get-ChildItem -Path $resolvedDir -Recurse -Include "*.dll", "*.exe" |
            Where-Object { $_.Name -like "PlcLab*" }
    )

    if ($binaries.Count -eq 0) {
        Write-Warning "No PlcLab*.dll / PlcLab*.exe files found in '$resolvedDir'. Nothing to sign."
        return
    }

    Write-Host "  Found $($binaries.Count) binary/binaries to sign."

    $signArgs = @(
        "sign",
        "--file-list", ($binaries.FullName -join "`n" | & { process { $_ } } | Out-String).Trim()
    )

    # Build file-list temp file
    $listFile = [System.IO.Path]::GetTempFileName()
    try {
        $binaries.FullName | Set-Content -Path $listFile -Encoding utf8

        $signArgs = @("sign", "--file-list", $listFile)

        if ($CertificateFile -ne "") {
            $cert = Resolve-Path $CertificateFile -ErrorAction Stop
            $signArgs += @("--certificate-path", $cert.Path)

            if ($null -ne $CertificatePassword) {
                $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($CertificatePassword)
                try {
                    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
                    $signArgs += @("--certificate-password", $plain)
                } finally {
                    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
                }
            }
        }

        if ($PSCmdlet.ShouldProcess($resolvedDir, "Sign .NET binaries")) {
            & dotnet @signArgs
            if ($LASTEXITCODE -ne 0) {
                throw "dotnet sign exited with code $LASTEXITCODE"
            }
            Write-Host "  Binaries signed successfully." -ForegroundColor Green
        }
    } finally {
        Remove-Item $listFile -Force -ErrorAction SilentlyContinue
    }
}

# ── Sign Docker Image with cosign ─────────────────────────────────────────────

function Sign-DockerImage([string]$imageRef) {
    Write-Step "Signing Docker image: $imageRef"

    Assert-Command "cosign"

    $cosignArgs = @("sign", "--yes")

    if ($KeylessOidc) {
        # Keyless signing via Sigstore / GitHub Actions OIDC
        Write-Host "  Using keyless OIDC signing (Sigstore)."
        $cosignArgs += $imageRef
    } elseif ($env:COSIGN_KEY) {
        Write-Host "  Using key from COSIGN_KEY environment variable."
        $cosignArgs += @("--key", "env://COSIGN_KEY", $imageRef)
    } else {
        throw "Provide -KeylessOidc or set the COSIGN_KEY environment variable (path to cosign.key)."
    }

    if ($PSCmdlet.ShouldProcess($imageRef, "Sign Docker image with cosign")) {
        & cosign @cosignArgs
        if ($LASTEXITCODE -ne 0) {
            throw "cosign sign exited with code $LASTEXITCODE"
        }
        Write-Host "  Docker image signed successfully." -ForegroundColor Green
    }
}

# ── Verify Docker Image signature ─────────────────────────────────────────────

function Verify-DockerImage([string]$imageRef) {
    Write-Step "Verifying Docker image signature: $imageRef"

    Assert-Command "cosign"

    $verifyArgs = @("verify")

    if ($KeylessOidc) {
        $verifyArgs += @(
            "--certificate-identity-regexp", "https://github.com/",
            "--certificate-oidc-issuer", "https://token.actions.githubusercontent.com"
        )
    } elseif ($env:COSIGN_KEY) {
        $verifyArgs += @("--key", "env://COSIGN_KEY")
    }

    $verifyArgs += $imageRef

    if ($PSCmdlet.ShouldProcess($imageRef, "Verify Docker image signature")) {
        & cosign @verifyArgs
        if ($LASTEXITCODE -ne 0) {
            throw "cosign verify failed — image signature is invalid or absent."
        }
        Write-Host "  Signature verified." -ForegroundColor Green
    }
}

# ── Main ───────────────────────────────────────────────────────────────────────

Write-Host "PlcLab Artifact Signing" -ForegroundColor White
Write-Host "========================" -ForegroundColor White

if (Test-Path $PublishDir) {
    Sign-DotNetBinaries
} else {
    Write-Warning "PublishDir '$PublishDir' does not exist. Skipping binary signing."
}

if ($ImageRef -ne "") {
    Sign-DockerImage $ImageRef
    Verify-DockerImage $ImageRef
} else {
    Write-Host "`nNo ImageRef provided — skipping Docker image signing." -ForegroundColor Yellow
}

Write-Host "`nAll signing tasks completed." -ForegroundColor Green
