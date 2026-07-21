[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:5188',
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
if ($BaseUrl -notmatch '^http://127\.0\.0\.1:[0-9]+$') { throw 'Control Desk launcher URL must be loopback-only.' }
$readyUrl = "$BaseUrl/ready"
$deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
$lastFailure = $null
do {
    try {
        $readiness = Invoke-RestMethod -Uri $readyUrl -TimeoutSec 2
        if ($readiness.status -eq 'Ready' -and $readiness.database.status -eq 'Ready') {
            Start-Process $BaseUrl
            Write-Output "Control Desk opened at $BaseUrl."
            return
        }
        $lastFailure = "Readiness reported '$($readiness.status)' with database '$($readiness.database.status)'."
    }
    catch { $lastFailure = 'The local Control Desk API is not responding yet.' }
    Start-Sleep -Milliseconds 500
} while ([DateTimeOffset]::UtcNow -lt $deadline)

throw "Control Desk did not become ready within $TimeoutSeconds seconds. $lastFailure Start the API service or run the repair command as administrator."
