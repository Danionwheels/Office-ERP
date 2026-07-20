[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ConfigRoot,
    [string]$ApiUrl = 'http://127.0.0.1:5188'
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Production settings generation requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Production settings generation requires administrator elevation.' }
if ($ApiUrl -notmatch '^http://127\.0\.0\.1:[0-9]+$') { throw 'Production API URL must be loopback-only HTTP.' }

New-Item -ItemType Directory -Force -Path $ConfigRoot | Out-Null
$settings = [ordered]@{
    Persistence = [ordered]@{ Provider = 'Postgres' }
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
if ($content -match '(?i)(password|secret|token|apikey|connectionstring|privatekey)') { throw 'Generated Production settings contain a secret-bearing property.' }
Write-Output "Generated non-secret Production settings at $outputPath."
