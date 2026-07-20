$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Register-OfficeApiService.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    demandStart = $source.Contains('start= demand')
    virtualAccount = $source.Contains('NT SERVICE\SafarSuiteControlDeskApi')
    databaseDependency = $source.Contains('depend= $DatabaseServiceName')
    foreignServiceRefusal = $source.Contains('foreign service') -or $source.Contains('foreign.')
    recoveryActions = $source.Contains('sc.exe failure')
    receipt = $source.Contains('api-service-receipt.json')
    payloadReceipt = $source.Contains('api-installation-receipt.json')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "API service contract check failed: $($check.Key)" } }
Write-Host "API service registration hermetic contract: passed ($($checks.Count) checks)"
