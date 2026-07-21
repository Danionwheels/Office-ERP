#Requires -Version 5.1
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [string]$PackageDirectory = (Split-Path -Parent $PSScriptRoot),
    [string]$ProgramFilesRoot,
    [string]$ProgramDataRoot,
    [string]$ExpectedApiExecutablePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'OfficeDatabaseLifecycle.psm1') -Force

$lifecycleParameters = @{ PackageDirectory = $PackageDirectory }
if (-not [string]::IsNullOrWhiteSpace($ProgramFilesRoot)) { $lifecycleParameters.ProgramFilesRoot = $ProgramFilesRoot }
if (-not [string]::IsNullOrWhiteSpace($ProgramDataRoot)) { $lifecycleParameters.ProgramDataRoot = $ProgramDataRoot }

$result = Install-OfficeDatabaseLifecycle @lifecycleParameters
$dependencyParameters = @{}
if (-not [string]::IsNullOrWhiteSpace($ExpectedApiExecutablePath)) {
    $dependencyParameters.ExpectedApiExecutablePath = $ExpectedApiExecutablePath
}
$dependency = Set-OfficeApiDatabaseDependency @dependencyParameters

[pscustomobject]@{
    database = $result
    apiDependency = $dependency
    physicalRebootProof = 'Pending'
}
