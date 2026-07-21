$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'New-OfficeProductionSettings.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    postgres = $source.Contains("Provider = 'Postgres'")
    loopback = $source.Contains('127\.0\.0\.1')
    noSecretScan = $source.Contains('password|secret|token|apikey|privatekey') -and $source.Contains('Passfile=') -and $source.Contains('^\s*"')
    passfileRequired = $source.Contains('ApplicationPassfilePath') -and $source.Contains('application passfile is required')
    applicationRole = $source.Contains('Username=safarsuite_control_desk_app')
    kestrelEndpoint = $source.Contains('OfficeLocal') -and -not $source.Contains('Endpoints = [ordered]@{ Http')
    machineConfigSeparate = $source.Contains('appsettings.Production.json')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Production settings contract check failed: $($check.Key)" } }
Write-Host "Production settings hermetic contract: passed ($($checks.Count) checks)"
