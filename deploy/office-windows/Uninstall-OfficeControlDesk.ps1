[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$InstallRoot,
    [Parameter(Mandatory)] [string]$ProgramDataRoot
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Control Desk uninstall requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Control Desk uninstall requires administrator elevation.' }

$serviceName = 'SafarSuiteControlDeskApi'
$service = Get-CimInstance Win32_Service -Filter "Name='$serviceName'" -ErrorAction SilentlyContinue
$receiptPath = Join-Path $InstallRoot 'api-installation-receipt.json'
if ($null -ne $service -and -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw 'API service has no ownership receipt; uninstall refused.' }
if ($null -ne $service -and $service.PathName -notlike ('"' + [IO.Path]::GetFullPath((Join-Path $InstallRoot 'Api\SafarSuite.ControlDesk.Api.exe')) + '"*')) { throw 'Foreign API service detected; uninstall refused.' }

if ($PSCmdlet.ShouldProcess('SafarSuite Control Desk API payload', 'Remove owned service, binaries, and shortcuts')) {
    if ($null -ne $service) {
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        & sc.exe delete $serviceName | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Could not remove the owned API service.' }
    }
    foreach ($shortcut in @(
        (Join-Path ([Environment]::GetFolderPath('CommonPrograms')) 'SafarSuite Control Desk.lnk'),
        (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'SafarSuite Control Desk.lnk')) {
        if (Test-Path -LiteralPath $shortcut) { Remove-Item -LiteralPath $shortcut -Force }
    }
    if (Test-Path -LiteralPath (Join-Path $InstallRoot 'Api')) { Remove-Item -LiteralPath (Join-Path $InstallRoot 'Api') -Recurse -Force }
}

Write-Output "Owned API payload removed. PostgreSQL data, operators, machine secrets, receipts, and logs under '$ProgramDataRoot' were preserved."
