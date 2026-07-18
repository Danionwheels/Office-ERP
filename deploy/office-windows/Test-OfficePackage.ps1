param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$executablePath = Join-Path $packagePath "SafarSuite.ControlDesk.Api.exe"
$indexPath = Join-Path $packagePath "wwwroot\index.html"
$manifestPath = Join-Path $packagePath "office-package-manifest.json"
$productionSettingsPath = Join-Path $packagePath "appsettings.Production.json"

foreach ($requiredPath in @($executablePath, $indexPath, $manifestPath, $productionSettingsPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required package file is missing: $requiredPath"
    }
}

function Set-CurrentProcessEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Overrides
    )

    $previous = @{}

    foreach ($entry in $Overrides.GetEnumerator()) {
        $previous[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key, "Process")
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
    }

    return $previous
}

function Restore-CurrentProcessEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Previous
    )

    foreach ($entry in $Previous.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value, "Process")
    }
}

function Assert-HttpStatus {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [int]$ExpectedStatusCode
    )

    $statusCode = $null

    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 5
        $statusCode = [int]$response.StatusCode
    }
    catch {
        if ($null -ne $_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
    }

    if ($statusCode -ne $ExpectedStatusCode) {
        throw "Expected HTTP $ExpectedStatusCode from '$Uri', received '$statusCode'."
    }
}

function Assert-PackagedStartupRejected {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$EnvironmentOverrides,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedOutputPattern
    )

    $previous = Set-CurrentProcessEnvironment -Overrides $EnvironmentOverrides
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $executablePath
    $startInfo.WorkingDirectory = $packagePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $rejectedProcess = [System.Diagnostics.Process]::new()
    $rejectedProcess.StartInfo = $startInfo

    try {
        if (-not $rejectedProcess.Start()) {
            throw "The production configuration-guard process did not start."
        }

        if (-not $rejectedProcess.WaitForExit(10000)) {
            $rejectedProcess.Kill()
            throw "The production package accepted a prohibited configuration and remained running."
        }

        $output = $rejectedProcess.StandardOutput.ReadToEnd() + $rejectedProcess.StandardError.ReadToEnd()
        if ($rejectedProcess.ExitCode -eq 0 -or $output -notmatch $ExpectedOutputPattern) {
            throw "The production package did not fail closed with expected evidence '$ExpectedOutputPattern'."
        }
    }
    finally {
        if (-not $rejectedProcess.HasExited) {
            $rejectedProcess.Kill()
            $rejectedProcess.WaitForExit(10000) | Out-Null
        }

        $rejectedProcess.Dispose()
        Restore-CurrentProcessEnvironment -Previous $previous
    }
}

function ConvertTo-Base64Url {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]]$Value
    )

    return [Convert]::ToBase64String($Value).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function New-SmokePasswordHash {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Password
    )

    $iterations = 10000
    $salt = New-Object byte[] 16
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $random.GetBytes($salt)
    $random.Dispose()

    $deriver = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $Password,
        $salt,
        $iterations,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256)

    try {
        $hash = $deriver.GetBytes(32)
    }
    finally {
        $deriver.Dispose()
    }

    return "pbkdf2-sha256.$iterations.$(ConvertTo-Base64Url $salt).$(ConvertTo-Base64Url $hash)"
}

$productionSettings = Get-Content -Raw -LiteralPath $productionSettingsPath | ConvertFrom-Json
if ($productionSettings.Persistence.Provider -ne "Postgres") {
    throw "Production package configuration must select PostgreSQL."
}

if ($productionSettings.Kestrel.Endpoints.OfficeLocal.Url -ne "http://127.0.0.1:5188") {
    throw "Production package configuration must bind the API to 127.0.0.1:5188."
}

Assert-PackagedStartupRejected `
    -EnvironmentOverrides ([ordered]@{
        ASPNETCORE_ENVIRONMENT = "Production"
        Persistence__Provider = "InMemory"
    }) `
    -ExpectedOutputPattern "requires PostgreSQL"

Assert-PackagedStartupRejected `
    -EnvironmentOverrides ([ordered]@{
        ASPNETCORE_ENVIRONMENT = "Production"
        Persistence__Provider = "Postgres"
        ConnectionStrings__ControlDesk = "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"
    }) `
    -ExpectedOutputPattern "must not use development"

$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $executablePath
$startInfo.WorkingDirectory = $packagePath
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true

$smokeOperatorEmail = "office.package.smoke@example.test"
$smokeOperatorPassword = "Office-package-smoke-password-123!"
$smokeOperatorPasswordHash = New-SmokePasswordHash -Password $smokeOperatorPassword

$environmentOverrides = [ordered]@{
    ASPNETCORE_ENVIRONMENT = "Development"
    ASPNETCORE_URLS = "http://127.0.0.1:5188"
    Persistence__Provider = "InMemory"
    ControlDesk__OperatorAccess__SessionSigningSecret = "office-package-smoke-session-signing-secret-20260718"
    ControlDesk__OperatorAccess__Users__0__UserId = "office-package-smoke"
    ControlDesk__OperatorAccess__Users__0__Email = $smokeOperatorEmail
    ControlDesk__OperatorAccess__Users__0__FullName = "Office Package Smoke"
    ControlDesk__OperatorAccess__Users__0__PasswordHash = $smokeOperatorPasswordHash
    ControlDesk__OperatorAccess__Users__0__Status = "Active"
    ControlDesk__OperatorAccess__Users__0__Roles__0 = "Administrator"
    ControlDesk__OperatorAccess__Users__0__Scopes__0 = "control-desk:admin"
}
$previousEnvironment = Set-CurrentProcessEnvironment -Overrides $environmentOverrides

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = $startInfo

try {
    $startedAtUtc = [DateTimeOffset]::UtcNow

    if (-not $process.Start()) {
        throw "The packaged Control Desk API did not start."
    }

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    $health = $null

    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($process.HasExited) {
            throw "The packaged Control Desk API exited with code $($process.ExitCode)."
        }

        try {
            $health = Invoke-RestMethod -Uri "http://127.0.0.1:5188/health" -TimeoutSec 2
            break
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    if ($null -eq $health -or $health.status -ne "Healthy") {
        throw "The packaged Control Desk API did not become healthy within 30 seconds."
    }

    $healthyAtUtc = [DateTimeOffset]::UtcNow
    $startupMilliseconds = [long]($healthyAtUtc - $startedAtUtc).TotalMilliseconds

    $rootResponse = Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:5188/" -TimeoutSec 5
    if ($rootResponse.StatusCode -ne 200 -or $rootResponse.Content -notmatch '<div id="root"></div>') {
        throw "The packaged React UI was not served from the API root."
    }

    Assert-HttpStatus -Uri "http://127.0.0.1:5188/api/v1/clients" -ExpectedStatusCode 401
    Assert-HttpStatus -Uri "http://127.0.0.1:5188/api/v1/not-a-real-route" -ExpectedStatusCode 404

    $loginBody = @{
        email = $smokeOperatorEmail
        password = $smokeOperatorPassword
        expiresInMinutes = 5
    } | ConvertTo-Json
    $session = Invoke-RestMethod `
        -Method Post `
        -Uri "http://127.0.0.1:5188/api/v1/auth/operator-sessions" `
        -ContentType "application/json" `
        -Body $loginBody `
        -TimeoutSec 5

    if ([string]::IsNullOrWhiteSpace($session.accessToken)) {
        throw "The packaged operator login did not return a bearer token."
    }

    $authenticatedResponse = Invoke-WebRequest `
        -UseBasicParsing `
        -Uri "http://127.0.0.1:5188/api/v1/clients?pageSize=1" `
        -Headers @{ Authorization = "$($session.tokenType) $($session.accessToken)" } `
        -TimeoutSec 5

    if ($authenticatedResponse.StatusCode -ne 200) {
        throw "The packaged authenticated business API call did not return HTTP 200."
    }

    $listeners = @(Get-NetTCPConnection -State Listen -OwningProcess $process.Id -ErrorAction Stop)
    if ($listeners.Count -eq 0) {
        throw "No listener was found for the packaged Control Desk process."
    }

    $nonLoopbackListeners = @($listeners | Where-Object {
        $_.LocalAddress -notin @("127.0.0.1", "::1")
    })

    if ($nonLoopbackListeners.Count -gt 0) {
        $addresses = ($nonLoopbackListeners.LocalAddress | Sort-Object -Unique) -join ", "
        throw "The packaged Control Desk process opened non-loopback listeners: $addresses"
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $evidence = [ordered]@{
        product = "SafarSuite Control Desk"
        proof = "office-windows-pilot-smoke-v1"
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        sourceRevision = $manifest.sourceRevision
        sourceTreeState = $manifest.sourceTreeState
        productionInMemoryRejected = $true
        productionDevelopmentConnectionRejected = $true
        startupMilliseconds = $startupMilliseconds
        healthStatus = $health.status
        uiRootStatusCode = 200
        anonymousBusinessApiStatusCode = 401
        authenticatedBusinessApiStatusCode = 200
        unknownApiStatusCode = 404
        listenerAddresses = @($listeners.LocalAddress | Sort-Object -Unique)
        loopbackOnly = $true
    }

    $evidence | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $packagePath "office-package-smoke-evidence.json") `
        -Encoding utf8

    Write-Host "Office package smoke passed."
    Write-Host "Production in-memory persistence: rejected"
    Write-Host "Production development connection: rejected"
    Write-Host "Health: $($health.status)"
    Write-Host "UI: same-origin root returned HTTP 200"
    Write-Host "Anonymous business API: HTTP 401"
    Write-Host "Authenticated business API: HTTP 200"
    Write-Host "Unknown API route: HTTP 404"
    Write-Host "Listeners: loopback only"
    Write-Host "Startup milliseconds: $startupMilliseconds"
}
finally {
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
    }

    $process.Dispose()

    Restore-CurrentProcessEnvironment -Previous $previousEnvironment
}
