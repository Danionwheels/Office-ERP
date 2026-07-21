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
    $databaseResult = & $databaseInstaller @databaseParameters
    if ($LASTEXITCODE -ne 0) { throw 'The database setup entry failed.' }

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

    [pscustomobject]@{
        checkpoint = 'OperatorReady'
        database = $databaseResult.database
        apiDependency = $databaseResult.apiDependency
        productionSettings = Join-Path $ProgramDataRoot 'Config\appsettings.Production.json'
        nextStep = 'Install the API payload, register the service, and activate readiness-gated services.'
    }
}
