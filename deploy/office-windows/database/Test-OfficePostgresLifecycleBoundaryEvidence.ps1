#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [Parameter(Mandatory = $true)][ValidateSet('success', 'failure', 'cancelled', 'skipped')][string]$LifecycleOutcome,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedSourceRevision,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ExpectedOfficePackageArchiveSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{32}$')][string]$ExpectedInvocationNonce
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

try {
    Import-Module (Join-Path $PSScriptRoot 'OfficePostgresLifecycleBoundaryDiagnostic.psm1') -Force
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence `
        -EvidencePath $EvidencePath `
        -LifecycleOutcome $LifecycleOutcome `
        -ExpectedSourceRevision $ExpectedSourceRevision `
        -ExpectedOfficePackageArchiveSha256 $ExpectedOfficePackageArchiveSha256 `
        -ExpectedInvocationNonce $ExpectedInvocationNonce
    if (-not $validation.IsValid) {
        Write-Error 'Office database boundary evidence validation failed.' -ErrorAction Continue
        exit 1
    }
    Write-Host 'Office database boundary evidence validation passed.'
}
catch {
    Write-Error 'Office database boundary evidence validation failed.' -ErrorAction Continue
    exit 1
}
