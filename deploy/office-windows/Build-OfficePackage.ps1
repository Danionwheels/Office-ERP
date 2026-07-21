param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$PostgresDistributionArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$VisualCppRedistributablePath,

    [string]$ExpectedSourceRevision,

    [switch]$RequireCleanSource
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$frontendRoot = Join-Path $repoRoot "apps\control-desk-ui"
$frontendDist = Join-Path $frontendRoot "dist"
$apiProject = Join-Path $repoRoot "src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj"
$firstOperatorProject = Join-Path $repoRoot "tools\SafarSuite.ControlDesk.FirstOperator\SafarSuite.ControlDesk.FirstOperator.csproj"
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

if ($RequireCleanSource -and ($sourceRevision -eq 'unknown' -or $sourceTreeState -ne 'clean')) {
    throw "A release package requires a known revision and a clean source tree."
}
if (-not [string]::IsNullOrWhiteSpace($ExpectedSourceRevision) -and
    $sourceRevision -cne $ExpectedSourceRevision) {
    throw "The package source revision '$sourceRevision' does not match the required revision '$ExpectedSourceRevision'."
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

$setupOutput = Join-Path $outputPath "Setup"
New-Item -ItemType Directory -Force -Path $setupOutput | Out-Null
Invoke-NativeCommand `
    -FilePath "dotnet" `
    -Arguments @(
        "publish",
        $firstOperatorProject,
        "--configuration", "Release",
        "--runtime", "win-x64",
        "--self-contained", "true",
        "--output", $setupOutput,
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

$developmentSettingsPath = Join-Path $outputPath "appsettings.Development.json"
if (Test-Path -LiteralPath $developmentSettingsPath) {
    Remove-Item -LiteralPath $developmentSettingsPath -Force
}

$databasePackage = & (Join-Path $PSScriptRoot "database\New-OfficeDatabasePackage.ps1") `
    -RepoRoot $repoRoot `
    -PackageDirectory $outputPath `
    -PostgresDistributionArchivePath $PostgresDistributionArchivePath `
    -VisualCppRedistributablePath $VisualCppRedistributablePath `
    -SourceRevision $sourceRevision

$setupScripts = @(
    'Install-OfficeControlDesk.ps1',
    'New-OfficeProductionSettings.ps1',
    'Install-OfficeApiPayload.ps1',
    'Register-OfficeApiService.ps1',
    'Configure-OfficeServiceActivation.ps1',
    'Activate-OfficeApiService.ps1',
    'Repair-OfficeApiService.ps1',
    'Start-OfficeControlDesk.ps1',
    'Install-OfficeShortcuts.ps1',
    'Uninstall-OfficeControlDesk.ps1'
)
foreach ($setupScript in $setupScripts) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $setupScript) -Destination (Join-Path $outputPath $setupScript)
}

if ($RequireCleanSource) {
    $finalSourceRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "The source revision could not be revalidated after the build."
    }
    $finalSourceStatus = @(& git -C $repoRoot status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw "The source tree could not be revalidated after the build."
    }
    if ($finalSourceRevision -cne $sourceRevision -or $finalSourceStatus.Count -ne 0) {
        throw "A release build changed or introduced source-tree content; package sealing was refused."
    }
}

$databaseManifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $databasePackage.ManifestPath).Hash.ToUpperInvariant()

$packageBytes = (Get-ChildItem -LiteralPath $outputPath -Recurse -File |
    Measure-Object -Property Length -Sum).Sum

$manifest = [ordered]@{
    product = "SafarSuite Control Desk"
    packageFormat = "office-windows-native-postgresql-v2"
    runtimeIdentifier = "win-x64"
    framework = "net10.0"
    selfContained = $true
    uiHosting = "same-origin-loopback"
    apiUrl = "http://127.0.0.1:5188"
    sourceRevision = $sourceRevision
    sourceTreeState = $sourceTreeState
    builtAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    payloadBytesExcludingTopManifest = [long]$packageBytes
    services = [ordered]@{
        api = [ordered]@{
            name = "SafarSuiteControlDeskApi"
            executable = "SafarSuite.ControlDesk.Api.exe"
            dependsOn = @("SafarSuiteControlDeskPostgreSQL")
            creationWorkPackage = "OFFICE-P0-05"
        }
        database = [ordered]@{
            name = "SafarSuiteControlDeskPostgreSQL"
            version = "17.10"
            manifestSha256 = $databaseManifestHash
            migrationCount = [int]$databasePackage.MigrationCount
            migrationTarget = [string]$databasePackage.MigrationTarget
        }
        setup = [ordered]@{
            firstOperatorExecutable = "Setup\SafarSuite.ControlDesk.FirstOperator.exe"
            creationWorkPackage = "SVC05-10B"
        }
    }
}

$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputPath "office-package-manifest.json") -Encoding utf8

Write-Host "Office Windows pilot package created: $outputPath"
Write-Host "Package bytes: $packageBytes"
