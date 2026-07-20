param(
    [Parameter(Mandatory = $true)]
    [string]$TestRoot,

    [Parameter(Mandatory = $true)]
    [string]$EvidencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-Proof {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-NormalizedRights {
    param([Parameter(Mandatory = $true)][Security.AccessControl.FileSystemRights]$Rights)

    return [int]($Rights -band (-bnot [int][Security.AccessControl.FileSystemRights]::Synchronize))
}

function Set-ExactMachineSecretAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][bool]$Directory,
        [Parameter(Mandatory = $true)][bool]$Installed
    )

    $security = if ($Directory) {
        [Security.AccessControl.DirectorySecurity]::new()
    }
    else {
        [Security.AccessControl.FileSecurity]::new()
    }
    $systemSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-18')
    $administratorsSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
    $serviceSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-80-2177609957-237951300-3651597395-3114367455-1078186923')
    $security.SetOwner($systemSid)
    $security.SetAccessRuleProtection($true, $false)
    $inheritance = if ($Directory) {
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
    }
    else {
        [Security.AccessControl.InheritanceFlags]::None
    }

    foreach ($identity in @($systemSid, $administratorsSid)) {
        [void]$security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $identity,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow))
    }

    if ($Installed) {
        [void]$security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $serviceSid,
            $(if ($Directory) { [Security.AccessControl.FileSystemRights]::ReadAndExecute } else { [Security.AccessControl.FileSystemRights]::Read }),
            [Security.AccessControl.InheritanceFlags]::None,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow))
    }

    Set-Acl -LiteralPath $Path -AclObject $security
}

function Assert-ExactMachineSecretAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][bool]$Directory,
        [Parameter(Mandatory = $true)][bool]$Installed
    )

    $acl = Get-Acl -LiteralPath $Path
    Assert-Proof -Condition $acl.AreAccessRulesProtected -Message 'Machine-secret ACL inheritance is enabled.'
    Assert-Proof -Condition ($acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -eq 'S-1-5-18') -Message 'Machine-secret ACL owner is not SYSTEM.'
    $inheritance = if ($Directory) {
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
    }
    else {
        [Security.AccessControl.InheritanceFlags]::None
    }
    $expected = @(
        [pscustomobject]@{ Sid = 'S-1-5-18'; Rights = [Security.AccessControl.FileSystemRights]::FullControl },
        [pscustomobject]@{ Sid = 'S-1-5-32-544'; Rights = [Security.AccessControl.FileSystemRights]::FullControl }
    )
    if ($Installed) {
        $expected += [pscustomobject]@{
            Sid = 'S-1-5-80-2177609957-237951300-3651597395-3114367455-1078186923'
            Rights = $(if ($Directory) { [Security.AccessControl.FileSystemRights]::ReadAndExecute } else { [Security.AccessControl.FileSystemRights]::Read })
        }
    }
    $actual = @($acl.Access)
    Assert-Proof -Condition ($actual.Count -eq $expected.Count) -Message 'Machine-secret ACL has an unexpected rule count.'
    foreach ($expectedRule in $expected) {
        $matches = @($actual | Where-Object {
            $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -eq $expectedRule.Sid -and
            (Get-NormalizedRights $_.FileSystemRights) -eq (Get-NormalizedRights $expectedRule.Rights) -and
            $_.InheritanceFlags -eq $inheritance -and
            $_.PropagationFlags -eq [Security.AccessControl.PropagationFlags]::None -and
            $_.AccessControlType -eq [Security.AccessControl.AccessControlType]::Allow -and
            -not $_.IsInherited
        })
        Assert-Proof -Condition ($matches.Count -eq 1) -Message 'Machine-secret ACL differs from the exact allowlist.'
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'The machine-secret ACL proof requires Windows.'
}
if (Test-Path -LiteralPath $TestRoot) {
    throw 'The machine-secret proof root already exists.'
}
New-Item -ItemType Directory -Path $TestRoot | Out-Null
$machineDirectory = Join-Path $TestRoot 'Machine'
$envelopePath = Join-Path $machineDirectory 'control-desk-machine-secrets.v1.json'
$replacementPath = Join-Path $machineDirectory 'replacement-envelope.tmp'
$resultRoot = Join-Path $TestRoot 'NormalUserResult'
$resultPath = Join-Path $resultRoot 'probe.json'
$userName = "SafarSuiteProbe_$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
$password = "CdProbe-$([Guid]::NewGuid().ToString('N'))-A9!"
$user = $null
$evidence = $null

try {
    New-Item -ItemType Directory -Path $machineDirectory | Out-Null
    New-Item -ItemType File -Path $envelopePath | Out-Null
    Set-ExactMachineSecretAcl -Path $machineDirectory -Directory $true -Installed $false
    Set-ExactMachineSecretAcl -Path $envelopePath -Directory $false -Installed $false
    Assert-ExactMachineSecretAcl -Path $machineDirectory -Directory $true -Installed $false
    Assert-ExactMachineSecretAcl -Path $envelopePath -Directory $false -Installed $false

    Set-ExactMachineSecretAcl -Path $machineDirectory -Directory $true -Installed $true
    Set-ExactMachineSecretAcl -Path $envelopePath -Directory $false -Installed $true
    Assert-ExactMachineSecretAcl -Path $machineDirectory -Directory $true -Installed $true
    Assert-ExactMachineSecretAcl -Path $envelopePath -Directory $false -Installed $true
    [IO.File]::WriteAllText($replacementPath, 'replacement-envelope')

    New-Item -ItemType Directory -Path $resultRoot | Out-Null
    icacls.exe $resultRoot /grant '*S-1-5-32-545:(OI)(CI)M' /inheritance:e | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'The normal-user probe result directory could not be prepared.'
    }

    $localMachine = [ADSI]"WinNT://$env:COMPUTERNAME"
    $user = $localMachine.Create('user', $userName)
    $user.SetPassword($password)
    $user.UserFlags = 512
    $user.SetInfo()
    $securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
    $credential = [PSCredential]::new("$env:COMPUTERNAME\$userName", $securePassword)
    $childScript = Join-Path $PSScriptRoot 'Invoke-MachineSecretNormalUserProbe.ps1'
    $process = Start-Process -FilePath "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
        -ArgumentList @('-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $childScript, '-EnvelopePath', $envelopePath, '-ResultPath', $resultPath, '-ReplacementPath', $replacementPath) `
        -Credential $credential -WorkingDirectory $env:SystemRoot -PassThru -Wait
    Assert-Proof -Condition ($process.ExitCode -eq 0) -Message 'The normal-user child process failed.'
    $probe = Get-Content -Raw -LiteralPath $resultPath | ConvertFrom-Json
    Assert-Proof -Condition ($probe.readDenied -and $probe.writeDenied -and $probe.deleteDenied -and $probe.replaceDenied) -Message 'A normal Windows account could access the machine-secret envelope.'

    $evidence = [ordered]@{
        proof = 'control-desk-machine-secret-acl-v1'
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        preServiceAclPassed = $true
        installedAclPassed = $true
        serviceSid = 'S-1-5-80-2177609957-237951300-3651597395-3114367455-1078186923'
        normalUserReadDenied = [bool]$probe.readDenied
        normalUserWriteDenied = [bool]$probe.writeDenied
        normalUserDeleteDenied = [bool]$probe.deleteDenied
        normalUserReplaceDenied = [bool]$probe.replaceDenied
        secretFreeEvidence = $true
    }
    $evidence | ConvertTo-Json | Set-Content -LiteralPath $EvidencePath -Encoding utf8
    Write-Host 'Machine-secret ACL native proof passed.'
    Write-Host 'Pre-service ACL: passed'
    Write-Host 'Installed service-SID ACL: passed'
    Write-Host 'Normal-user read/replace denial: passed'
}
finally {
    if ($null -ne $user) {
        try {
            $user.Delete()
        }
        catch {
        }
    }
    if (Test-Path -LiteralPath $TestRoot) {
        Remove-Item -LiteralPath $TestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
