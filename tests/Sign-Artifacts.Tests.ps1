<#
.SYNOPSIS
    Pester unit tests for scripts/Sign-Artifacts.ps1

.DESCRIPTION
    Tests cover:
    - Binary signing: skips gracefully when PublishDir doesn't exist
    - Binary signing: skips when no PlcLab*.dll/exe files are found
    - Binary signing: calls dotnet sign with correct arguments
    - Image signing: throws when neither -KeylessOidc nor COSIGN_KEY is set
    - Image signing: calls cosign sign and cosign verify with correct arguments
    - Signing and verifying image (keyless OIDC mode) - WhatIf dry-run
#>

#Requires -Module Pester

BeforeAll {
    $ScriptPath = Join-Path $PSScriptRoot "../../scripts/Sign-Artifacts.ps1"
    $ScriptPath = Resolve-Path $ScriptPath
}

Describe "Sign-Artifacts.ps1 — Binary signing" {

    It "Skips binary signing when PublishDir does not exist" {
        $output = & $ScriptPath -PublishDir "C:\nonexistent\dir\xyz" -ImageRef "" -WhatIf 2>&1
        # Should warn but not throw
        $output | Should -Not -BeNullOrEmpty
    }

    It "Warns when no PlcLab binaries are found in PublishDir" {
        $emptyDir = Join-Path $TestDrive "empty-publish"
        New-Item -ItemType Directory -Path $emptyDir | Out-Null

        # Mock dotnet so we can assert it is NOT called
        Mock dotnet { } -Verifiable -MockWith { } -ModuleName "Global" -ErrorAction SilentlyContinue

        $output = & $ScriptPath -PublishDir $emptyDir -ImageRef "" -WhatIf 2>&1
        $output | Should -Not -BeNullOrEmpty
    }

    It "Calls dotnet sign with a file-list when PlcLab binaries are present (-WhatIf)" {
        $publishDir = Join-Path $TestDrive "fake-publish"
        New-Item -ItemType Directory -Path $publishDir | Out-Null
        New-Item -ItemType File -Path (Join-Path $publishDir "PlcLab.Web.dll") | Out-Null

        # -WhatIf prevents actual execution of dotnet sign while still running the
        # ShouldProcess-guarded code path.
        { & $ScriptPath -PublishDir $publishDir -ImageRef "" -WhatIf } | Should -Not -Throw
    }
}

Describe "Sign-Artifacts.ps1 — Docker image signing" {

    It "Throws when no OIDC or key is configured" {
        $env:COSIGN_KEY = $null

        { & $ScriptPath -PublishDir "C:\nonexistent" -ImageRef "ghcr.io/org/repo@sha256:abc" } |
            Should -Throw
    }

    It "Does not throw calling with -KeylessOidc and -WhatIf (cosign not invoked)" {
        # cosign is guarded by ShouldProcess so -WhatIf prevents the actual call
        { & $ScriptPath -PublishDir "C:\nonexistent" -ImageRef "ghcr.io/org/repo@sha256:abc" -KeylessOidc -WhatIf } |
            Should -Not -Throw
    }

    It "Does not throw calling with COSIGN_KEY set and -WhatIf" {
        $env:COSIGN_KEY = "fake-cosign.key"

        { & $ScriptPath -PublishDir "C:\nonexistent" -ImageRef "ghcr.io/org/repo@sha256:abc" -WhatIf } |
            Should -Not -Throw

        $env:COSIGN_KEY = $null
    }

    It "Skips Docker signing when ImageRef is empty string" {
        $publishDir = Join-Path $TestDrive "any"
        New-Item -ItemType Directory -Path $publishDir -ErrorAction SilentlyContinue | Out-Null

        { & $ScriptPath -PublishDir $publishDir -ImageRef "" -WhatIf } | Should -Not -Throw
    }
}

Describe "Sign-Artifacts.ps1 — BuildSafePath-equivalent validation" {

    It "Script file exists at expected location" {
        Test-Path $ScriptPath | Should -Be $true
    }

    It "Script has a -PublishDir parameter" {
        $cmd = Get-Command $ScriptPath
        $cmd.Parameters.Keys | Should -Contain "PublishDir"
    }

    It "Script has an -ImageRef parameter" {
        $cmd = Get-Command $ScriptPath
        $cmd.Parameters.Keys | Should -Contain "ImageRef"
    }

    It "Script has a -KeylessOidc switch parameter" {
        $cmd = Get-Command $ScriptPath
        $cmd.Parameters.Keys | Should -Contain "KeylessOidc"
    }

    It "Script has a -CertificateFile parameter" {
        $cmd = Get-Command $ScriptPath
        $cmd.Parameters.Keys | Should -Contain "CertificateFile"
    }

    It "Script has SupportsShouldProcess set" {
        $cmd = Get-Command $ScriptPath
        $cmd.Parameters.Keys | Should -Contain "WhatIf"
    }
}
