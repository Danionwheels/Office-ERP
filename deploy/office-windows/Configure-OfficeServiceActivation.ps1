[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$DatabaseServiceName = 'SafarSuiteControlDeskPostgreSQL',
    [string]$ApiServiceName = 'SafarSuiteControlDeskApi'
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Service activation configuration requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Service activation configuration requires administrator elevation.' }

foreach ($name in @($DatabaseServiceName, $ApiServiceName)) {
    if ($null -eq (Get-Service -Name $name -ErrorAction SilentlyContinue)) { throw "Owned service '$name' is not installed." }
}

if ($PSCmdlet.ShouldProcess($DatabaseServiceName, 'Configure delayed automatic startup and bounded recovery')) {
    & sc.exe config $DatabaseServiceName start= delayed-auto | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not configure delayed automatic startup for '$DatabaseServiceName'." }
    & sc.exe failure $DatabaseServiceName reset= 86400 actions= restart/5000/restart/30000/none/0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not configure bounded recovery for '$DatabaseServiceName'." }
    & sc.exe config $ApiServiceName depend= $DatabaseServiceName start= delayed-auto | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not configure the API dependency/start policy." }
}

Write-Output "PostgreSQL delayed-start and API dependency contract verified."
