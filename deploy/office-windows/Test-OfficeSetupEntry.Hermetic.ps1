$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeControlDesk.ps1')
$checks = [ordered]@{
    administrator = $source.Contains('#Requires -RunAsAdministrator')
    databaseEntry = $source.Contains('Install-OfficeDatabase.ps1')
    explicitRoots = $source.Contains('ProgramFilesRoot') -and $source.Contains('ProgramDataRoot')
    apiDependency = $source.Contains('ExpectedApiExecutablePath')
    checkpoint = $source.Contains("checkpoint = 'DatabaseReady'")
    stopsBeforeSecrets = $source.Contains('Generate machine secrets')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Setup-entry contract check failed: $($check.Key)" } }
Write-Host "One-entry setup hermetic contract: passed ($($checks.Count) checks)"
