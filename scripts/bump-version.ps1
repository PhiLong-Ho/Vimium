#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bump the Vimium version across all files that contain version metadata.

.DESCRIPTION
    Updates the version in:
      - src\Vimium\Vimium.csproj    (<ApplicationVersion>)
      - src\SolutionInfo.cs          (AssemblyVersion, AssemblyFileVersion, internal const)

.PARAMETER Version
    The new version string. Must be in MAJOR.MINOR.PATCH.REVISION format (e.g. 1.4.2.0).

.PARAMETER WhatIf
    Show what would change without modifying files.

.EXAMPLE
    .\scripts\bump-version.ps1 1.4.3.0
    .\scripts\bump-version.ps1 1.5.0.0 -WhatIf
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $Version,

    [switch] $WhatIf
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

$csprojPath = Join-Path $repoRoot 'src\Vimium\Vimium.csproj'
$infoPath   = Join-Path $repoRoot 'src\SolutionInfo.cs'

foreach ($p in @($csprojPath, $infoPath)) {
    if (-not (Test-Path $p)) { Write-Error "Not found: $p"; exit 1 }
}

# ── Read current version from csproj ─────────────────────────────

$csproj = Get-Content $csprojPath -Raw
$oldVersion = [regex]::Match($csproj, '<ApplicationVersion>([^<]+)</ApplicationVersion>').Groups[1].Value
if (-not $oldVersion) {
    Write-Error 'Could not find <ApplicationVersion> in Vimium.csproj'
    exit 1
}

Write-Host "Current version: $oldVersion"
Write-Host "New version:     $Version"

if ($oldVersion -eq $Version) {
    Write-Host 'Version already current — nothing to do.'
    exit 0
}

# ── Bump ─────────────────────────────────────────────────────────

function Replace-InFile($Path, $Pattern, $Replacement, $Label) {
    $content = Get-Content $Path -Raw
    $newContent = [regex]::Replace($content, $Pattern, $Replacement)
    if (-not $WhatIf) {
        Set-Content $Path -Value $newContent -NoNewline
    }
    Write-Host "  $Label"
}

Write-Host "`nBumping to $Version..."

Replace-InFile $csprojPath `
    '<ApplicationVersion>[^<]+</ApplicationVersion>' `
    "<ApplicationVersion>$Version</ApplicationVersion>" `
    '[1/2] Vimium.csproj'

Replace-InFile $infoPath `
    'AssemblyVersionAttribute\("[^"]+"\)' `
    "AssemblyVersionAttribute(""$Version"")" `
    '[2/2] SolutionInfo.cs (AssemblyVersion)'

Replace-InFile $infoPath `
    'AssemblyFileVersionAttribute\("[^"]+"\)' `
    "AssemblyFileVersionAttribute(""$Version"")" `
    '     SolutionInfo.cs (AssemblyFileVersion)'

Replace-InFile $infoPath `
    'internal const string Version = "[^"]+"' `
    "internal const string Version = ""$Version""" `
    '     SolutionInfo.cs (const Version)'

if ($WhatIf) {
    Write-Host "`n--- WhatIf: no files modified ---"
} else {
    Write-Host "`nDone: $oldVersion -> $Version"
}
