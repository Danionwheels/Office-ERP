[CmdletBinding()]
param(
    [string]$DatabaseServiceName = 'SafarSuiteControlDeskPostgreSQL',
    [string]$ApiServiceName = 'SafarSuiteControlDeskApi',
    [string]$ReadyUrl = 'http://127.0.0.1:5188/ready',
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'API activation requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'API activation requires administrator elevation.' }
if ($ReadyUrl -notmatch '^http://127\.0\.0\.1:[0-9]+/ready$') { throw 'Readiness URL must be loopback-only.' }

$database = Get-Service -Name $DatabaseServiceName -ErrorAction SilentlyContinue
$api = Get-Service -Name $ApiServiceName -ErrorAction SilentlyContinue
if ($null -eq $database -or $null -eq $api) { throw 'Database and API services must be registered before activation.' }
if ($database.Status -ne 'Running') { throw 'API activation refused because PostgreSQL is not running.' }

try {
    if ($api.Status -ne 'Running') { Start-Service -Name $ApiServiceName }
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    $lastServiceState = $api.Status
    $lastReadinessFailure = $null
    do {
        $api = Get-Service -Name $ApiServiceName -ErrorAction Stop
        $lastServiceState = $api.Status
        if ($api.Status -eq 'Stopped') {
            $serviceDetails = Get-CimInstance Win32_Service -Filter "Name='$ApiServiceName'" -ErrorAction SilentlyContinue
            $exitCode = if ($null -eq $serviceDetails) { 'unknown' } else { [string]$serviceDetails.ExitCode }
            throw "The Control Desk API stopped during activation (service exit code $exitCode)."
        }
        try {
            $readiness = Invoke-RestMethod -Uri $ReadyUrl -TimeoutSec 2
            if ($readiness.status -eq 'Ready' -and $readiness.database.status -eq 'Ready') {
                Write-Output 'Control Desk API activated after database and migration readiness.'
                return
            }
            $lastReadinessFailure = "The API returned a non-ready response ($($readiness.status))."
        } catch {
            $lastReadinessFailure = $_.Exception.Message
        }
        Start-Sleep -Milliseconds 500
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "The Control Desk API did not reach database-ready state within the activation timeout. Last service state: $lastServiceState. Last readiness result: $lastReadinessFailure"
}
catch {
    Stop-Service -Name $ApiServiceName -Force -ErrorAction SilentlyContinue
    throw
}
