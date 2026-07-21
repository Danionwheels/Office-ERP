$ErrorActionPreference = 'Stop'
$runbook = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\docs\architecture\office-native-setup-acceptance-v1.md')
$setup = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeControlDesk.ps1')
$checks = [ordered]@{
    onePcBoundary = $runbook.Contains('one-PC topology') -and $runbook.Contains('No Linux, Docker, DNS, HTTPS, SMTP')
    sealedPackage = $runbook.Contains('package SHA-256') -and $runbook.Contains('office-windows-native-postgresql-v2')
    checkpoints = $runbook.Contains('DatabaseReady') -and $runbook.Contains('OperatorReady') -and $runbook.Contains('Ready')
    serviceProof = $runbook.Contains('service SIDs') -and $runbook.Contains('dependency')
    readinessProof = $runbook.Contains('127.0.0.1:5188/ready') -and $runbook.Contains('no listener exists outside loopback')
    interruptionProof = $runbook.Contains('Interruption and recovery matrix') -and $runbook.Contains('preserve the PostgreSQL cluster')
    reinstallProof = $runbook.Contains('Uninstall and reinstall') -and $runbook.Contains('same cluster system identifier')
    cleanupGuard = $runbook.Contains('GUID-owned disposable root') -and $runbook.Contains('foreign service')
    setupHasRollback = $setup.Contains('Owned payload/configuration remain for repair')
}
foreach ($check in $checks.GetEnumerator()) {
    if (-not $check.Value) { throw "Native setup acceptance contract check failed: $($check.Key)" }
}
Write-Host "Native setup acceptance hermetic contract: passed ($($checks.Count) checks)"
