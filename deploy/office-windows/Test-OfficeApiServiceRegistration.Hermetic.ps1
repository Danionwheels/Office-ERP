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
    aclConvergence = $source.Contains('icacls.exe') -and $source.Contains('Set-OfficeApiAcl')
    machineSecretAcl = $source.Contains('control-desk-machine-secrets.v1.json') -and $source.Contains('Set-OfficeMachineSecretAcl') -and $source.Contains('2177609957-237951300-3651597395-3114367455-1078186923') -and $source.Contains('$secretsRoot')
    applicationPassfileAcl = $source.Contains('application.pgpass') -and $source.Contains('Set-OfficeApplicationPassfileAcl') -and $source.Contains('$databaseSecretsDirectory')
    normalizesServicePath = $source.Contains('pathMatches') -and $source.Contains('StringComparison]::OrdinalIgnoreCase')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "API service contract check failed: $($check.Key)" } }
Write-Host "API service registration hermetic contract: passed ($($checks.Count) checks)"
