[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ConfigRoot,
    [Parameter(Mandatory)] [string]$ApplicationPassfilePath,
    [string]$ApiUrl = 'http://127.0.0.1:5188'
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Production settings generation requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Production settings generation requires administrator elevation.' }
if ($ApiUrl -notmatch '^http://127\.0\.0\.1:[0-9]+$') { throw 'Production API URL must be loopback-only HTTP.' }
if (-not [IO.Path]::IsPathRooted($ApplicationPassfilePath)) { throw 'The PostgreSQL application passfile path must be absolute.' }
if (-not (Test-Path -LiteralPath $ApplicationPassfilePath -PathType Leaf)) { throw 'The PostgreSQL application passfile is required before production settings generation.' }

New-Item -ItemType Directory -Force -Path $ConfigRoot | Out-Null
$settings = [ordered]@{
    Persistence = [ordered]@{ Provider = 'Postgres' }
    ConnectionStrings = [ordered]@{
        ControlDesk = "Host=127.0.0.1;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Passfile=$([IO.Path]::GetFullPath($ApplicationPassfilePath))"
    }
    ControlDesk = [ordered]@{
        Logging = [ordered]@{ File = [ordered]@{ Enabled = $true; DirectoryPath = (Join-Path (Split-Path -Parent $ConfigRoot) 'Logs') } }
        OperatorAccess = [ordered]@{ SessionMinutes = 480 }
    }
    ControlCloud = [ordered]@{ OutboxWorker = [ordered]@{ Enabled = $false; BatchSize = 20; PollIntervalSeconds = 15 } }
    Kestrel = [ordered]@{ Endpoints = [ordered]@{ Http = [ordered]@{ Url = $ApiUrl } } }
}
$outputPath = Join-Path $ConfigRoot 'appsettings.Production.json'
$settings | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $outputPath -Encoding utf8
$content = Get-Content -Raw -LiteralPath $outputPath
# Inspect JSON property names, not values: the canonical protected passfile path
# contains the directory name "Secrets" and must remain a valid non-secret value.
if ($content -match '(?im)^\s*"[^"]*(password|secret|token|apikey|privatekey)[^"]*"\s*:') {
    throw 'Generated Production settings contain a secret-bearing property.'
}
if ($content -notmatch '(?i)Passfile=') { throw 'Generated Production settings must reference the protected PostgreSQL application passfile.' }
Write-Output "Generated non-secret Production settings at $outputPath."
