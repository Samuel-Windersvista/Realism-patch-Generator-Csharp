param(
    [string]$Version = "1.1.0",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Configuration = "Release",
    [string]$ArtifactsRoot = "artifacts\release",
    [switch]$FrameworkDependent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot ".."))
$releaseRoot = Join-Path $repoRoot $ArtifactsRoot
$deploymentMode = if ($FrameworkDependent) { "framework-dependent" } else { "self-contained" }
$packageSuffix = if ($FrameworkDependent) { "-fd" } else { "" }
$packageName = "RealismPatchGenerator-v$Version-$RuntimeIdentifier$packageSuffix"
$packageRoot = Join-Path $releaseRoot $packageName
$tempRoot = Join-Path $releaseRoot "_tmp_$packageName"
$guiPublishRoot = Join-Path $tempRoot "gui"
$cliPublishRoot = Join-Path $tempRoot "cli"
$zipPath = Join-Path $releaseRoot ($packageName + ".zip")

function Reset-Directory {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Get-TemplatesDirectory {
    param([string]$Root)

    $preferredPath = Join-Path $Root "现实主义物品模板"
    if (Test-Path -LiteralPath $preferredPath) {
        return Get-Item -LiteralPath $preferredPath
    }

    $knownNames = @(
        ".vs",
        ".git",
        "artifacts",
        "audit_reports",
        "docs",
        "input",
        "output",
        "rules",
        "scripts"
    )

    return Get-ChildItem -LiteralPath $Root -Directory |
        Where-Object {
            $knownNames -notcontains $_.Name -and
            -not $_.Name.StartsWith("RealismPatchGenerator", [System.StringComparison]::OrdinalIgnoreCase)
        } |
        Select-Object -First 1
}

Reset-Directory -Path $releaseRoot
New-Item -ItemType Directory -Path $tempRoot | Out-Null

$guiProject = Join-Path $repoRoot "RealismPatchGenerator.Gui\RealismPatchGenerator.Gui.csproj"
$cliProject = Join-Path $repoRoot "RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj"
$templatesDirectory = Get-TemplatesDirectory -Root $repoRoot

if ($null -eq $templatesDirectory) {
    throw "Template directory not found under repository root."
}

$selfContainedValue = if ($FrameworkDependent) { "false" } else { "true" }
$publishSingleFileValue = if ($FrameworkDependent) { "false" } else { "true" }
$includeNativeLibrariesValue = if ($FrameworkDependent) { "false" } else { "true" }

dotnet publish $guiProject `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained $selfContainedValue `
    -p:PublishSingleFile=$publishSingleFileValue `
    -p:IncludeNativeLibrariesForSelfExtract=$includeNativeLibrariesValue `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $guiPublishRoot

dotnet publish $cliProject `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained $selfContainedValue `
    -p:PublishSingleFile=$publishSingleFileValue `
    -p:IncludeNativeLibrariesForSelfExtract=$includeNativeLibrariesValue `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $cliPublishRoot

Reset-Directory -Path $packageRoot

Copy-Item -Path (Join-Path $guiPublishRoot "*") -Destination $packageRoot -Recurse -Force
if ($FrameworkDependent) {
    Copy-Item -Path (Join-Path $cliPublishRoot "*") -Destination $packageRoot -Recurse -Force
}
else {
    Copy-Item -Path (Join-Path $cliPublishRoot "RealismPatchGenerator.Cli.exe") -Destination $packageRoot -Force
}

Copy-Item -Path (Join-Path $repoRoot "README.md") -Destination $packageRoot -Force
Copy-Item -Path (Join-Path $repoRoot "CHANGELOG.md") -Destination $packageRoot -Force
Copy-Item -Path (Join-Path $repoRoot "docs") -Destination $packageRoot -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "rules") -Destination $packageRoot -Recurse -Force
Copy-Item -Path (Join-Path $repoRoot "input") -Destination $packageRoot -Recurse -Force
Copy-Item -Path $templatesDirectory.FullName -Destination $packageRoot -Recurse -Force

New-Item -ItemType Directory -Path (Join-Path $packageRoot "output") | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot "audit_reports") | Out-Null

$releaseInfo = @"
SPT Realism Patch Generator v$Version
Build: $Configuration
Runtime: $RuntimeIdentifier
Deployment: $deploymentMode

Entry points:
- RealismPatchGenerator.Gui.exe: GUI application
- RealismPatchGenerator.Cli.exe: CLI generate and audit tool

Bundled data directories:
- input
- $($templatesDirectory.Name)
- rules
- docs
- output
- audit_reports

Recommended usage:
- Use GUI for rule editing and item exception management
- Use CLI for batch generation and audits

Runtime requirement:
- $(if ($FrameworkDependent) { '.NET Desktop Runtime / .NET Runtime must already be installed on the target machine.' } else { 'No preinstalled .NET runtime is required on the target machine.' })

Packaging mode:
- $(if ($FrameworkDependent) { 'Multi-file publish optimized for smaller package size.' } else { 'Single-file publish optimized for no-install distribution.' })
"@

Set-Content -Path (Join-Path $packageRoot "RELEASE.txt") -Value $releaseInfo -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}

Compress-Archive -Path $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item -Path $tempRoot -Recurse -Force

Write-Host "发布目录: $packageRoot"
Write-Host "发布压缩包: $zipPath"