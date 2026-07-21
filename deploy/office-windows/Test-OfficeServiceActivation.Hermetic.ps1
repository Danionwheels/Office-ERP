$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Configure-OfficeServiceActivation.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    delayedStart = $source.Contains('start= delayed-auto')
    databaseRecovery = $source.Contains('failure $DatabaseServiceName')
    apiDependency = $source.Contains('config $ApiServiceName depend= $DatabaseServiceName')
    boundedActions = $source.Contains('restart/5000/restart/30000/none/0')
    missingServiceRefusal = $source.Contains('is not installed')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Service activation contract check failed: $($check.Key)" } }
Write-Host "Service activation hermetic contract: passed ($($checks.Count) checks)"
