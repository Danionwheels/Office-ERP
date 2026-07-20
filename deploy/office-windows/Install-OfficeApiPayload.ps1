[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$PackageDirectory,
    [Parameter(Mandatory)] [string]$ProgramFilesRoot,
    [string]$ProductVersion = '0.1.0'
)

$ErrorActionPreference = 'Stop'

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'The office payload installer requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'The office payload installer requires administrator elevation.'
}

$packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$manifestPath = Join-Path $packagePath 'office-package-manifest.json'
$apiExecutable = Join-Path $packagePath 'SafarSuite.ControlDesk.Api.exe'
$uiIndex = Join-Path $packagePath 'wwwroot\index.html'
foreach ($requiredPath in @($manifestPath, $apiExecutable, $uiIndex)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "The office package is missing the required payload file: $requiredPath"
    }
}

function Assert-NoReparse([string]$Root) {
    foreach ($item in Get-ChildItem -LiteralPath $Root -Force -Recurse) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "The office payload contains a reparse point: $($item.FullName)"
        }
    }
}

Assert-NoReparse $packagePath
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
if ([string]$manifest.product -ne 'SafarSuite Control Desk' -or
    [string]$manifest.packageFormat -ne 'office-windows-native-postgresql-v2') {
    throw 'The office package manifest is not a recognized Control Desk package.'
}

$installRoot = Join-Path $ProgramFilesRoot 'SafarSuite\ControlDesk'
$finalPath = Join-Path $installRoot 'Api'
$receiptPath = Join-Path $installRoot 'api-installation-receipt.json'
$stagePath = Join-Path $installRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))
$backupPath = Join-Path $installRoot ('.previous-' + [Guid]::NewGuid().ToString('N'))

New-Item -ItemType Directory -Force -Path $installRoot, $stagePath | Out-Null
try {
    Copy-Item -LiteralPath (Join-Path $packagePath '*') -Destination $stagePath -Recurse -Force
    Assert-NoReparse $stagePath
    $payloadHash = (Get-ChildItem -LiteralPath $stagePath -Recurse -File | Sort-Object FullName | ForEach-Object {
        (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash
    }) -join '' | ForEach-Object { [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($_)) } |
        ForEach-Object { [Convert]::ToHexString($_) }
    $receipt = [ordered]@{
        product = 'SafarSuite Control Desk'
        serviceName = 'SafarSuiteControlDeskApi'
        installPath = $finalPath
        packageManifestSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $manifestPath).Hash
        payloadSha256 = $payloadHash
        productVersion = $ProductVersion
        installedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    }
    $receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stagePath 'api-installation-receipt.json') -Encoding utf8
    if (Test-Path -LiteralPath $finalPath) {
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw 'The existing API path has no ownership receipt.' }
        $existing = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
        if ([string]$existing.serviceName -ne 'SafarSuiteControlDeskApi' -or [string]$existing.installPath -ne $finalPath) {
            throw 'The existing API path is not owned by Control Desk.'
        }
        Move-Item -LiteralPath $finalPath -Destination $backupPath
    }
    Move-Item -LiteralPath $stagePath -Destination $finalPath
    Move-Item -LiteralPath (Join-Path $finalPath 'api-installation-receipt.json') -Destination $receiptPath -Force
    if (Test-Path -LiteralPath $backupPath) { Remove-Item -LiteralPath $backupPath -Recurse -Force }
    Write-Output "Installed Control Desk API payload at $finalPath."
}
catch {
    if (Test-Path -LiteralPath $stagePath) { Remove-Item -LiteralPath $stagePath -Recurse -Force }
    throw
}
