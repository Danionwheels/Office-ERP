$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\..\tools\SafarSuite.ControlDesk.FirstOperator\Program.cs')
$checks = [ordered]@{
    elevation = $source.Contains('WindowsBuiltInRole.Administrator')
    environmentConnection = $source.Contains('ControlDesk__ConnectionStrings__ControlDesk')
    noEcho = $source.Contains('ReadSecret') -and $source.Contains('Credentials were not echoed or logged')
    singleUse = $source.Contains('an operator already exists')
    machineSecret = $source.Contains('ControlDeskMachineSecretEnvelopeStore') -and $source.Contains('CreateOrLoad')
    canonicalPath = $source.Contains('GetCanonicalEnvelopePath')
}
foreach ($check in $checks.GetEnumerator()) {
    if (-not $check.Value) { throw "First-operator bootstrap contract check failed: $($check.Key)" }
}
Write-Host "First-operator bootstrap hermetic contract: passed ($($checks.Count) checks)"
