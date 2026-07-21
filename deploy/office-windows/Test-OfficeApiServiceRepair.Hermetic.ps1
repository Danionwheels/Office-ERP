$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Repair-OfficeApiService.ps1')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    receiptRequired = $source.Contains('Missing API installation receipt')
    pathOwnership = $source.Contains('payloadReceipt.installPath')
    foreignRefusal = $source.Contains('Foreign API service configuration detected')
    explicitApply = $source.Contains('[switch]$Apply')
    noMutationDefault = $source.Contains('No mutation requested')
    delegatesRegistration = $source.Contains('Register-OfficeApiService.ps1')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "API repair contract check failed: $($check.Key)" } }
Write-Host "API service repair hermetic contract: passed ($($checks.Count) checks)"
