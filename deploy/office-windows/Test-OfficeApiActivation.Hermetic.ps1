$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Activate-OfficeApiService.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    databaseGate = $source.Contains('PostgreSQL is not running')
    readinessGate = $source.Contains("status -eq 'Ready'") -and $source.Contains("database.status -eq 'Ready'")
    loopback = $source.Contains('127\.0\.0\.1')
    timeout = $source.Contains('TimeoutSeconds')
    stopOnFailure = $source.Contains('Stop-Service -Name $ApiServiceName')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "API activation contract check failed: $($check.Key)" } }
Write-Host "API activation hermetic contract: passed ($($checks.Count) checks)"
