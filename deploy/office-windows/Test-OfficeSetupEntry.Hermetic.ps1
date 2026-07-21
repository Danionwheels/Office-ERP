$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeControlDesk.ps1')
$checks = [ordered]@{
    administrator = $source.Contains('#Requires -RunAsAdministrator')
    databaseEntry = $source.Contains('Install-OfficeDatabase.ps1')
    explicitRoots = $source.Contains('ProgramFilesRoot') -and $source.Contains('ProgramDataRoot')
    apiDependency = $source.Contains('ExpectedApiExecutablePath')
    applicationPassfile = $source.Contains('application.pgpass')
    productionSettings = $source.Contains('New-OfficeProductionSettings.ps1') -and $source.Contains('appsettings.Production.json')
    firstOperator = $source.Contains('SafarSuite.ControlDesk.FirstOperator.exe') -and $source.Contains('ControlDesk__ConnectionStrings__ControlDesk')
    operatorCheckpoint = $source.Contains("checkpoint = 'OperatorReady'")
    noDestructiveRerun = $source.Contains('First-operator provisioning failed or was refused')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Setup-entry contract check failed: $($check.Key)" } }
Write-Host "One-entry setup hermetic contract: passed ($($checks.Count) checks)"
