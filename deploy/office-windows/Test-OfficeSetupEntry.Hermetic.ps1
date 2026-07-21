$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeControlDesk.ps1')
$checks = [ordered]@{
    administrator = $source.Contains('#Requires -RunAsAdministrator')
    nestedPowerShellExitState = $source.Contains('Invoke-OfficeSetupPowerShellStep') -and $source.Contains('$global:LASTEXITCODE = 0')
    databaseEntry = $source.Contains('Install-OfficeDatabase.ps1')
    explicitRoots = $source.Contains('ProgramFilesRoot') -and $source.Contains('ProgramDataRoot')
    apiDependency = $source.Contains('ExpectedApiExecutablePath')
    applicationPassfile = $source.Contains('application.pgpass')
    productionSettings = $source.Contains('New-OfficeProductionSettings.ps1') -and $source.Contains('appsettings.Production.json')
    firstOperator = $source.Contains('SafarSuite.ControlDesk.FirstOperator.exe') -and $source.Contains('ControlDesk__ConnectionStrings__ControlDesk')
    operatorCheckpoint = $source.Contains("checkpoint = 'OperatorReady'")
    noDestructiveRerun = $source.Contains('First-operator provisioning failed or was refused')
    apiPayload = $source.Contains('Install-OfficeApiPayload.ps1') -and $source.Contains('ApiPayloadInstalled')
    serviceRegistration = $source.Contains('Register-OfficeApiService.ps1') -and $source.Contains('ApiRegistered')
    serviceActivation = $source.Contains('Configure-OfficeServiceActivation.ps1') -and $source.Contains('Activate-OfficeApiService.ps1')
    launcherAndShortcuts = $source.Contains('Start-OfficeControlDesk.ps1') -and $source.Contains('Install-OfficeShortcuts.ps1')
    finalReadiness = $source.Contains("checkpoint = 'Ready'")
    rollbackBoundary = $source.Contains('Owned payload/configuration remain for repair')
}
foreach ($check in $checks.GetEnumerator()) { if (-not $check.Value) { throw "Setup-entry contract check failed: $($check.Key)" } }
Write-Host "One-entry setup hermetic contract: passed ($($checks.Count) checks)"
