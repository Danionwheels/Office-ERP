#Requires -Version 5.1
#Requires -RunAsAdministrator
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$PackageDirectory,
    [Parameter(Mandatory)] [string]$ProgramFilesRoot,
    [Parameter(Mandatory)] [string]$ProgramDataRoot
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
    [pscustomobject]@{
        checkpoint = 'DatabaseReady'
        database = $databaseResult.database
        apiDependency = $databaseResult.apiDependency
        nextStep = 'Generate machine secrets, provision the first operator, install the API payload, and activate readiness-gated services.'
    }
}
