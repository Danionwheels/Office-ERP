$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'New-OfficeProductionSettings.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    postgres = $source.Contains("Provider = 'Postgres'")
    loopback = $source.Contains('127\.0\.0\.1')
    noSecretScan = $source.Contains('password|secret|token|apikey|connectionstring|privatekey')
    machineConfigSeparate = $source.Contains('appsettings.Production.json')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Production settings contract check failed: $($check.Key)" } }
Write-Host "Production settings hermetic contract: passed ($($checks.Count) checks)"
