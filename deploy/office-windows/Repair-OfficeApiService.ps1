[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$InstallRoot,
    [Parameter(Mandatory)] [string]$ProgramDataRoot,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'API service repair requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'API service repair requires administrator elevation.' }

$serviceName = 'SafarSuiteControlDeskApi'
$apiPath = [IO.Path]::GetFullPath((Join-Path $InstallRoot 'Api\SafarSuite.ControlDesk.Api.exe'))
$receiptPath = Join-Path $InstallRoot 'api-installation-receipt.json'
$serviceReceiptPath = Join-Path $ProgramDataRoot 'Service\api-service-receipt.json'
$service = Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction SilentlyContinue

if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw 'Missing API installation receipt; repair refused.' }
$payloadReceipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
if ([IO.Path]::GetFullPath([string]$payloadReceipt.installPath) -ne [IO.Path]::GetFullPath((Join-Path $InstallRoot 'Api'))) { throw 'API installation receipt path is foreign; repair refused.' }

if ($null -ne $service) {
    if ($service.PathName -notlike ('"' + $apiPath + '"*') -or $service.StartName -ne 'NT SERVICE\SafarSuiteControlDeskApi') { throw 'Foreign API service configuration detected; repair refused.' }
    if (-not $Apply) { Write-Output "Owned API service state: $($service.State). No mutation requested."; return }
}

if (-not $Apply) { Write-Output 'API service is missing but owned payload is present. No mutation requested.'; return }
if ($PSCmdlet.ShouldProcess($serviceName, 'Repair owned API service registration')) {
    & (Join-Path $PSScriptRoot 'Register-OfficeApiService.ps1') -InstallRoot $InstallRoot -ProgramDataRoot $ProgramDataRoot
}
Write-Output "Owned API service repair completed. Receipt: $serviceReceiptPath"
