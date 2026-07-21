#Requires -Version 5.1
#Requires -RunAsAdministrator

[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$TestRoot)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($env:GITHUB_ACTIONS -ne 'true' -or [string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
    throw 'Native lifecycle cleanup may run only on a disposable GitHub Actions Windows runner.'
}

$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd('\')
$testPath = [IO.Path]::GetFullPath($TestRoot).TrimEnd('\')
if (-not $testPath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
    $testPath -notmatch 'safarsuite-office-db-[0-9a-f]{32}$') {
    throw 'Cleanup refused a test root outside the GUID-owned RUNNER_TEMP boundary.'
}

function Get-DisposableServiceConfiguration {
    param([Parameter(Mandatory = $true)][string]$Name)
    $escapedName = $Name.Replace("'", "''")
    return Get-CimInstance -ClassName Win32_Service -Filter "Name='$escapedName'" -ErrorAction SilentlyContinue
}

function Stop-DisposableService {
    param([Parameter(Mandatory = $true)][string]$Name)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    do {
        $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
        if ($null -eq $service) { return }
        try {
            $service.Refresh()
            if ($service.Status -eq 'Stopped') { return }
            if ($service.Status -notin @('StartPending', 'StopPending', 'PausePending', 'ContinuePending')) {
                try {
                    Stop-Service -InputObject $service -Force -ErrorAction Stop
                }
                catch {
                    $service.Refresh()
                    if ($service.Status -notin @('Stopped', 'StartPending', 'StopPending', 'PausePending', 'ContinuePending')) { throw }
                }
            }
        }
        finally {
            $service.Dispose()
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Cleanup timed out waiting for service '$Name' to stop."
}

function Wait-DisposableServiceAbsent {
    param([Parameter(Mandatory = $true)][string]$Name)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    do {
        $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
        try {
            $configuration = Get-DisposableServiceConfiguration -Name $Name
            if ($null -eq $service -and $null -eq $configuration) { return }
        }
        finally {
            if ($null -ne $service) { $service.Dispose() }
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Cleanup timed out waiting for service '$Name' to disappear."
}

$serviceName = 'SafarSuiteControlDeskPostgreSQL'
$serviceConfiguration = Get-DisposableServiceConfiguration -Name $serviceName
if ($null -ne $serviceConfiguration) {
    $expectedRuntimeParent = [IO.Path]::GetFullPath((Join-Path $testPath 'ProgramFiles\SafarSuite\ControlDesk\Database\PostgreSQL')).TrimEnd('\')
    $expectedData = [IO.Path]::GetFullPath((Join-Path $testPath 'ProgramData\SafarSuite\ControlDesk\Database\PostgreSQL17\Data'))
    $pathName = [string]$serviceConfiguration.PathName
    $executableMatch = [regex]::Match($pathName, '^\s*"(?<exe>[^"]+\\pg_ctl\.exe)"\s+runservice\s+')
    $dataMatch = [regex]::Match($pathName, '(?:^|\s)-D\s+"(?<data>[^"]+)"(?:\s|$)')
    $nameMatch = [regex]::Match($pathName, '(?:^|\s)-N\s+"(?<name>[^"]+)"(?:\s|$)')
    if (-not $executableMatch.Success -or -not $dataMatch.Success -or -not $nameMatch.Success -or
        -not [IO.Path]::GetFullPath($executableMatch.Groups['exe'].Value).StartsWith($expectedRuntimeParent + '\', [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFullPath($dataMatch.Groups['data'].Value) -ne $expectedData -or
        $nameMatch.Groups['name'].Value -cne $serviceName -or
        $pathName -notmatch '(?:^|\s)-w(?:\s|$)') {
        throw 'Cleanup refused to touch a service outside the GUID-owned test root.'
    }
    Stop-DisposableService -Name $serviceName
    & "$env:SystemRoot\System32\sc.exe" delete $serviceName | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Cleanup could not delete the disposable PostgreSQL service.'
    }
    Wait-DisposableServiceAbsent -Name $serviceName
}
elseif ($null -ne (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    throw 'Cleanup found a service whose ownership configuration could not be verified.'
}

$expectedRuntimeParent = [IO.Path]::GetFullPath((Join-Path $testPath 'ProgramFiles\SafarSuite\ControlDesk\Database\PostgreSQL')).TrimEnd('\')
$processDeadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
do {
    $ownedProcesses = @(Get-CimInstance -ClassName Win32_Process -Filter "Name='postgres.exe'" -ErrorAction SilentlyContinue | Where-Object {
        -not [string]::IsNullOrWhiteSpace([string]$_.ExecutablePath) -and
        [IO.Path]::GetFullPath([string]$_.ExecutablePath).StartsWith($expectedRuntimeParent + '\', [StringComparison]::OrdinalIgnoreCase)
    })
    if ($ownedProcesses.Count -eq 0) { break }
    Start-Sleep -Milliseconds 250
} while ([DateTimeOffset]::UtcNow -lt $processDeadline)
if ($ownedProcesses.Count -ne 0) {
    throw 'Cleanup timed out waiting for the owned PostgreSQL processes to exit.'
}

if (Test-Path -LiteralPath $testPath) {
    $markerPath = Join-Path $testPath '.safarsuite-native-ci-marker'
    $item = Get-Item -LiteralPath $testPath -Force
    $markerValue = if (Test-Path -LiteralPath $markerPath -PathType Leaf) { (Get-Content -Raw -LiteralPath $markerPath).Trim() } else { $null }
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf) -or
        $markerValue -cne 'owned-disposable-github-runner-root' -or
        [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Cleanup refused an unmarked or reparse-point test root.'
    }
    Remove-Item -LiteralPath $testPath -Recurse -Force
}

Write-Host 'Disposable office database lifecycle state is absent.'
