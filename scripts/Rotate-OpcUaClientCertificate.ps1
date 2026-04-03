param(
    [string]$PkiRoot = "./pki",
    [string]$Subject = "CN=PlcLabClient",
    [int]$ValidityDays = 365,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

$resolvedRoot = (Resolve-Path -Path (Join-Path $PWD $PkiRoot) -ErrorAction SilentlyContinue)
if (-not $resolvedRoot) {
    Ensure-Directory -Path $PkiRoot
    $resolvedRoot = Resolve-Path -Path $PkiRoot
}

$pkiRootPath = $resolvedRoot.Path
$ownDir = Join-Path $pkiRootPath "own"
$trustedDir = Join-Path $pkiRootPath "trusted"
$rejectedDir = Join-Path $pkiRootPath "rejected"
$archiveDir = Join-Path $ownDir "archive"

Ensure-Directory -Path $ownDir
Ensure-Directory -Path $trustedDir
Ensure-Directory -Path $rejectedDir
Ensure-Directory -Path $archiveDir

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$newBaseName = "PlcLabClient-$timestamp"
$pfxPath = Join-Path $ownDir "$newBaseName.pfx"
$cerPath = Join-Path $ownDir "$newBaseName.cer"

$existingCurrentPfx = Join-Path $ownDir "client.current.pfx"
$existingCurrentCer = Join-Path $ownDir "client.current.cer"

if (((Test-Path $existingCurrentPfx) -or (Test-Path $existingCurrentCer)) -and -not $Force) {
    throw "Existing current certificate found. Use -Force to rotate and archive old files."
}

if (Test-Path $existingCurrentPfx) {
    Move-Item -Path $existingCurrentPfx -Destination (Join-Path $archiveDir "client.$timestamp.old.pfx") -Force
}

if (Test-Path $existingCurrentCer) {
    Move-Item -Path $existingCurrentCer -Destination (Join-Path $archiveDir "client.$timestamp.old.cer") -Force
}

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddDays($ValidityDays) `
    -CertStoreLocation "cert:\CurrentUser\My"

if (-not $cert) {
    throw "Certificate creation failed."
}

$randomPassword = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
$securePassword = ConvertTo-SecureString -String $randomPassword -AsPlainText -Force

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Copy-Item -Path $pfxPath -Destination $existingCurrentPfx -Force
Copy-Item -Path $cerPath -Destination $existingCurrentCer -Force

$passwordFile = Join-Path $ownDir "client.current.password.txt"
Set-Content -Path $passwordFile -Value $randomPassword -NoNewline

Write-Host "Created new OPC UA client certificate"
Write-Host "PKI root: $pkiRootPath"
Write-Host "Current cert: $existingCurrentCer"
Write-Host "Current key:  $existingCurrentPfx"
Write-Host "Password file: $passwordFile"
Write-Host "Thumbprint: $($cert.Thumbprint)"
Write-Host "Valid to: $($cert.NotAfter.ToUniversalTime().ToString('u'))"
