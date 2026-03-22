param(
    [string]$Version = "1.30.0",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Configuration = "Release",
    [string]$ArtifactsRoot = "artifacts\release",
    [switch]$FrameworkDependent,
    [switch]$BuildBoth
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot ".."))
$releaseRoot = Join-Path $repoRoot $ArtifactsRoot

function Reset-Directory {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Get-TemplatesDirectory {
    param([string]$Root)

    $preferredPath = Join-Path $Root "RealismItemTemplates"
    if (Test-Path -LiteralPath $preferredPath) {
        return Get-Item -LiteralPath $preferredPath
    }

    $structuredTemplateDirectory = Get-ChildItem -LiteralPath $Root -Directory |
        Where-Object {
            (Test-Path -LiteralPath (Join-Path $_.FullName "Ammo_templates.json")) -or
            (
                (Test-Path -LiteralPath (Join-Path $_.FullName "ammo")) -and
                (Test-Path -LiteralPath (Join-Path $_.FullName "weapons")) -and
                (Test-Path -LiteralPath (Join-Path $_.FullName "gear"))
            )
        } |
        Select-Object -First 1

    if ($null -ne $structuredTemplateDirectory) {
        return $structuredTemplateDirectory
    }

    $namedTemplateDirectory = Get-ChildItem -LiteralPath $Root -Directory |
        Where-Object { $_.Name -like "*模板*" } |
        Select-Object -First 1

    if ($null -ne $namedTemplateDirectory) {
        return $namedTemplateDirectory
    }

    $knownNames = @(
        ".venv",
        ".vs",
        ".git",
        "artifacts",
        "audit_reports",
        "bin",
        "docs",
        "input",
        "obj",
        "output",
        "Realism-patch-Generator-Output",
        "RealismItemRules",
        "scripts"
    )

    return Get-ChildItem -LiteralPath $Root -Directory |
        Where-Object {
            $knownNames -notcontains $_.Name -and
            -not $_.Name.StartsWith("seed_check", [System.StringComparison]::OrdinalIgnoreCase) -and
            -not $_.Name.StartsWith("RealismPatchGenerator", [System.StringComparison]::OrdinalIgnoreCase)
        } |
        Select-Object -First 1
}

function Copy-CommonPayload {
    param(
        [string]$Destination,
        [System.IO.DirectoryInfo]$TemplatesDirectory
    )

    Copy-Item -Path (Join-Path $repoRoot "README.md") -Destination $Destination -Force
    Copy-Item -Path (Join-Path $repoRoot "CHANGELOG.md") -Destination $Destination -Force
    Copy-Item -Path (Join-Path $repoRoot "docs") -Destination $Destination -Recurse -Force
    Copy-Item -Path (Join-Path $repoRoot "RealismItemRules") -Destination $Destination -Recurse -Force
    Copy-Item -Path (Join-Path $repoRoot "input") -Destination $Destination -Recurse -Force
    Copy-Item -Path $TemplatesDirectory.FullName -Destination $Destination -Recurse -Force

    New-Item -ItemType Directory -Path (Join-Path $Destination "output") | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $Destination "audit_reports") | Out-Null
}

function Write-ReleaseInfo {
    param(
        [string]$Destination,
        [string[]]$EntryPoints,
        [string[]]$RecommendedUsage,
        [System.IO.DirectoryInfo]$TemplatesDirectory,
        [string]$PackageKind,
        [bool]$IsFrameworkDependent,
        [string]$DeploymentMode
    )

    $entryPointText = ($EntryPoints | ForEach-Object { "- $_" }) -join [Environment]::NewLine
    $recommendedUsageText = ($RecommendedUsage | ForEach-Object { "- $_" }) -join [Environment]::NewLine

    $releaseInfo = @"
SPT Realism Patch Generator v$Version
Package: $PackageKind
Build: $Configuration
Runtime: $RuntimeIdentifier
Deployment: $deploymentMode

Entry points:
$entryPointText

Bundled data directories:
- input
- $($TemplatesDirectory.Name)
- RealismItemRules
- docs
- output
- audit_reports

Recommended usage:
$recommendedUsageText

Runtime requirement:
- $(if ($IsFrameworkDependent) { '.NET Desktop Runtime / .NET Runtime must already be installed on the target machine.' } else { 'No preinstalled .NET runtime is required on the target machine.' })

Packaging mode:
- $(if ($IsFrameworkDependent) { 'Multi-file publish optimized for smaller package size.' } else { 'Single-file publish optimized for no-install distribution.' })
"@

    Set-Content -Path (Join-Path $Destination "RELEASE.txt") -Value $releaseInfo -Encoding UTF8
}

function New-ZipFromDirectory {
    param(
        [string]$DirectoryPath,
        [string]$ZipFilePath
    )

    if (Test-Path $ZipFilePath) {
        Remove-Item -Path $ZipFilePath -Force
    }

    Compress-Archive -Path $DirectoryPath -DestinationPath $ZipFilePath -CompressionLevel Optimal
}

$guiProject = Join-Path $repoRoot "RealismPatchGenerator.Gui\RealismPatchGenerator.Gui.csproj"
$templatesDirectory = Get-TemplatesDirectory -Root $repoRoot

if ($null -eq $templatesDirectory) {
    throw "Template directory not found under repository root."
}

Reset-Directory -Path $releaseRoot

$packageModes = if ($BuildBoth) { @($false, $true) } else { @($FrameworkDependent.IsPresent) }

foreach ($isFrameworkDependent in $packageModes) {
    $deploymentMode = if ($isFrameworkDependent) { "framework-dependent" } else { "self-contained" }
    $packageSuffix = if ($isFrameworkDependent) { "-fd" } else { "" }
    $packageName = "RealismPatchGenerator-v$Version-$RuntimeIdentifier$packageSuffix"
    $packageRoot = Join-Path $releaseRoot $packageName
    $tempRoot = Join-Path $releaseRoot "_tmp_$packageName"
    $guiPublishRoot = Join-Path $tempRoot "gui"
    $zipPath = Join-Path $releaseRoot ($packageName + ".zip")
    $selfContainedValue = if ($isFrameworkDependent) { "false" } else { "true" }
    $publishSingleFileValue = if ($isFrameworkDependent) { "false" } else { "true" }
    $includeNativeLibrariesValue = if ($isFrameworkDependent) { "false" } else { "true" }

    Reset-Directory -Path $tempRoot

    dotnet publish $guiProject `
        -c $Configuration `
        -r $RuntimeIdentifier `
        --self-contained $selfContainedValue `
        -p:PublishSingleFile=$publishSingleFileValue `
        -p:IncludeNativeLibrariesForSelfExtract=$includeNativeLibrariesValue `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $guiPublishRoot

    Reset-Directory -Path $packageRoot

    Copy-Item -Path (Join-Path $guiPublishRoot "*") -Destination $packageRoot -Recurse -Force

    Copy-CommonPayload -Destination $packageRoot -TemplatesDirectory $templatesDirectory
    Write-ReleaseInfo -Destination $packageRoot `
        -EntryPoints @("RealismPatchGenerator.Gui.exe: GUI application") `
        -RecommendedUsage @("Use GUI for rule editing, generation, item exception management, and audit review") `
        -TemplatesDirectory $templatesDirectory `
        -PackageKind "GUI" `
        -IsFrameworkDependent $isFrameworkDependent `
        -DeploymentMode $deploymentMode

    New-ZipFromDirectory -DirectoryPath $packageRoot -ZipFilePath $zipPath

    Write-Host "发布目录: $packageRoot"
    Write-Host "发布压缩包: $zipPath"

    Remove-Item -Path $tempRoot -Recurse -Force
}