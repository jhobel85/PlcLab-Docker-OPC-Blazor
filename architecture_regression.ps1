<#
    Full Architecture Extraction + SOLID Scoring
    --------------------------------------------
    Generates:
      - structure.txt
      - classes.txt
      - methods.txt
      - properties.txt
      - di_registrations.txt
      - dependencies.txt
      - metrics.txt
      - solid_score.txt
#>


param(
    [string]$Root = $PSScriptRoot
)

# Output folder
$OutDir = Join-Path $Root 'architecture'
if (-not (Test-Path $OutDir)) {
    New-Item -Path $OutDir -ItemType Directory | Out-Null
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -Path $Root

$excludeDirs = @('.git', '.vs', 'bin', 'obj', 'node_modules')

function Test-Excluded {
    param([string]$Path)
    foreach ($dir in $excludeDirs) {
        if ($Path -like "*\$dir\*") { return $true }
    }
    return $false
}

function Get-CodeFiles {
    param([string[]]$Patterns = @('*.cs', '*.java'))
    Get-ChildItem -Path $Root -Recurse -File -Include $Patterns -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-Excluded $_.FullName) }
}

Write-Host "=== Architecture Extraction + SOLID Scoring Started ==="


# 1) Project structure (exclude heavy folders)
Get-ChildItem -Path $Root -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) } |
    ForEach-Object { $_.FullName.Substring($Root.Length + 1) } |
    Sort-Object |
    Out-File (Join-Path $OutDir 'structure.txt')


$codeFiles = Get-CodeFiles


# 2) Class/interface declarations
$codeFiles |
    Select-String -Pattern "class |interface " |
    ForEach-Object {
        "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    } | Out-File (Join-Path $OutDir 'classes.txt')


# 3) Method signatures
$codeFiles |
    Select-String -Pattern "public |private |protected |internal " |
    Where-Object { $_.Line -match "\(.*\)" -and $_.Line -notmatch "class " } |
    ForEach-Object {
        "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    } | Out-File (Join-Path $OutDir 'methods.txt')


# 4) Properties
$codeFiles |
    Select-String -Pattern "public .*{ get;" |
    ForEach-Object {
        "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    } | Out-File (Join-Path $OutDir 'properties.txt')


# 5) DI registrations
$diPatterns = @('AddSingleton', 'AddScoped', 'AddTransient', 'AddDbContext', 'AddHttpClient')
$codeFiles |
    Select-String -Pattern ($diPatterns -join "|") |
    ForEach-Object {
        "$($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    } | Out-File (Join-Path $OutDir 'di_registrations.txt')


# 6) Dependencies
$csproj = Get-ChildItem -Path $Root -Recurse -File -Filter *.csproj -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) } |
    Select-Object -First 1
$pom = Get-ChildItem -Path $Root -Recurse -File -Filter pom.xml -ErrorAction SilentlyContinue |
    Where-Object { -not (Test-Excluded $_.FullName) } |
    Select-Object -First 1
if ($csproj) {
    dotnet list $csproj.FullName package > (Join-Path $OutDir 'dependencies.txt')
} elseif ($pom) {
    mvn -f $pom.FullName dependency:tree > (Join-Path $OutDir 'dependencies.txt')
} else {
    "No .NET or Java project detected." | Out-File (Join-Path $OutDir 'dependencies.txt')
}

# 7) Metrics


# 7) Metrics
$metrics = foreach ($file in $codeFiles) {
    $lines = (Get-Content $file.FullName -ErrorAction SilentlyContinue).Count
    $complexityMatches = Select-String -Path $file.FullName -Pattern "if|for|while|case|switch" -ErrorAction SilentlyContinue
    if ($null -eq $complexityMatches) {
        $complexity = 0
    } elseif ($complexityMatches -is [array]) {
        $complexity = $complexityMatches.Length
    } else {
        $complexity = 1
    }
    [PSCustomObject]@{ File = $file.FullName; Lines = $lines; Complexity = $complexity }
}
$metrics | Sort-Object -Property Complexity -Descending |
    Format-Table -AutoSize |
    Out-File (Join-Path $OutDir 'metrics.txt')


# 8) SOLID scoring
Write-Host "Calculating SOLID compliance score..."

$methodCount = (Get-Content (Join-Path $OutDir 'methods.txt')).Count
$classCount = [math]::Max(1, (Get-Content (Join-Path $OutDir 'classes.txt')).Count) # avoid divide-by-zero when no classes
$propertyCount = (Get-Content (Join-Path $OutDir 'properties.txt')).Count
$diCount = (Get-Content (Join-Path $OutDir 'di_registrations.txt')).Count

# Heuristic scoring
$SRP = [math]::Max(0, 10 - [math]::Floor($methodCount / $classCount))
$OCP = [math]::Min(10, [math]::Floor($diCount / 2))
$LSP = [math]::Min(10, [math]::Floor($classCount / 5))
$ISP = [math]::Max(0, 10 - [math]::Floor($propertyCount / $classCount))
$DIP = [math]::Min(10, $diCount)


# Output
@"
=== SOLID Compliance Score ===

SRP (Single Responsibility):     $SRP / 10
OCP (Open/Closed):               $OCP / 10
LSP (Liskov Substitution):       $LSP / 10
ISP (Interface Segregation):     $ISP / 10
DIP (Dependency Inversion):      $DIP / 10

TOTAL SCORE:                     $($SRP + $OCP + $LSP + $ISP + $DIP) / 50
"@ | Out-File (Join-Path $OutDir 'solid_score.txt')

Write-Host "=== Extraction + Scoring Complete ==="
Write-Host "Generated files in architecture/ folder:"
Write-Host " - structure.txt"
Write-Host " - classes.txt"
Write-Host " - methods.txt"
Write-Host " - properties.txt"
Write-Host " - di_registrations.txt"
Write-Host " - dependencies.txt"
Write-Host " - metrics.txt"
Write-Host " - solid_score.txt"
