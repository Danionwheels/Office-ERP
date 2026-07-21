[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string]$InstallRoot,
    [Parameter(Mandatory)] [string]$ProgramDataRoot,
    [string]$ServiceName = 'SafarSuiteControlDeskApi',
    [string]$DatabaseServiceName = 'SafarSuiteControlDeskPostgreSQL'
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'API service registration requires Windows.' }
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'API service registration requires administrator elevation.' }

$apiPath = Join-Path $InstallRoot 'Api\SafarSuite.ControlDesk.Api.exe'
$receiptPath = Join-Path $InstallRoot 'api-installation-receipt.json'
$serviceReceiptPath = Join-Path $ProgramDataRoot 'Service\api-service-receipt.json'
$configRoot = Join-Path $ProgramDataRoot 'Config'
$logRoot = Join-Path $ProgramDataRoot 'Logs'
$machineSecretDirectory = Join-Path $ProgramDataRoot 'Secrets\Machine'
$machineSecretPath = Join-Path $machineSecretDirectory 'control-desk-machine-secrets.v1.json'
if (-not (Test-Path -LiteralPath $apiPath -PathType Leaf) -or -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw 'The owned API payload and installation receipt are required before service registration.' }
if (-not (Test-Path -LiteralPath $machineSecretDirectory -PathType Container) -or -not (Test-Path -LiteralPath $machineSecretPath -PathType Leaf)) { throw 'The protected machine-secret envelope is required before API service registration.' }

$existing = Get-CimInstance Win32_Service -Filter "Name='$ServiceName'" -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    $expectedPath = '"' + [IO.Path]::GetFullPath($apiPath) + '"'
    if ($existing.PathName -notlike "$expectedPath*") { throw "A foreign service already owns '$ServiceName'." }
    if ($existing.StartName -ne 'NT SERVICE\SafarSuiteControlDeskApi') { throw 'The existing API service identity is foreign.' }
}

$binPath = '"' + [IO.Path]::GetFullPath($apiPath) + '"'
function Set-OfficeApiAcl([string]$Path, [string]$ServiceRights) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    & icacls.exe $Path /inheritance:r /grant:r `
        'SYSTEM:(OI)(CI)(F)' `
        'Administrators:(OI)(CI)(F)' `
        ("NT SERVICE\SafarSuiteControlDeskApi:(OI)(CI)($ServiceRights)") | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not converge ACLs for '$Path'." }
}

function Set-OfficeMachineSecretAcl([string]$Directory, [string]$Envelope) {
    $systemSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-18')
    $administratorsSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
    $serviceSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-80-2177609957-237951300-3651597395-3114367455-1078186923')

    $directorySecurity = [Security.AccessControl.DirectorySecurity]::new()
    $directorySecurity.SetOwner($systemSid)
    $directorySecurity.SetAccessRuleProtection($true, $false)
    $directoryInheritance = [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
    foreach ($identity in @($systemSid, $administratorsSid)) {
        [void]$directorySecurity.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $identity,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $directoryInheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow))
    }
    [void]$directorySecurity.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $serviceSid,
        [Security.AccessControl.FileSystemRights]::ReadAndExecute,
        [Security.AccessControl.InheritanceFlags]::None,
        [Security.AccessControl.PropagationFlags]::None,
        [Security.AccessControl.AccessControlType]::Allow))
    Set-Acl -LiteralPath $Directory -AclObject $directorySecurity

    $fileSecurity = [Security.AccessControl.FileSecurity]::new()
    $fileSecurity.SetOwner($systemSid)
    $fileSecurity.SetAccessRuleProtection($true, $false)
    foreach ($identity in @($systemSid, $administratorsSid)) {
        [void]$fileSecurity.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $identity,
            [Security.AccessControl.FileSystemRights]::FullControl,
            [Security.AccessControl.InheritanceFlags]::None,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow))
    }
    [void]$fileSecurity.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $serviceSid,
        [Security.AccessControl.FileSystemRights]::Read,
        [Security.AccessControl.InheritanceFlags]::None,
        [Security.AccessControl.PropagationFlags]::None,
        [Security.AccessControl.AccessControlType]::Allow))
    Set-Acl -LiteralPath $Envelope -AclObject $fileSecurity
}

if ($PSCmdlet.ShouldProcess($ServiceName, 'Register or verify demand-start API service')) {
    if ($null -eq $existing) {
        & sc.exe create $ServiceName binPath= $binPath start= demand obj= 'NT SERVICE\SafarSuiteControlDeskApi' DisplayName= 'SafarSuite Control Desk API' | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Windows Service Control Manager could not create '$ServiceName'." }
    }
    & sc.exe config $ServiceName depend= $DatabaseServiceName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Windows Service Control Manager could not set the '$ServiceName' dependency." }
    & sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/30000/none/0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Windows Service Control Manager could not set '$ServiceName' recovery actions." }
    Set-OfficeApiAcl $InstallRoot 'RX'
    Set-OfficeApiAcl $configRoot 'M'
    Set-OfficeApiAcl $logRoot 'M'
    Set-OfficeMachineSecretAcl $machineSecretDirectory $machineSecretPath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $serviceReceiptPath) | Out-Null
    [ordered]@{ product = 'SafarSuite Control Desk'; serviceName = $ServiceName; serviceAccount = 'NT SERVICE\SafarSuiteControlDeskApi'; executable = [IO.Path]::GetFullPath($apiPath); dependsOn = @($DatabaseServiceName); startupType = 'demand'; registeredAtUtc = [DateTimeOffset]::UtcNow.ToString('O') } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $serviceReceiptPath -Encoding utf8
}

Write-Output "API service contract verified: $ServiceName (demand-start, depends on $DatabaseServiceName)."
