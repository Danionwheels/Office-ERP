#Requires -Version 5.1
#Requires -RunAsAdministrator
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$PackageDirectory,
    [Parameter(Mandatory)] [string]$ProgramFilesRoot,
    [Parameter(Mandatory)] [string]$ProgramDataRoot,
    [string]$FirstOperatorExecutablePath
)

$ErrorActionPreference = 'Stop'
$databaseInstaller = Join-Path $PSScriptRoot 'database\Install-OfficeDatabase.ps1'
$databaseParameters = @{
    PackageDirectory = $PackageDirectory
    ProgramFilesRoot = $ProgramFilesRoot
    ProgramDataRoot = $ProgramDataRoot
    ExpectedApiExecutablePath = Join-Path $ProgramFilesRoot 'SafarSuite\ControlDesk\Api\SafarSuite.ControlDesk.Api.exe'
}

if ($PSCmdlet.ShouldProcess('SafarSuite Control Desk', 'Run elevated preflight and database-ready setup')) {
    $checkpoint = 'Starting'
    try {
    $databaseResult = & $databaseInstaller @databaseParameters
    if ($LASTEXITCODE -ne 0) { throw 'The database setup entry failed.' }
    $checkpoint = 'DatabaseReady'

    $applicationPassfilePath = Join-Path $ProgramDataRoot 'Secrets\Database\application.pgpass'
    if (-not (Test-Path -LiteralPath $applicationPassfilePath -PathType Leaf)) {
        throw 'The database setup did not leave the protected application passfile required by the API.'
    }

    $settingsGenerator = Join-Path $PSScriptRoot 'New-OfficeProductionSettings.ps1'
    & $settingsGenerator `
        -ConfigRoot (Join-Path $ProgramDataRoot 'Config') `
        -ApplicationPassfilePath $applicationPassfilePath
    if ($LASTEXITCODE -ne 0) { throw 'Production settings generation failed.' }

    if ([string]::IsNullOrWhiteSpace($FirstOperatorExecutablePath)) {
        $FirstOperatorExecutablePath = Join-Path $PackageDirectory 'Setup\SafarSuite.ControlDesk.FirstOperator.exe'
    }
    if (-not (Test-Path -LiteralPath $FirstOperatorExecutablePath -PathType Leaf)) {
        throw "The packaged first-operator bootstrap executable is missing: $FirstOperatorExecutablePath"
    }

    $connectionString = "Host=127.0.0.1;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Passfile=$([IO.Path]::GetFullPath($applicationPassfilePath))"
    $priorConnectionString = [Environment]::GetEnvironmentVariable('ControlDesk__ConnectionStrings__ControlDesk', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('ControlDesk__ConnectionStrings__ControlDesk', $connectionString, 'Process')
        & $FirstOperatorExecutablePath
        if ($LASTEXITCODE -ne 0) { throw 'First-operator provisioning failed or was refused.' }
    }
    finally {
        [Environment]::SetEnvironmentVariable('ControlDesk__ConnectionStrings__ControlDesk', $priorConnectionString, 'Process')
    }
    $checkpoint = 'OperatorReady'

    $apiPayloadInstaller = Join-Path $PSScriptRoot 'Install-OfficeApiPayload.ps1'
    & $apiPayloadInstaller -PackageDirectory $PackageDirectory -ProgramFilesRoot $ProgramFilesRoot
    if ($LASTEXITCODE -ne 0) { throw 'API payload installation failed.' }
    $checkpoint = 'ApiPayloadInstalled'

    $installRoot = Join-Path $ProgramFilesRoot 'SafarSuite\ControlDesk'
    $launcherDirectory = Join-Path $installRoot 'Launcher'
    New-Item -ItemType Directory -Force -Path $launcherDirectory | Out-Null
    Copy-Item -LiteralPath (Join-Path $PackageDirectory 'Start-OfficeControlDesk.ps1') `
        -Destination (Join-Path $launcherDirectory 'Start-OfficeControlDesk.ps1') -Force

    $registerService = Join-Path $PSScriptRoot 'Register-OfficeApiService.ps1'
    & $registerService -InstallRoot $installRoot -ProgramDataRoot $ProgramDataRoot
    if ($LASTEXITCODE -ne 0) { throw 'API service registration failed.' }
    $checkpoint = 'ApiRegistered'

    $configureActivation = Join-Path $PSScriptRoot 'Configure-OfficeServiceActivation.ps1'
    & $configureActivation
    if ($LASTEXITCODE -ne 0) { throw 'Service activation configuration failed.' }
    $checkpoint = 'ServicesConfigured'

    $installShortcuts = Join-Path $PSScriptRoot 'Install-OfficeShortcuts.ps1'
    & $installShortcuts -InstallRoot $installRoot
    if ($LASTEXITCODE -ne 0) { throw 'Shortcut installation failed.' }
    $checkpoint = 'ShortcutsInstalled'

    $activateApi = Join-Path $PSScriptRoot 'Activate-OfficeApiService.ps1'
    & $activateApi
    if ($LASTEXITCODE -ne 0) { throw 'Final API readiness activation failed.' }
    $checkpoint = 'Ready'

    [pscustomobject]@{
        checkpoint = $checkpoint
        database = $databaseResult.database
        apiDependency = $databaseResult.apiDependency
        productionSettings = Join-Path $ProgramDataRoot 'Config\appsettings.Production.json'
        installRoot = $installRoot
        nextStep = 'Launch SafarSuite Control Desk from the owned shortcut.'
    }
    }
    catch {
        if ($checkpoint -in @('ApiRegistered', 'ServicesConfigured', 'ShortcutsInstalled')) {
            Stop-Service -Name 'SafarSuiteControlDeskApi' -Force -ErrorAction SilentlyContinue
        }
        throw "Office setup failed at checkpoint '$checkpoint'. Owned payload/configuration remain for repair; database, operators, and secrets were not removed. $($_.Exception.Message)"
    }
}
