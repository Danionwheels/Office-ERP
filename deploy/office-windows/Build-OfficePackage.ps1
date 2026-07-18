param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$frontendRoot = Join-Path $repoRoot "apps\control-desk-ui"
$frontendDist = Join-Path $frontendRoot "dist"
$apiProject = Join-Path $repoRoot "src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj"
$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    [System.IO.Path]::GetFullPath($OutputDirectory)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$FilePath exited with code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

if (Test-Path -LiteralPath $outputPath) {
    $existingItems = @(Get-ChildItem -LiteralPath $outputPath -Force)
    if ($existingItems.Count -gt 0) {
        throw "OutputDirectory must be absent or empty: $outputPath"
    }
}
else {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

$sourceRevision = "unknown"
$sourceTreeState = "unknown"
try {
    $sourceRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        $sourceRevision = "unknown"
    }

    $sourceStatus = @(& git -C $repoRoot status --porcelain)
    if ($LASTEXITCODE -eq 0) {
        $sourceTreeState = if ($sourceStatus.Count -eq 0) { "clean" } else { "dirty" }
    }
}
catch {
    $sourceRevision = "unknown"
    $sourceTreeState = "unknown"
}

Invoke-NativeCommand `
    -FilePath "npm" `
    -Arguments @("ci") `
    -WorkingDirectory $frontendRoot

Invoke-NativeCommand `
    -FilePath "npm" `
    -Arguments @("run", "build") `
    -WorkingDirectory $frontendRoot

Invoke-NativeCommand `
    -FilePath "dotnet" `
    -Arguments @(
        "publish",
        $apiProject,
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "--output", $outputPath,
        "-p:PublishSingleFile=false",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-p:InformationalVersion=1.0.0+$sourceRevision",
        "-p:IncludeSourceRevisionInInformationalVersion=false"
    ) `
    -WorkingDirectory $repoRoot

$webRoot = Join-Path $outputPath "wwwroot"
New-Item -ItemType Directory -Force -Path $webRoot | Out-Null
Get-ChildItem -LiteralPath $frontendDist -Force | Copy-Item -Destination $webRoot -Recurse -Force

$packageBytes = (Get-ChildItem -LiteralPath $outputPath -Recurse -File |
    Measure-Object -Property Length -Sum).Sum

$manifest = [ordered]@{
    product = "SafarSuite Control Desk"
    packageFormat = "office-windows-pilot-v1"
    runtimeIdentifier = "win-x64"
    framework = "net10.0"
    selfContained = $true
    uiHosting = "same-origin-loopback"
    apiUrl = "http://127.0.0.1:5188"
    sourceRevision = $sourceRevision
    sourceTreeState = $sourceTreeState
    builtAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    payloadBytes = [long]$packageBytes
}

$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath "office-package-manifest.json") -Encoding utf8

Write-Host "Office Windows pilot package created: $outputPath"
Write-Host "Package bytes: $packageBytes"
