$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Install-OfficeApiPayload.ps1'
$source = Get-Content -Raw -LiteralPath $scriptPath

$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole]::Administrator')
    rejectsReparse = $source.Contains('ReparsePoint')
    stagesOutsideFinal = $source.Contains('.staging-') -and $source.Contains('Move-Item -LiteralPath $stagePath')
    verifiesManifest = $source.Contains('office-package-manifest.json') -and $source.Contains('packageFormat')
    hashesPayload = $source.Contains('Get-FileHash') -and $source.Contains('payloadSha256')
    requiresOwnershipReceipt = $source.Contains('api-installation-receipt.json') -and $source.Contains('has no ownership receipt')
    cleansFailedStage = $source.Contains('Remove-Item -LiteralPath $stagePath')
    loopbackContract = $source.Contains('SafarSuiteControlDeskApi')
}

foreach ($check in $checks.GetEnumerator()) {
    if (-not $check.Value) { throw "API payload installer contract check failed: $($check.Key)" }
}

Write-Host 'API payload installer hermetic contract: passed'
Write-Host "Checks: $($checks.Count)"
