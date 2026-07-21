param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$EvidenceDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$executablePath = Join-Path $packagePath "SafarSuite.ControlDesk.Api.exe"
$indexPath = Join-Path $packagePath "wwwroot\index.html"
$manifestPath = Join-Path $packagePath "office-package-manifest.json"
$productionSettingsPath = Join-Path $packagePath "appsettings.Production.json"
$databaseDirectory = Join-Path $packagePath "database"
$databaseManifestPath = Join-Path $databaseDirectory "database-package-manifest.json"
$databaseLifecycleModulePath = Join-Path $databaseDirectory "OfficeDatabaseLifecycle.psm1"
$evidenceRoot = if ([string]::IsNullOrWhiteSpace($EvidenceDirectory)) {
    Join-Path ([IO.Path]::GetTempPath()) "safarsuite-office-package-evidence-$([Guid]::NewGuid().ToString('N'))"
}
else {
    [IO.Path]::GetFullPath($EvidenceDirectory)
}
New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
$logDirectory = Join-Path $evidenceRoot "smoke-logs-$([Guid]::NewGuid().ToString('N'))"

foreach ($requiredPath in @(
    $executablePath,
    $indexPath,
    $manifestPath,
    $productionSettingsPath,
    $databaseManifestPath,
    $databaseLifecycleModulePath,
    (Join-Path $databaseDirectory "Install-OfficeDatabase.ps1"),
    (Join-Path $databaseDirectory "Repair-OfficeDatabase.ps1"),
    (Join-Path $databaseDirectory "Uninstall-OfficeDatabase.ps1")
)) {
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
    $rejectedProcessStarted = $false
    $stdoutTask = $null
    $stderrTask = $null

    try {
        if (-not $rejectedProcess.Start()) {
            throw "The production configuration-guard process did not start."
        }
        $rejectedProcessStarted = $true
        $stdoutTask = $rejectedProcess.StandardOutput.ReadToEndAsync()
        $stderrTask = $rejectedProcess.StandardError.ReadToEndAsync()

        if (-not $rejectedProcess.WaitForExit(10000)) {
            $rejectedProcess.Kill()
            $rejectedProcess.WaitForExit(10000) | Out-Null
            $null = $stdoutTask.Result
            $null = $stderrTask.Result
            throw "The production package accepted a prohibited configuration and remained running."
        }

        $output = $stdoutTask.Result + $stderrTask.Result
        if ($rejectedProcess.ExitCode -eq 0 -or $output -notmatch $ExpectedOutputPattern) {
            throw "The production package did not fail closed with expected evidence '$ExpectedOutputPattern'."
        }
    }
    finally {
        if ($rejectedProcessStarted -and -not $rejectedProcess.HasExited) {
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
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

if (Test-Path -LiteralPath (Join-Path $packagePath "appsettings.Development.json")) {
    throw "The office package must not include Development settings or fixture credentials."
}
$packagedSettingsText = (Get-ChildItem -LiteralPath $packagePath -Filter "appsettings*.json" -File |
    ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join [Environment]::NewLine
if ($packagedSettingsText -match "local-development|safarsuite-owner-dev-key|safarsuite_dev_password") {
    throw "The office package settings contain development-only secret material."
}
Import-Module $databaseLifecycleModulePath -Force
$databaseManifest = Test-OfficeDatabasePackage -PackageDirectory $packagePath
if ($manifest.packageFormat -ne "office-windows-native-postgresql-v2") {
    throw "The office package does not use the native PostgreSQL package format."
}

$databaseManifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $databaseManifestPath).Hash.ToUpperInvariant()
if ($databaseManifestHash -ne ([string]$manifest.services.database.manifestSha256).ToUpperInvariant()) {
    throw "The top-level package manifest does not bind the database manifest."
}

if ($databaseManifest.migrations.count -ne 33 -or
    $databaseManifest.migrations.target -ne "20260720035506_AddLocalOperatorAuthentication" -or
    @($databaseManifest.migrations.requiredExtensions) -notcontains "pg_trgm") {
    throw "The package does not contain the reviewed Control Desk migration target."
}

if (@($manifest.services.api.dependsOn).Count -ne 1 -or
    $manifest.services.api.dependsOn[0] -ne $manifest.services.database.name) {
    throw "The office package does not declare the exact API-to-PostgreSQL service dependency."
}

$visualCppRuntimePath = Join-Path $databaseDirectory ([string]$databaseManifest.postgresql.visualCppRuntime.archiveFileName)
$visualCppSignature = Get-AuthenticodeSignature -LiteralPath $visualCppRuntimePath
if ($visualCppSignature.Status -ne "Valid" -or $visualCppSignature.SignerCertificate.Subject -notmatch "Microsoft Corporation") {
    throw "The packaged Microsoft Visual C++ runtime signature is invalid."
}
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
        ControlDesk__Logging__File__Enabled = "false"
    }) `
    -ExpectedOutputPattern "requires PostgreSQL"

Assert-PackagedStartupRejected `
    -EnvironmentOverrides ([ordered]@{
        ASPNETCORE_ENVIRONMENT = "Production"
        Persistence__Provider = "Postgres"
        ConnectionStrings__ControlDesk = "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password"
        ControlDesk__Logging__File__Enabled = "false"
    }) `
    -ExpectedOutputPattern "must not use development"

Assert-PackagedStartupRejected `
    -EnvironmentOverrides ([ordered]@{
        ASPNETCORE_ENVIRONMENT = "Production"
        Persistence__Provider = "Postgres"
        ConnectionStrings__ControlDesk = "Host=localhost;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=office-package-production-proof"
        ControlDesk__Logging__File__Enabled = "false"
        ControlCloud__OutboxWorker__Enabled = "false"
    }) `
    -ExpectedOutputPattern "machine-secret"

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
    ControlCloud__OutboxWorker__Enabled = "false"
    ControlDesk__Logging__File__Enabled = "true"
    ControlDesk__Logging__File__DirectoryPath = $logDirectory
    ControlDesk__Logging__File__RetainedFileCountLimit = "4"
    ControlDesk__Logging__File__RetainedDays = "2"
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
$restartProcess = $null
$processStarted = $false
$restartProcessStarted = $false

try {
    $startedAtUtc = [DateTimeOffset]::UtcNow

    if (-not $process.Start()) {
        throw "The packaged Control Desk API did not start."
    }
    $processStarted = $true

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
    $readiness = Invoke-RestMethod -Uri "http://127.0.0.1:5188/ready" -TimeoutSec 5

    if ($readiness.status -ne "Ready" -or $readiness.database.status -ne "Ready") {
        throw "The packaged Control Desk API did not separate ready persistence from process liveness."
    }

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

    $diagnosticsResponse = Invoke-WebRequest `
        -UseBasicParsing `
        -Uri "http://127.0.0.1:5188/api/v1/diagnostics/summary" `
        -Headers @{ Authorization = "$($session.tokenType) $($session.accessToken)" } `
        -TimeoutSec 10

    if ($diagnosticsResponse.StatusCode -ne 200) {
        throw "The packaged authorized diagnostics summary did not return HTTP 200."
    }

    $diagnostics = $diagnosticsResponse.Content | ConvertFrom-Json
    if ($manifest.sourceRevision -ne "unknown" `
        -and $diagnostics.service.version -notmatch [Regex]::Escape($manifest.sourceRevision)) {
        throw "The diagnostics build version did not include the package source revision."
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

    $process.Kill()
    if (-not $process.WaitForExit(10000)) {
        throw "The packaged Control Desk API did not exit after abrupt termination."
    }
    Start-Sleep -Milliseconds 250

    $restartProcess = [System.Diagnostics.Process]::new()
    $restartProcess.StartInfo = $startInfo
    if (-not $restartProcess.Start()) {
        throw "The packaged Control Desk API did not restart after abrupt termination."
    }
    $restartProcessStarted = $true

    $restartDeadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    $recoveredHealth = $null

    while ([DateTimeOffset]::UtcNow -lt $restartDeadline) {
        if ($restartProcess.HasExited) {
            throw "The restarted packaged Control Desk API exited with code $($restartProcess.ExitCode)."
        }

        try {
            $recoveredHealth = Invoke-RestMethod -Uri "http://127.0.0.1:5188/health" -TimeoutSec 2
            break
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    if ($null -eq $recoveredHealth -or $recoveredHealth.status -ne "Healthy") {
        throw "The packaged Control Desk API did not recover after abrupt termination."
    }

    $recoveredReadiness = Invoke-RestMethod -Uri "http://127.0.0.1:5188/ready" -TimeoutSec 5
    if ($recoveredReadiness.status -ne "Ready") {
        throw "The restarted packaged Control Desk API did not return to Ready."
    }

    $restartProcess.Kill()
    if (-not $restartProcess.WaitForExit(10000)) {
        throw "The restarted packaged Control Desk API did not exit after the recovery proof."
    }
    Start-Sleep -Milliseconds 250

    $logFiles = @(Get-ChildItem -LiteralPath $logDirectory -Filter "control-desk-*.jsonl" -File)
    if ($logFiles.Count -eq 0) {
        throw "The packaged Control Desk process did not retain a rolling file log."
    }

    $retainedLog = ($logFiles | ForEach-Object {
        Get-Content -Raw -LiteralPath $_.FullName
    }) -join [Environment]::NewLine

    if ($retainedLog -notmatch "OfficeHostStarted") {
        throw "The retained log did not preserve startup evidence before abrupt termination."
    }

    if ($retainedLog -notmatch "OfficeReadinessConfirmed") {
        throw "The retained log did not preserve the successful readiness check."
    }

    if ([Regex]::Matches($retainedLog, "OfficeHostStarted").Count -lt 2) {
        throw "The retained log did not prove a successful restart after abrupt termination."
    }

    foreach ($secretValue in @(
        $smokeOperatorPassword,
        $smokeOperatorPasswordHash,
        $environmentOverrides.ControlDesk__OperatorAccess__SessionSigningSecret,
        $session.accessToken
    )) {
        if ($retainedLog.IndexOf($secretValue, [StringComparison]::Ordinal) -ge 0) {
            throw "A protected value was written to the retained log."
        }

        if ($diagnosticsResponse.Content.IndexOf($secretValue, [StringComparison]::Ordinal) -ge 0) {
            throw "A protected value was exposed by the diagnostics summary."
        }
    }

    $evidence = [ordered]@{
        product = "SafarSuite Control Desk"
        proof = "office-windows-pilot-smoke-v3"
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        sourceRevision = $manifest.sourceRevision
        sourceTreeState = $manifest.sourceTreeState
        productionInMemoryRejected = $true
        productionDevelopmentConnectionRejected = $true
        startupMilliseconds = $startupMilliseconds
        healthStatus = $health.status
        readinessStatus = $readiness.status
        diagnosticsStatusCode = 200
        diagnosticsVersion = $diagnostics.service.version
        retainedLogFileCount = $logFiles.Count
        abruptStopEvidenceRetained = $true
        abruptStopRestartRecovered = $true
        diagnosticsAndLogsSecretScanPassed = $true
        uiRootStatusCode = 200
        anonymousBusinessApiStatusCode = 401
        authenticatedBusinessApiStatusCode = 200
        unknownApiStatusCode = 404
        listenerAddresses = @($listeners.LocalAddress | Sort-Object -Unique)
        loopbackOnly = $true
    }

    $evidence | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $evidenceRoot "office-package-smoke-evidence.json") `
        -Encoding utf8

    Write-Host "Office package smoke passed."
    Write-Host "Production in-memory persistence: rejected"
    Write-Host "Production development connection: rejected"
    Write-Host "Health: $($health.status)"
    Write-Host "Readiness: $($readiness.status)"
    Write-Host "Authorized diagnostics: HTTP 200"
    Write-Host "Retained rolling logs: startup evidence survived abrupt stop"
    Write-Host "Abrupt-stop restart: recovered to Ready"
    Write-Host "Diagnostics/log secret scan: passed"
    Write-Host "UI: same-origin root returned HTTP 200"
    Write-Host "Anonymous business API: HTTP 401"
    Write-Host "Authenticated business API: HTTP 200"
    Write-Host "Unknown API route: HTTP 404"
    Write-Host "Listeners: loopback only"
    Write-Host "Startup milliseconds: $startupMilliseconds"
}
finally {
    if ($null -ne $restartProcess) {
        if ($restartProcessStarted -and -not $restartProcess.HasExited) {
            $restartProcess.Kill()
            $restartProcess.WaitForExit(10000) | Out-Null
        }

        $restartProcess.Dispose()
    }

    if ($processStarted -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(10000) | Out-Null
    }

    $process.Dispose()

    Restore-CurrentProcessEnvironment -Previous $previousEnvironment
}
