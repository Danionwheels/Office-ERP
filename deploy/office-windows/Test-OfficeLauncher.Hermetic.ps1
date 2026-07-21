$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Start-OfficeControlDesk.ps1')
$checks = [ordered]@{
    loopback = $source.Contains('127\.0\.0\.1')
    readiness = $source.Contains('/ready')
    databaseReady = $source.Contains("database.status -eq 'Ready'")
    boundedWait = $source.Contains('TimeoutSeconds')
    opensAfterReady = $source.Contains('Start-Process $BaseUrl')
    guidance = $source.Contains('run the repair command')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Launcher contract check failed: $($check.Key)" } }
Write-Host "Control Desk launcher hermetic contract: passed ($($checks.Count) checks)"
