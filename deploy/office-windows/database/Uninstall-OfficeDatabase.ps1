#Requires -Version 5.1
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [string]$PackageDirectory = (Split-Path -Parent $PSScriptRoot),
    [string]$ProgramFilesRoot,
    [string]$ProgramDataRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Import-Module (Join-Path $PSScriptRoot 'OfficeDatabaseLifecycle.psm1') -Force

$lifecycleParameters = @{ PackageDirectory = $PackageDirectory }
if (-not [string]::IsNullOrWhiteSpace($ProgramFilesRoot)) { $lifecycleParameters.ProgramFilesRoot = $ProgramFilesRoot }
if (-not [string]::IsNullOrWhiteSpace($ProgramDataRoot)) { $lifecycleParameters.ProgramDataRoot = $ProgramDataRoot }

$result = Uninstall-OfficeDatabaseLifecycle @lifecycleParameters

[pscustomobject]@{
    database = $result
    dataPreserved = $true
    secretsPreserved = $true
    reinstallSupported = $true
}
