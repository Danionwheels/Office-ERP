$ErrorActionPreference = 'Stop'

$setup = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Install-OfficeControlDesk.ps1')
$registration = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Register-OfficeApiService.ps1')
$uninstall = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Uninstall-OfficeControlDesk.ps1')

$checks = [ordered]@{
    orderedCheckpoints = $setup.Contains("checkpoint = 'DatabaseReady'") -and $setup.Contains('$checkpoint = ''OperatorReady''') -and $setup.Contains('$checkpoint = ''Ready''')
    interruptionBoundary = $setup.Contains('Office setup failed at checkpoint ''$checkpoint''')
    serviceRollback = $setup.Contains("Stop-Service -Name 'SafarSuiteControlDeskApi'")
    durableStatePreserved = $setup.Contains('database, operators, and secrets were not removed')
    aclConvergence = $registration.Contains('icacls.exe') -and $registration.Contains('NT SERVICE\SafarSuiteControlDeskApi')
    foreignRefusal = $registration.Contains('foreign service') -and $uninstall.Contains('Foreign API service detected')
    reinstallReceipt = $uninstall.Contains('ownership receipt') -and $uninstall.Contains('PostgreSQL data')
}
foreach ($check in $checks.GetEnumerator()) {
    if (-not $check.Value) { throw "Setup lifecycle contract check failed: $($check.Key)" }
}

$simulationRoot = Join-Path ([IO.Path]::GetTempPath()) ('safarsuite-setup-lifecycle-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $simulationRoot | Out-Null
try {
    $durable = @('database.cluster', 'operators.json', 'machine-secrets.json', 'database-receipt.json') |
        ForEach-Object { $path = Join-Path $simulationRoot $_; Set-Content -LiteralPath $path -Value ('durable-' + $_); $path }

    function Invoke-SetupSimulation([string]$FailureCheckpoint) {
        $checkpoint = 'Starting'
        $serviceRunning = $false
        try {
            $checkpoint = 'DatabaseReady'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $checkpoint = 'OperatorReady'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $checkpoint = 'ApiPayloadInstalled'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $checkpoint = 'ApiRegistered'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $checkpoint = 'ServicesConfigured'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $checkpoint = 'ShortcutsInstalled'
            if ($FailureCheckpoint -eq $checkpoint) { throw 'simulated interruption' }
            $serviceRunning = $true
            $checkpoint = 'Ready'
            [pscustomobject]@{ checkpoint = $checkpoint; serviceRunning = $serviceRunning; failed = $false }
        }
        catch {
            if ($checkpoint -in @('ApiRegistered', 'ServicesConfigured', 'ShortcutsInstalled')) { $serviceRunning = $false }
            [pscustomobject]@{ checkpoint = $checkpoint; serviceRunning = $serviceRunning; failed = $true }
        }
    }

    foreach ($failure in @('DatabaseReady', 'OperatorReady', 'ApiPayloadInstalled', 'ApiRegistered', 'ServicesConfigured', 'ShortcutsInstalled')) {
        $result = Invoke-SetupSimulation $failure
        if (-not $result.failed) { throw "Interruption at '$failure' was not captured." }
        if ($result.serviceRunning) { throw "Service remained running after interruption at '$failure'." }
        foreach ($path in $durable) {
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Durable state was lost after '$failure'." }
        }
    }

    $first = Invoke-SetupSimulation ''
    $second = Invoke-SetupSimulation ''
    if ($first.checkpoint -ne 'Ready' -or $second.checkpoint -ne 'Ready') { throw 'Successful setup simulation did not reach Ready twice.' }
    foreach ($path in $durable) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Durable state was lost during reinstall simulation: $path" }
    }
}
finally {
    if (Test-Path -LiteralPath $simulationRoot) { Remove-Item -LiteralPath $simulationRoot -Recurse -Force }
}

Write-Host "Complete setup lifecycle hermetic contract: passed ($($checks.Count) static checks plus interruption/recovery/reinstall simulations)"
