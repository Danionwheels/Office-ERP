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
$installedProgramFilesRoot = Join-Path $ProgramFilesRoot 'SafarSuite\ControlDesk'
$installedProgramDataRoot = Join-Path $ProgramDataRoot 'SafarSuite\ControlDesk'

function Invoke-OfficeSetupPowerShellStep {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [hashtable]$Parameters = @{},
        [Parameter(Mandatory)] [string]$FailureMessage
    )

    # A nested PowerShell script does not guarantee that LASTEXITCODE is reset.
    # Clear inherited native state before each step, then honor a real native
    # failure while still allowing the step to throw its detailed exception.
    $global:LASTEXITCODE = 0
    & $Path @Parameters
    if ($global:LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

function Wait-OfficeSetupFile {
    param(
        [Parameter(Mandatory)] [string]$Path,
        [Parameter(Mandatory)] [string]$Description,
        [int]$TimeoutSeconds = 15
    )

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (Test-Path -LiteralPath $Path -PathType Leaf) { return }
        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "$Description was not published within $TimeoutSeconds seconds: $Path"
}

$databaseParameters = @{
    PackageDirectory = $PackageDirectory
    ProgramFilesRoot = $ProgramFilesRoot
    ProgramDataRoot = $ProgramDataRoot
    ExpectedApiExecutablePath = Join-Path $ProgramFilesRoot 'SafarSuite\ControlDesk\Api\SafarSuite.ControlDesk.Api.exe'
}

if ($PSCmdlet.ShouldProcess('SafarSuite Control Desk', 'Run elevated preflight and database-ready setup')) {
    $checkpoint = 'Starting'
    try {
    $databaseResult = Invoke-OfficeSetupPowerShellStep `
        -Path $databaseInstaller `
        -Parameters $databaseParameters `
        -FailureMessage 'The database setup entry failed.'
    $checkpoint = 'DatabaseReady'

    $applicationPassfilePath = Join-Path $installedProgramDataRoot 'Secrets\Database\application.pgpass'
    Wait-OfficeSetupFile `
        -Path $applicationPassfilePath `
        -Description 'The database setup did not leave the protected application passfile required by the API'

    $settingsGenerator = Join-Path $PSScriptRoot 'New-OfficeProductionSettings.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $settingsGenerator `
        -Parameters @{
            ConfigRoot = Join-Path $installedProgramDataRoot 'Config'
            ApplicationPassfilePath = $applicationPassfilePath
        } `
        -FailureMessage 'Production settings generation failed.'

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
        $global:LASTEXITCODE = 0
        & $FirstOperatorExecutablePath
        if ($LASTEXITCODE -ne 0) { throw 'First-operator provisioning failed or was refused.' }
    }
    finally {
        [Environment]::SetEnvironmentVariable('ControlDesk__ConnectionStrings__ControlDesk', $priorConnectionString, 'Process')
    }
    $checkpoint = 'OperatorReady'

    $apiPayloadInstaller = Join-Path $PSScriptRoot 'Install-OfficeApiPayload.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $apiPayloadInstaller `
        -Parameters @{ PackageDirectory = $PackageDirectory; ProgramFilesRoot = $ProgramFilesRoot } `
        -FailureMessage 'API payload installation failed.'
    $checkpoint = 'ApiPayloadInstalled'

    $installRoot = $installedProgramFilesRoot
    $launcherDirectory = Join-Path $installRoot 'Launcher'
    New-Item -ItemType Directory -Force -Path $launcherDirectory | Out-Null
    Copy-Item -LiteralPath (Join-Path $PackageDirectory 'Start-OfficeControlDesk.ps1') `
        -Destination (Join-Path $launcherDirectory 'Start-OfficeControlDesk.ps1') -Force

    $registerService = Join-Path $PSScriptRoot 'Register-OfficeApiService.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $registerService `
        -Parameters @{ InstallRoot = $installRoot; ProgramDataRoot = $installedProgramDataRoot } `
        -FailureMessage 'API service registration failed.'
    $checkpoint = 'ApiRegistered'

    $configureActivation = Join-Path $PSScriptRoot 'Configure-OfficeServiceActivation.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $configureActivation `
        -FailureMessage 'Service activation configuration failed.'
    $checkpoint = 'ServicesConfigured'

    $installShortcuts = Join-Path $PSScriptRoot 'Install-OfficeShortcuts.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $installShortcuts `
        -Parameters @{ InstallRoot = $installRoot } `
        -FailureMessage 'Shortcut installation failed.'
    $checkpoint = 'ShortcutsInstalled'

    $activateApi = Join-Path $PSScriptRoot 'Activate-OfficeApiService.ps1'
    Invoke-OfficeSetupPowerShellStep `
        -Path $activateApi `
        -FailureMessage 'Final API readiness activation failed.'
    $checkpoint = 'Ready'

    [pscustomobject]@{
        checkpoint = $checkpoint
        database = $databaseResult.database
        apiDependency = $databaseResult.apiDependency
        productionSettings = Join-Path $installedProgramDataRoot 'Config\appsettings.Production.json'
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
