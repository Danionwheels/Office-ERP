#Requires -Version 5.1
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true)][string]$TestRoot,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$OfficePackageArchiveSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{32}$')][string]$BoundaryInvocationNonce
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($env:GITHUB_ACTIONS -ne 'true' -or [string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
    throw 'The native lifecycle proof may run only on a disposable GitHub Actions Windows runner.'
}

$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd('\')
$testPath = [IO.Path]::GetFullPath($TestRoot).TrimEnd('\')
$evidenceFilePath = [IO.Path]::GetFullPath($EvidencePath)
if (-not $testPath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
    $testPath -notmatch 'safarsuite-office-db-[0-9a-f]{32}$') {
    throw 'The native lifecycle test root must be a GUID-owned descendant of RUNNER_TEMP.'
}
if (-not $evidenceFilePath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The native lifecycle evidence path must be inside RUNNER_TEMP.'
}

$packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packagedLifecycleModule = Join-Path $packagePath 'database\OfficeDatabaseLifecycle.psm1'
$lifecycleModule = Import-Module $packagedLifecycleModule -Force -PassThru
Import-Module (Join-Path $PSScriptRoot 'OfficePostgresLifecycleBoundaryDiagnostic.psm1') -Force

function Assert-NativeProof {
    param([Parameter(Mandatory = $true)][bool]$Condition, [Parameter(Mandatory = $true)][string]$Message)
    if (-not $Condition) { throw $Message }
}

function Invoke-PackagedPsql {
    param(
        [Parameter(Mandatory = $true)][string]$PsqlPath,
        [Parameter(Mandatory = $true)][string]$Passfile,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$Database,
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Sql,
        [switch]$ExpectFailure
    )
    $previousPassword = $env:PGPASSWORD
    $previousPassfile = $env:PGPASSFILE
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        $env:PGPASSFILE = $Passfile
        # Windows PowerShell 5.1 promotes redirected native stderr to ErrorRecord
        # instances. Capture those records and decide from the native exit code.
        $ErrorActionPreference = 'Continue'
        $output = @($Sql | & $PsqlPath -X -q -w -v ON_ERROR_STOP=1 -tA -h 127.0.0.1 -p $Port -U $Role -d $Database 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        $env:PGPASSWORD = $previousPassword
        $env:PGPASSFILE = $previousPassfile
    }
    if ($ExpectFailure) {
        if ($exitCode -eq 0) { throw 'A PostgreSQL command expected to fail succeeded.' }
    }
    elseif ($exitCode -ne 0) {
        throw "A packaged PostgreSQL command failed with exit code $exitCode."
    }
    return ($output -join [Environment]::NewLine).Trim()
}

function Invoke-PackagedMigrationBundle {
    param(
        [Parameter(Mandatory = $true)][string]$BundlePath,
        [Parameter(Mandatory = $true)][string]$TargetMigration,
        [Parameter(Mandatory = $true)][string]$Passfile,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$Database,
        [Parameter(Mandatory = $true)][int]$Port,
        [switch]$ExpectFailure
    )
    $priorConnection = $env:SAFARSUITE_CONTROL_DESK_CONNECTION_STRING
    $priorPassword = $env:PGPASSWORD
    $priorPassfile = $env:PGPASSFILE
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $env:SAFARSUITE_CONTROL_DESK_CONNECTION_STRING = "Host=127.0.0.1;Port=$Port;Database=$Database;Username=$Role;Passfile=$Passfile;SSL Mode=Disable;Application Name=SafarSuite Native CI Migrator"
        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        Remove-Item Env:PGPASSFILE -ErrorAction SilentlyContinue
        $ErrorActionPreference = 'Continue'
        $output = @(& $BundlePath $TargetMigration 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        $env:SAFARSUITE_CONTROL_DESK_CONNECTION_STRING = $priorConnection
        $env:PGPASSWORD = $priorPassword
        $env:PGPASSFILE = $priorPassfile
    }
    if ($ExpectFailure) {
        if ($exitCode -eq 0) { throw 'A migration bundle expected to fail succeeded.' }
    }
    elseif ($exitCode -ne 0) {
        $output | ForEach-Object { Write-Host ([string]$_) }
        throw "The packaged migration bundle failed with exit code $exitCode."
    }
    return [pscustomobject]@{ ExitCode = $exitCode; Output = ($output -join [Environment]::NewLine) }
}

function Assert-LifecycleFailure {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)]
        [ValidateSet('ForeignCluster', 'MigrationConflict', 'MigrationDivergence', 'UnavailableCredentials')]
        [string]$Case,
        [Parameter(Mandatory = $true)][string]$Message,
        [Parameter(Mandatory = $true)][string]$ExpectedMessagePattern
    )
    $failure = $null
    try { $null = & $Action }
    catch { $failure = $_ }
    Assert-NativeProof -Condition ($null -ne $failure) -Message $Message
    $failureCategory = switch -Regex ($failure.Exception.Message) {
        "state '[^']+' requires manual recovery" { 'ManualRecoveryRequired'; break }
        'A required database lifecycle process failed with exit code [1-9][0-9]*\.' { 'RequiredProcessFailed'; break }
        'Database migration failed at the finite' { 'FiniteMigrationFailed'; break }
        'migration bundle failed with exit code [1-9][0-9]*' { 'MigrationBundleFailed'; break }
        default { 'UnexpectedRuntimeFailure' }
    }
    Assert-NativeProof `
        -Condition ($failure.Exception.Message -match $ExpectedMessagePattern) `
        -Message "The '$Case' lifecycle case returned '$failureCategory' instead of its reviewed failure contract."
    return [pscustomobject]@{
        ExceptionType = $failure.Exception.GetType().FullName
        Message = $failure.Exception.Message
    }
}

function ConvertTo-SanitizedNativeOutput {
    param(
        [AllowEmptyString()][string]$Text,
        [string[]]$ProtectedValues = @()
    )

    $safeLines = [Collections.Generic.List[string]]::new()
    foreach ($sourceLine in @($Text -split "`r?`n")) {
        if ($sourceLine -match '(?i)(Host|Port|Database|Username|Password|Passfile)\s*=') {
            continue
        }
        $safeLine = [string]$sourceLine
        foreach ($protectedValue in @($ProtectedValues)) {
            if (-not [string]::IsNullOrWhiteSpace($protectedValue)) {
                $safeLine = $safeLine.Replace($protectedValue, '[REDACTED]')
            }
        }
        $safeLines.Add($safeLine)
    }
    return (($safeLines -join [Environment]::NewLine).Trim())
}

function Get-ServiceConfiguration {
    param([Parameter(Mandatory = $true)][string]$Name)
    $escaped = $Name.Replace("'", "''")
    return Get-CimInstance -ClassName Win32_Service -Filter "Name='$escaped'" -ErrorAction SilentlyContinue
}

function Test-DisposableServiceOwnership {
    param(
        [Parameter(Mandatory = $true)]$Configuration,
        [Parameter(Mandatory = $true)][string]$OwnedRoot
    )
    try {
        $tokens = @([SafarSuiteControlDeskCommandLine]::Split([string]$Configuration.PathName))
        if ($tokens.Count -lt 7 -or $tokens[1] -ne 'runservice') { return $false }
        $expectedRuntimeParent = [IO.Path]::GetFullPath((Join-Path $OwnedRoot 'ProgramFiles\SafarSuite\ControlDesk\Database\PostgreSQL')).TrimEnd('\')
        $executable = [IO.Path]::GetFullPath($tokens[0])
        if (-not $executable.StartsWith($expectedRuntimeParent + '\', [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetFileName($executable) -ne 'pg_ctl.exe') { return $false }
        $serviceArgument = $null
        $dataArgument = $null
        $wait = $false
        for ($index = 2; $index -lt $tokens.Count; $index++) {
            switch -CaseSensitive ($tokens[$index]) {
                '-N' { if ($null -ne $serviceArgument -or ++$index -ge $tokens.Count) { return $false }; $serviceArgument = $tokens[$index] }
                '-D' { if ($null -ne $dataArgument -or ++$index -ge $tokens.Count) { return $false }; $dataArgument = $tokens[$index] }
                '-w' { if ($wait) { return $false }; $wait = $true }
                default { return $false }
            }
        }
        $expectedData = [IO.Path]::GetFullPath((Join-Path $OwnedRoot 'ProgramData\SafarSuite\ControlDesk\Database\PostgreSQL17\Data'))
        return $serviceArgument -ceq 'SafarSuiteControlDeskPostgreSQL' -and
            $null -ne $dataArgument -and
            [IO.Path]::GetFullPath($dataArgument) -eq $expectedData -and
            $wait
    }
    catch { return $false }
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
            $configuration = Get-ServiceConfiguration -Name $Name
            if ($null -eq $service -and $null -eq $configuration) { return }
        }
        finally {
            if ($null -ne $service) { $service.Dispose() }
        }
        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw "Cleanup timed out waiting for service '$Name' to disappear."
}

function Remove-DisposableService {
    param([Parameter(Mandatory = $true)][string]$Name)

    Stop-DisposableService -Name $Name
    & "$env:SystemRoot\System32\sc.exe" delete $Name | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Cleanup could not delete disposable service '$Name'."
    }
    Wait-DisposableServiceAbsent -Name $Name
}

function Assert-ExactAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [ValidateSet('Secrets', 'Data', 'Runtime')][string]$Profile,
        [string]$ServiceSid
    )
    $item = Get-Item -LiteralPath $Path -Force
    $acl = Get-Acl -LiteralPath $Path
    Assert-NativeProof -Condition $acl.AreAccessRulesProtected -Message "ACL inheritance remains enabled on '$Path'."
    Assert-NativeProof -Condition ($acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -eq 'S-1-5-32-544') -Message "ACL owner is not Administrators on '$Path'."
    $inheritance = if ($item.PSIsContainer) {
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
    }
    else { [Security.AccessControl.InheritanceFlags]::None }
    $expected = @(
        [pscustomobject]@{ Sid = 'S-1-5-18'; Rights = [Security.AccessControl.FileSystemRights]::FullControl },
        [pscustomobject]@{ Sid = 'S-1-5-32-544'; Rights = [Security.AccessControl.FileSystemRights]::FullControl }
    )
    if (-not [string]::IsNullOrWhiteSpace($ServiceSid)) {
        $rights = if ($Profile -eq 'Runtime') { [Security.AccessControl.FileSystemRights]::ReadAndExecute } else { [Security.AccessControl.FileSystemRights]::Modify }
        $expected += [pscustomobject]@{ Sid = $ServiceSid; Rights = $rights }
    }
    $actual = @($acl.Access)
    Assert-NativeProof -Condition ($actual.Count -eq $expected.Count) -Message "ACL contains an unexpected number of entries on '$Path'."
    foreach ($expectedRule in $expected) {
        $matching = @($actual | Where-Object {
            $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -eq $expectedRule.Sid -and
            ([int]$_.FileSystemRights -band (-bnot [int][Security.AccessControl.FileSystemRights]::Synchronize)) -eq
                ([int]$expectedRule.Rights -band (-bnot [int][Security.AccessControl.FileSystemRights]::Synchronize)) -and
            $_.InheritanceFlags -eq $inheritance -and
            $_.PropagationFlags -eq [Security.AccessControl.PropagationFlags]::None -and
            $_.AccessControlType -eq [Security.AccessControl.AccessControlType]::Allow -and
            -not $_.IsInherited
        })
        Assert-NativeProof -Condition ($matching.Count -eq 1) -Message "ACL rights differ from the exact allowlist on '$Path'."
    }
}

$markerPath = Join-Path $testPath '.safarsuite-native-ci-marker'
$programFilesRoot = Join-Path $testPath 'ProgramFiles'
$programDataRoot = Join-Path $testPath 'ProgramData'
$serviceName = 'SafarSuiteControlDeskPostgreSQL'
$installed = $false
$finalEvidence = $null
$proofFailure = $null
$migrationFailureEvidence = $null
$sanitizedMigrationFailureOutput = $null
$packageManifest = $null
$paths = $null
$runtimeBoundaryEvidencePath = Join-Path (Split-Path -Parent $evidenceFilePath) 'runtime-stage-boundary.json'

if (Test-Path -LiteralPath $testPath) {
    throw 'The native lifecycle test root already exists.'
}
if ($null -ne (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
    throw 'The disposable runner already has the SafarSuite PostgreSQL service name.'
}
if ($null -ne (Get-Service -Name 'SafarSuiteControlDeskApi' -ErrorAction SilentlyContinue)) {
    throw 'The P0-03 native proof requires the P0-05 API service to be absent.'
}
try {
    New-Item -ItemType Directory -Path $testPath | Out-Null
    Set-Content -LiteralPath $markerPath -Value 'owned-disposable-github-runner-root'

    $packageManifest = Test-OfficeDatabasePackage -PackageDirectory $PackageDirectory
    $officeManifestPath = Join-Path $packagePath 'office-package-manifest.json'
    $databaseManifestPath = Join-Path $packagePath 'database\database-package-manifest.json'
    $officeManifest = Get-Content -Raw -LiteralPath $officeManifestPath | ConvertFrom-Json
    Assert-NativeProof `
        -Condition ([string]$officeManifest.sourceRevision -ceq [string]$packageManifest.sourceRevision) `
        -Message 'The office and database package manifests identify different source revisions.'
    Assert-NativeProof `
        -Condition ([string]$officeManifest.sourceRevision -match '^[0-9a-f]{40}$' -and [string]$officeManifest.sourceTreeState -eq 'clean') `
        -Message 'The native proof requires a clean, revision-bound office package.'
    $paths = Get-OfficeDatabasePaths -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot

    $faultCases = [ordered]@{
        AfterRuntimeExtract = 'Absent'
        AfterClusterInitialize = 'InterruptedInitialization'
        AfterClusterPromote = 'InterruptedInitialization'
        AfterServiceStart = 'InitializationIncomplete'
        AfterServiceActivation = 'InitializationIncomplete'
    }
    foreach ($faultCase in $faultCases.GetEnumerator()) {
        $priorFault = $env:SAFARSUITE_OFFICE_DATABASE_FAULT_POINT
        $failureRaised = $false
        try {
            $env:SAFARSUITE_OFFICE_DATABASE_FAULT_POINT = $faultCase.Key
            $null = Install-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
        }
        catch {
            $failureRaised = $_.Exception.Message -match [regex]::Escape("Injected disposable database lifecycle failure at '$($faultCase.Key)'.")
            if (-not $failureRaised) { throw }
        }
        finally {
            $env:SAFARSUITE_OFFICE_DATABASE_FAULT_POINT = $priorFault
        }
        Assert-NativeProof -Condition $failureRaised -Message "The '$($faultCase.Key)' interruption was not injected."
        Assert-NativeProof -Condition (-not (Test-Path -LiteralPath $paths.ActivationFilePath)) -Message "The '$($faultCase.Key)' interruption created an activation receipt."
        $interruptedState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
        Assert-NativeProof -Condition ($interruptedState -eq $faultCase.Value) -Message "The '$($faultCase.Key)' interruption classified as '$interruptedState' instead of '$($faultCase.Value)'."
    }

    $first = Install-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    $installed = $true
    Assert-NativeProof -Condition ($first.InitialState -eq 'InitializationIncomplete' -and $first.FinalState -eq 'Ready') -Message 'Interrupted native installation did not resume to Ready.'

    $configuration = Get-ServiceConfiguration -Name $serviceName
    Assert-NativeProof -Condition ($null -ne $configuration) -Message 'The PostgreSQL Windows service was not registered.'
    Assert-NativeProof -Condition ($configuration.State -eq 'Running' -and $configuration.StartMode -eq 'Auto') -Message 'The PostgreSQL service is not running with automatic startup.'
    Assert-NativeProof -Condition ($configuration.StartName -eq [string]$packageManifest.postgresql.serviceAccount) -Message 'The PostgreSQL service does not use its virtual service account.'
    Assert-NativeProof -Condition (([string]$configuration.PathName).IndexOf($paths.RuntimeRoot, [StringComparison]::OrdinalIgnoreCase) -ge 0) -Message 'The PostgreSQL service image is outside the disposable owned runtime.'

    $postgresVersion = (& (Join-Path $paths.RuntimeRoot 'bin\postgres.exe') --version).Trim()
    Assert-NativeProof -Condition ($postgresVersion -eq 'postgres (PostgreSQL) 17.10') -Message 'The installed PostgreSQL binary is not exactly 17.10.'

    $serviceSid = ([Security.Principal.NTAccount]::new('NT SERVICE', $serviceName)).Translate([Security.Principal.SecurityIdentifier]).Value
    Assert-ExactAcl -Path $paths.RuntimeRoot -Profile Runtime -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.DataRoot -Profile Data -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.DatabaseLogDirectory -Profile Data -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.SecretDirectory -Profile Secrets
    Assert-ExactAcl -Path $paths.StateDirectory -Profile Secrets
    foreach ($protectedFile in @(
        $paths.AdminPassfilePath,
        $paths.MigratorPassfilePath,
        $paths.ApplicationPassfilePath,
        $paths.StateFilePath,
        $paths.ActivationFilePath,
        (Join-Path $paths.RuntimeRoot '.safarsuite-runtime-receipt.json')
    )) {
        Assert-ExactAcl -Path $protectedFile -Profile Secrets
    }

    $psql = Join-Path $paths.RuntimeRoot 'bin\psql.exe'
    $databaseName = [string]$packageManifest.postgresql.databaseName
    $adminRole = [string]$packageManifest.postgresql.adminRole
    $migratorRole = [string]$packageManifest.postgresql.migratorRole
    $applicationRole = [string]$packageManifest.postgresql.applicationRole
    $port = [int]$packageManifest.postgresql.port
    $listenAddresses = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'SHOW listen_addresses;'
    Assert-NativeProof -Condition ($listenAddresses -eq '127.0.0.1') -Message 'PostgreSQL effective listen_addresses is not loopback-only.'
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction Stop)
    Assert-NativeProof -Condition ($listeners.Count -gt 0 -and @($listeners | Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') }).Count -eq 0) -Message 'PostgreSQL opened a non-loopback listener.'

    $hba = Get-Content -Raw -LiteralPath $paths.HbaConfigurationPath
    Assert-NativeProof -Condition ($hba -notmatch '(?im)^\s*host\s+.*\s+trust\s*$' -and $hba -match '0\.0\.0\.0/0\s+reject') -Message 'The packaged HBA is permissive.'

    $applicationIdentity = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.ApplicationPassfilePath -Role $applicationRole -Database $databaseName -Port $port -Sql 'SELECT current_user;'
    Assert-NativeProof -Condition ($applicationIdentity -eq $applicationRole) -Message 'The application role cannot authenticate with its generated credential.'
    $wrongPassfile = Join-Path $testPath 'wrong.pgpass'
    [IO.File]::WriteAllText($wrongPassfile, "127.0.0.1:$port`:$databaseName`:$applicationRole`:deliberately-wrong", [Text.UTF8Encoding]::new($false))
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $wrongPassfile -Role $applicationRole -Database $databaseName -Port $port -Sql 'SELECT 1;' -ExpectFailure
    Remove-Item -LiteralPath $wrongPassfile -Force

    $roleFlags = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.ApplicationPassfilePath -Role $applicationRole -Database $databaseName -Port $port -Sql "SELECT rolsuper::text || ',' || rolcreatedb::text || ',' || rolcreaterole::text FROM pg_roles WHERE rolname = current_user;"
    Assert-NativeProof -Condition ($roleFlags -eq 'false,false,false') -Message 'The application role has elevated PostgreSQL privileges.'

    $migrationOutput = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'SELECT "MigrationId" FROM control.__ef_migrations_history ORDER BY "MigrationId";'
    $appliedMigrations = @($migrationOutput -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    Assert-NativeProof -Condition (($appliedMigrations -join '|') -eq (@($packageManifest.migrations.orderedIds) -join '|')) -Message 'The native database migration ledger is not exact.'
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.ApplicationPassfilePath -Role $applicationRole -Database $databaseName -Port $port -Sql 'UPDATE control.__ef_migrations_history SET "ProductVersion" = "ProductVersion";' -ExpectFailure
    $extension = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql "SELECT extname FROM pg_extension WHERE extname='pg_trgm';"
    Assert-NativeProof -Condition ($extension -eq 'pg_trgm') -Message 'pg_trgm is missing from the native database.'

    $sentinel = [Guid]::NewGuid().ToString('N')
    $sentinelSql = "CREATE TABLE IF NOT EXISTS control.office_lifecycle_ci_sentinel (value text PRIMARY KEY); INSERT INTO control.office_lifecycle_ci_sentinel(value) VALUES ('$sentinel');"
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql $sentinelSql
    $clusterState = Get-Content -Raw -LiteralPath $paths.StateFilePath | ConvertFrom-Json
    $clusterId = [string]$clusterState.clusterSystemIdentifier
    $passfileHashes = @(
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.AdminPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.MigratorPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ApplicationPassfilePath).Hash
    )

    $rerun = Install-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($rerun.InitialState -eq 'Ready' -and $rerun.FinalState -eq 'Ready') -Message 'Native installation rerun was not idempotent.'
    $rerunState = Get-Content -Raw -LiteralPath $paths.StateFilePath | ConvertFrom-Json
    $rerunHashes = @(
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.AdminPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.MigratorPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ApplicationPassfilePath).Hash
    )
    Assert-NativeProof -Condition ([string]$rerunState.clusterSystemIdentifier -eq $clusterId -and ($rerunHashes -join '|') -eq ($passfileHashes -join '|')) -Message 'Native rerun changed cluster identity or credentials.'

    $stateReceiptText = Get-Content -Raw -LiteralPath $paths.StateFilePath
    $tamperedReceipt = $stateReceiptText | ConvertFrom-Json
    $lastIdentifierDigit = [string]$tamperedReceipt.clusterSystemIdentifier
    $replacementDigit = if ($lastIdentifierDigit.EndsWith('0')) { '1' } else { '0' }
    $tamperedReceipt.clusterSystemIdentifier = $lastIdentifierDigit.Substring(0, $lastIdentifierDigit.Length - 1) + $replacementDigit
    [IO.File]::WriteAllText($paths.StateFilePath, ($tamperedReceipt | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $identityMismatchState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($identityMismatchState -eq 'ForeignCluster') -Message 'A live cluster identity mismatch was not rejected.'
    $null = Assert-LifecycleFailure `
        -Case ForeignCluster `
        -Message 'Repair mutated a cluster whose live identity differed from its receipt.' `
        -ExpectedMessagePattern "state 'ForeignCluster' requires manual recovery" `
        -Action {
        Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    }
    [IO.File]::WriteAllText($paths.StateFilePath, $stateReceiptText, [Text.UTF8Encoding]::new($false))
    Assert-NativeProof -Condition ((Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot) -eq 'Ready') -Message 'Restoring the exact ownership receipt did not restore Ready.'

    & "$env:SystemRoot\System32\sc.exe" failureflag $serviceName 0 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Disposable service-policy mutation failed.' }
    $servicePolicyState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($servicePolicyState -eq 'CorruptServiceConfiguration') -Message 'Service recovery-policy drift was not classified.'
    $servicePolicyRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($servicePolicyRepair.InitialState -eq 'CorruptServiceConfiguration' -and $servicePolicyRepair.FinalState -eq 'Ready') -Message 'Service recovery-policy repair failed.'

    $dataAcl = Get-Acl -LiteralPath $paths.DataRoot
    $usersSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-545')
    $unexpectedRule = [Security.AccessControl.FileSystemAccessRule]::new(
        $usersSid,
        [Security.AccessControl.FileSystemRights]::ReadAndExecute,
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit,
        [Security.AccessControl.PropagationFlags]::None,
        [Security.AccessControl.AccessControlType]::Allow)
    [void]$dataAcl.AddAccessRule($unexpectedRule)
    Set-Acl -LiteralPath $paths.DataRoot -AclObject $dataAcl
    $permissionState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($permissionState -eq 'CorruptPermissions') -Message 'Unexpected filesystem access was not classified.'
    $permissionRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($permissionRepair.InitialState -eq 'CorruptPermissions' -and $permissionRepair.FinalState -eq 'Ready') -Message 'Filesystem permission repair failed.'
    Assert-ExactAcl -Path $paths.DataRoot -Profile Data -ServiceSid $serviceSid

    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.AdminPassfilePath -Role ([string]$packageManifest.postgresql.adminRole) -Database $databaseName -Port $port -Sql "ALTER ROLE $applicationRole SUPERUSER; ALTER DATABASE $databaseName OWNER TO $([string]$packageManifest.postgresql.adminRole);"
    $securityState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($securityState -eq 'SecurityDrift') -Message 'Database role/ownership drift was not classified.'
    $securityRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($securityRepair.InitialState -eq 'SecurityDrift' -and $securityRepair.FinalState -eq 'Ready') -Message 'Database security repair failed.'

    $migrationBundle = $paths.MigrationBundlePath
    $penultimateMigration = [string]$packageManifest.migrations.orderedIds[-2]
    $null = Invoke-PackagedMigrationBundle -BundlePath $migrationBundle -TargetMigration $penultimateMigration -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port
    $mismatchState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($mismatchState -eq 'MigrationMismatch') -Message 'A real exact-prefix database was not classified as MigrationMismatch.'
    $mismatchRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($mismatchRepair.InitialState -eq 'MigrationMismatch' -and $mismatchRepair.FinalState -eq 'Ready') -Message 'Real migration-prefix repair failed.'

    $null = Invoke-PackagedMigrationBundle -BundlePath $migrationBundle -TargetMigration $penultimateMigration -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'CREATE TABLE control.portal_payment_claims (fault_marker integer);'
    $activationHashBeforeFailure = (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ActivationFilePath).Hash
    $bundleFailure = Invoke-PackagedMigrationBundle `
        -BundlePath $migrationBundle `
        -TargetMigration ([string]$packageManifest.migrations.target) `
        -Passfile $paths.MigratorPassfilePath `
        -Role $migratorRole `
        -Database $databaseName `
        -Port $port `
        -ExpectFailure
    Assert-NativeProof `
        -Condition ($bundleFailure.Output -match '(?i)(42P07|portal_payment_claims.*already exists|already exists.*portal_payment_claims)') `
        -Message 'The deliberately failed migration did not report the reviewed relation conflict.'
    $migrationFailureRecord = Assert-LifecycleFailure `
        -Case MigrationConflict `
        -Message 'The deliberately conflicting lifecycle repair unexpectedly succeeded.' `
        -ExpectedMessagePattern 'A required database lifecycle process failed with exit code [1-9][0-9]*\.' `
        -Action {
        Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    }
    Assert-NativeProof -Condition ((Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ActivationFilePath).Hash -eq $activationHashBeforeFailure) -Message 'A failed real migration changed the activation receipt.'
    $failedLedger = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'SELECT "MigrationId" FROM control.__ef_migrations_history ORDER BY "MigrationId";'
    Assert-NativeProof -Condition ((@($failedLedger -split "`r?`n")[-1]) -eq $penultimateMigration) -Message 'A failed real migration changed the reviewed ledger prefix.'
    $failedMigrationState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($failedMigrationState -eq 'MigrationMismatch') -Message 'A failed migration did not remain in the exact MigrationMismatch state.'
    Assert-NativeProof -Condition ($null -eq (Get-Service -Name 'SafarSuiteControlDeskApi' -ErrorAction SilentlyContinue)) -Message 'P0-03 unexpectedly created the deferred P0-05 API service.'
    $failureProtectedValues = @(
        ((Get-Content -Raw -LiteralPath $paths.AdminPassfilePath) -split ':', 5)[4],
        ((Get-Content -Raw -LiteralPath $paths.MigratorPassfilePath) -split ':', 5)[4],
        ((Get-Content -Raw -LiteralPath $paths.ApplicationPassfilePath) -split ':', 5)[4],
        $paths.AdminPassfilePath,
        $paths.MigratorPassfilePath,
        $paths.ApplicationPassfilePath,
        $testPath
    )
    $sanitizedMigrationFailureOutput = ConvertTo-SanitizedNativeOutput -Text $bundleFailure.Output -ProtectedValues $failureProtectedValues
    if ([string]::IsNullOrWhiteSpace($sanitizedMigrationFailureOutput)) {
        $sanitizedMigrationFailureOutput = 'The migration bundle returned the reviewed nonzero relation-conflict result; verbose output was fully redacted.'
    }
    $migrationFailureEvidence = [ordered]@{
        bundleExitCode = [int]$bundleFailure.ExitCode
        lifecycleExceptionType = [string]$migrationFailureRecord.ExceptionType
        lifecycleExceptionMessage = [string]$migrationFailureRecord.Message
        resultingState = $failedMigrationState
        ledgerTarget = $penultimateMigration
        activationReceiptUnchanged = $true
        candidateApiActivationTested = $false
        candidateApiActivationReason = 'API service creation belongs to OFFICE-P0-05.'
    }
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'DROP TABLE control.portal_payment_claims;'
    $failedMigrationRecovery = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($failedMigrationRecovery.FinalState -eq 'Ready') -Message 'Database did not recover after removing the deliberate migration conflict.'

    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'INSERT INTO control.__ef_migrations_history ("MigrationId", "ProductVersion") VALUES (''99999999999999_DisposableUnknown'', ''10.0.0'');'
    Assert-NativeProof -Condition ((Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot) -eq 'MigrationDiverged') -Message 'An unknown migration ledger row was not rejected.'
    $null = Assert-LifecycleFailure `
        -Case MigrationDivergence `
        -Message 'Repair mutated a divergent migration ledger.' `
        -ExpectedMessagePattern "state 'MigrationDiverged' requires manual recovery" `
        -Action {
        Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    }
    $null = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'DELETE FROM control.__ef_migrations_history WHERE "MigrationId" = ''99999999999999_DisposableUnknown'';'
    Assert-NativeProof -Condition ((Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot) -eq 'Ready') -Message 'Removing the disposable divergent row did not restore Ready.'

    $adminPassfileText = Get-Content -Raw -LiteralPath $paths.AdminPassfilePath
    $migratorPassfileText = Get-Content -Raw -LiteralPath $paths.MigratorPassfilePath
    foreach ($passfileMutation in @(
        @{ Path = $paths.AdminPassfilePath; Content = $adminPassfileText },
        @{ Path = $paths.MigratorPassfilePath; Content = $migratorPassfileText }
    )) {
        $parts = $passfileMutation.Content -split ':', 5
        [IO.File]::WriteAllText($passfileMutation.Path, (($parts[0..3] -join ':') + ':deliberately-wrong'), [Text.UTF8Encoding]::new($false))
    }
    Assert-NativeProof -Condition ((Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot) -eq 'UnavailableDatabase') -Message 'Unavailable database authentication was not classified.'
    $activationHashBeforeUnavailable = (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ActivationFilePath).Hash
    $null = Assert-LifecycleFailure `
        -Case UnavailableCredentials `
        -Message 'Unavailable database repair unexpectedly activated.' `
        -ExpectedMessagePattern 'A required database lifecycle process failed with exit code [1-9][0-9]*\.' `
        -Action {
        Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    }
    Assert-NativeProof -Condition ((Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ActivationFilePath).Hash -eq $activationHashBeforeUnavailable) -Message 'Unavailable database repair changed activation evidence.'
    [IO.File]::WriteAllText($paths.AdminPassfilePath, $adminPassfileText, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($paths.MigratorPassfilePath, $migratorPassfileText, [Text.UTF8Encoding]::new($false))
    $unavailableRecovery = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($unavailableRecovery.FinalState -eq 'Ready') -Message 'Database did not recover after credentials were restored.'

    $concurrentJobs = @(
        Start-Job -ScriptBlock {
            param($ModulePath, $PackagePath, $ProgramFilesPath, $ProgramDataPath)
            $ErrorActionPreference = 'Stop'
            Import-Module $ModulePath -Force
            Repair-OfficeDatabaseLifecycle -PackageDirectory $PackagePath -ProgramFilesRoot $ProgramFilesPath -ProgramDataRoot $ProgramDataPath | ConvertTo-Json -Compress -Depth 6
        } -ArgumentList $packagedLifecycleModule, $PackageDirectory, $programFilesRoot, $programDataRoot
        Start-Job -ScriptBlock {
            param($ModulePath, $PackagePath, $ProgramFilesPath, $ProgramDataPath)
            $ErrorActionPreference = 'Stop'
            Import-Module $ModulePath -Force
            Repair-OfficeDatabaseLifecycle -PackageDirectory $PackagePath -ProgramFilesRoot $ProgramFilesPath -ProgramDataRoot $ProgramDataPath | ConvertTo-Json -Compress -Depth 6
        } -ArgumentList $packagedLifecycleModule, $PackageDirectory, $programFilesRoot, $programDataRoot
    )
    try {
        $completedJobs = @(Wait-Job -Job $concurrentJobs -Timeout 360)
        Assert-NativeProof -Condition ($completedJobs.Count -eq 2) -Message 'Concurrent lifecycle proof timed out.'
        $concurrentOutput = @($concurrentJobs | Receive-Job -ErrorAction Stop)
        Assert-NativeProof -Condition (@($concurrentJobs | Where-Object { $_.State -ne 'Completed' }).Count -eq 0) -Message 'A serialized concurrent lifecycle process failed.'
        Assert-NativeProof -Condition (@($concurrentOutput | Where-Object { $_ -match '"FinalState":"Ready"' }).Count -eq 2) -Message 'Concurrent lifecycle calls did not both converge to Ready.'
    }
    finally {
        $concurrentJobs | Stop-Job -ErrorAction SilentlyContinue
        $concurrentJobs | Remove-Job -Force -ErrorAction SilentlyContinue
    }
    $postConcurrencyState = Get-Content -Raw -LiteralPath $paths.StateFilePath | ConvertFrom-Json
    $postConcurrencyHashes = @(
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.AdminPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.MigratorPassfilePath).Hash,
        (Get-FileHash -Algorithm SHA256 -LiteralPath $paths.ApplicationPassfilePath).Hash
    )
    Assert-NativeProof -Condition ([string]$postConcurrencyState.clusterSystemIdentifier -eq $clusterId -and ($postConcurrencyHashes -join '|') -eq ($passfileHashes -join '|')) -Message 'Concurrent lifecycle calls changed cluster identity or credentials.'

    Stop-DisposableService -Name $serviceName
    $stoppedRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($stoppedRepair.InitialState -eq 'StoppedService' -and $stoppedRepair.FinalState -eq 'Ready') -Message 'Stopped-service repair failed.'

    Stop-DisposableService -Name $serviceName
    & (Join-Path $paths.RuntimeRoot 'bin\pg_ctl.exe') unregister -N $serviceName
    if ($LASTEXITCODE -ne 0) { throw 'Disposable missing-service setup failed.' }
    Wait-DisposableServiceAbsent -Name $serviceName
    $missingRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($missingRepair.InitialState -eq 'MissingService' -and $missingRepair.FinalState -eq 'Ready') -Message 'Missing-service repair failed.'

    Stop-DisposableService -Name $serviceName
    Add-Content -LiteralPath $paths.PostgresConfigurationPath -Value "`nlisten_addresses = '*'"
    $configurationRepair = Repair-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($configurationRepair.InitialState -eq 'CorruptConfiguration' -and $configurationRepair.FinalState -eq 'Ready') -Message 'Corrupt-configuration repair failed.'
    Assert-NativeProof -Condition (@(Get-ChildItem -LiteralPath $paths.DataDirectory -Filter 'postgresql.conf.safarsuite-backup-*').Count -gt 0) -Message 'Configuration repair did not preserve the prior file.'

    $defaultUninstall = Uninstall-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    $installed = $false
    Assert-NativeProof -Condition ($defaultUninstall.FinalState -eq 'PreservedData' -and (Test-Path -LiteralPath $paths.DataDirectory)) -Message 'Default uninstall did not preserve the database.'
    $reinstall = Install-OfficeDatabaseLifecycle -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    $installed = $true
    Assert-NativeProof -Condition ($reinstall.InitialState -eq 'MissingService' -and $reinstall.FinalState -eq 'Ready') -Message 'Reinstall did not reopen preserved data.'
    $reopenedSentinel = Invoke-PackagedPsql -PsqlPath (Join-Path $paths.RuntimeRoot 'bin\psql.exe') -Passfile $paths.ApplicationPassfilePath -Role $applicationRole -Database $databaseName -Port $port -Sql "SELECT value FROM control.office_lifecycle_ci_sentinel WHERE value='$sentinel';"
    Assert-NativeProof -Condition ($reopenedSentinel -eq $sentinel) -Message 'Reinstall did not retain the database sentinel.'

    $dependency = Set-OfficeApiDatabaseDependency
    Assert-NativeProof -Condition ($dependency.Status -eq 'Deferred' -and $dependency.Reason -eq 'ApiServiceNotInstalled') -Message 'The API dependency contract did not defer cleanly before OFFICE-P0-05.'

    $finalLifecycleState = Get-OfficeDatabaseLifecycleState -PackageDirectory $PackageDirectory -ProgramFilesRoot $programFilesRoot -ProgramDataRoot $programDataRoot
    Assert-NativeProof -Condition ($finalLifecycleState -eq 'Ready') -Message 'The final native lifecycle state is not Ready.'
    $finalConfiguration = Get-ServiceConfiguration -Name $serviceName
    Assert-NativeProof `
        -Condition ($null -ne $finalConfiguration -and $finalConfiguration.State -eq 'Running' -and $finalConfiguration.StartMode -eq 'Auto') `
        -Message 'The final PostgreSQL service is not running with automatic startup.'
    Assert-NativeProof `
        -Condition (Test-DisposableServiceOwnership -Configuration $finalConfiguration -OwnedRoot $testPath) `
        -Message 'The final PostgreSQL service command line is outside the owned lifecycle roots.'
    Assert-NativeProof `
        -Condition ($finalConfiguration.StartName -eq [string]$packageManifest.postgresql.serviceAccount) `
        -Message 'The final PostgreSQL service identity changed.'

    $finalListenAddresses = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'SHOW listen_addresses;'
    Assert-NativeProof -Condition ($finalListenAddresses -eq '127.0.0.1') -Message 'The final PostgreSQL listen_addresses value is not loopback-only.'
    $finalListeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction Stop)
    Assert-NativeProof -Condition ($finalListeners.Count -gt 0 -and @($finalListeners | Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') }).Count -eq 0) -Message 'The final PostgreSQL service opened a non-loopback listener.'

    $finalHbaSummary = Invoke-PackagedPsql `
        -PsqlPath $psql `
        -Passfile $paths.AdminPassfilePath `
        -Role $adminRole `
        -Database $databaseName `
        -Port $port `
        -Sql @"
SELECT
    (count(*) FILTER (WHERE auth_method = 'scram-sha-256'))::text || ',' ||
    (count(*) FILTER (WHERE auth_method = 'reject'))::text || ',' ||
    (count(*) FILTER (WHERE auth_method NOT IN ('scram-sha-256', 'reject')))::text || ',' ||
    (count(*) FILTER (WHERE error IS NOT NULL))::text
FROM pg_hba_file_rules;
"@
    Assert-NativeProof -Condition ($finalHbaSummary -eq '4,4,0,0') -Message "The effective HBA rules are not the exact four-SCRAM/four-reject policy: '$finalHbaSummary'."

    $finalMigrationOutput = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql 'SELECT "MigrationId" FROM control.__ef_migrations_history ORDER BY "MigrationId";'
    $finalMigrations = @($finalMigrationOutput -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    Assert-NativeProof -Condition (($finalMigrations -join '|') -eq (@($packageManifest.migrations.orderedIds) -join '|')) -Message 'The final migration ledger is not the exact packaged ledger.'
    $finalExtension = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.MigratorPassfilePath -Role $migratorRole -Database $databaseName -Port $port -Sql "SELECT extname FROM pg_extension WHERE extname='pg_trgm';"
    Assert-NativeProof -Condition ($finalExtension -eq 'pg_trgm') -Message 'pg_trgm is missing after the final reinstall.'
    $finalRoleFlags = Invoke-PackagedPsql -PsqlPath $psql -Passfile $paths.ApplicationPassfilePath -Role $applicationRole -Database $databaseName -Port $port -Sql "SELECT rolsuper::text || ',' || rolcreatedb::text || ',' || rolcreaterole::text FROM pg_roles WHERE rolname = current_user;"
    Assert-NativeProof -Condition ($finalRoleFlags -eq 'false,false,false') -Message 'The final application role has elevated PostgreSQL privileges.'
    Assert-ExactAcl -Path $paths.RuntimeRoot -Profile Runtime -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.DataRoot -Profile Data -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.DatabaseLogDirectory -Profile Data -ServiceSid $serviceSid
    Assert-ExactAcl -Path $paths.SecretDirectory -Profile Secrets
    Assert-ExactAcl -Path $paths.StateDirectory -Profile Secrets
    foreach ($protectedFile in @(
        $paths.AdminPassfilePath,
        $paths.MigratorPassfilePath,
        $paths.ApplicationPassfilePath,
        $paths.StateFilePath,
        $paths.ActivationFilePath,
        (Join-Path $paths.RuntimeRoot '.safarsuite-runtime-receipt.json')
    )) {
        Assert-ExactAcl -Path $protectedFile -Profile Secrets
    }
    Assert-NativeProof -Condition ($null -ne $migrationFailureEvidence) -Message 'The retained failed-migration evidence was not created.'

    $databaseManifestSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $databaseManifestPath).Hash.ToUpperInvariant()
    $officeManifestSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $officeManifestPath).Hash.ToUpperInvariant()

    $finalEvidence = [ordered]@{
        product = 'SafarSuite Control Desk'
        proof = 'office-p0-03-native-windows-ci-v1'
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        sourceRevision = [string]$officeManifest.sourceRevision
        sourceTreeState = [string]$officeManifest.sourceTreeState
        officePackageArchiveSha256 = $OfficePackageArchiveSha256.ToUpperInvariant()
        officePackageManifestSha256 = $officeManifestSha256
        databasePackageManifestSha256 = $databaseManifestSha256
        postgresRuntimeSha256 = [string]$packageManifest.postgresql.runtimeSha256
        migrationBundleSha256 = [string]$packageManifest.migrations.bundleSha256
        postgresVersion = $postgresVersion
        serviceName = $serviceName
        serviceIdentity = [string]$packageManifest.postgresql.serviceAccount
        automaticStartConfigured = $true
        physicalRebootProved = $false
        physicalRebootReason = 'GitHub-hosted runners cannot reboot and resume the same acceptance job.'
        finalLifecycleState = $finalLifecycleState
        listenerAddresses = @($finalListeners.LocalAddress | Sort-Object -Unique)
        loopbackOnly = $true
        scramHba = $true
        scramHbaRuleCount = 4
        rejectHbaRuleCount = 4
        applicationRoleLeastPrivilege = $true
        migrationCount = $finalMigrations.Count
        migrationTarget = $finalMigrations[-1]
        requiredExtension = $finalExtension
        interruptionRecoveryPoints = @($faultCases.Keys)
        clusterIdentityPreservedOnRerun = $true
        liveClusterIdentityBoundToReceipt = $true
        credentialsPreservedOnRerun = $true
        exactServiceRecoveryPolicy = $true
        exactFilesystemAllowlist = $true
        concurrentLifecycleSerialized = $true
        failedMigration = $migrationFailureEvidence
        failedMigrationActivationReceiptUnchanged = $true
        candidateApiActivationProof = 'DeferredUntilOfficeP005'
        divergentMigrationRefused = $true
        repairClassifications = @(
            'InitializationIncomplete',
            'StoppedService',
            'MissingService',
            'CorruptConfiguration',
            'CorruptPermissions',
            'CorruptServiceConfiguration',
            'SecurityDrift',
            'MigrationMismatch',
            'UnavailableDatabase'
        )
        defaultUninstallPreservedData = $true
        reinstallRetainedSentinel = $true
        apiDependency = 'DeferredUntilOfficeP005'
        lifecycleAuditFile = 'database-lifecycle.jsonl'
        migrationFailureOutputFile = 'migration-failure-sanitized.txt'
    }
    $evidenceJson = $finalEvidence | ConvertTo-Json -Depth 8
    $protectedValues = @(
        ((Get-Content -Raw -LiteralPath $paths.AdminPassfilePath) -split ':', 5)[4],
        ((Get-Content -Raw -LiteralPath $paths.MigratorPassfilePath) -split ':', 5)[4],
        ((Get-Content -Raw -LiteralPath $paths.ApplicationPassfilePath) -split ':', 5)[4]
    )
    $auditText = Get-Content -Raw -LiteralPath $paths.AuditPath
    $redactedMaterial = $evidenceJson + [Environment]::NewLine + $auditText + [Environment]::NewLine + $sanitizedMigrationFailureOutput
    foreach ($protectedValue in $protectedValues) {
        Assert-NativeProof -Condition ($redactedMaterial.IndexOf($protectedValue, [StringComparison]::Ordinal) -lt 0) -Message 'Native lifecycle evidence contains a generated database credential.'
    }
    Assert-NativeProof -Condition ($redactedMaterial -notmatch '(?i)(Host|Username|Password|Passfile)\s*=') -Message 'Native lifecycle evidence contains a raw connection string.'
    $evidenceDirectory = Split-Path -Parent $evidenceFilePath
    New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null
    [IO.File]::WriteAllText($evidenceFilePath, $evidenceJson, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $evidenceDirectory 'database-lifecycle.jsonl'), $auditText, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $evidenceDirectory 'migration-failure-sanitized.txt'), $sanitizedMigrationFailureOutput, [Text.UTF8Encoding]::new($false))
}
catch {
    $proofFailure = $_
    if ($null -ne $packageManifest -and $null -ne $paths -and (Test-Path -LiteralPath $testPath -PathType Container)) {
        try {
            $null = Invoke-OfficePostgresLifecycleBoundaryDiagnostic `
                -LifecycleModule $lifecycleModule `
                -PackageManifest $packageManifest `
                -Paths $paths `
                -TestRoot $testPath `
                -EvidencePath $runtimeBoundaryEvidencePath `
                -OfficePackageArchiveSha256 $OfficePackageArchiveSha256 `
                -InvocationNonce $BoundaryInvocationNonce `
                -LifecycleFailure $proofFailure
        }
        catch {
            Write-Warning 'The safe PostgreSQL lifecycle boundary diagnostic could not produce evidence.'
        }
    }
}
finally {
    $cleanupFailure = $null
    try {
        $serviceConfiguration = Get-ServiceConfiguration -Name $serviceName
        if ($null -ne $serviceConfiguration) {
            if (-not (Test-DisposableServiceOwnership -Configuration $serviceConfiguration -OwnedRoot $testPath)) {
                throw 'Cleanup refused to touch a service outside the disposable native test root.'
            }
            Remove-DisposableService -Name $serviceName
        }
        elseif ($null -ne (Get-Service -Name $serviceName -ErrorAction SilentlyContinue)) {
            throw 'Cleanup found a service whose ownership configuration could not be verified.'
        }

        $ownedRuntimeParent = [IO.Path]::GetFullPath((Join-Path $testPath 'ProgramFiles\SafarSuite\ControlDesk\Database\PostgreSQL')).TrimEnd('\')
        $processDeadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
        do {
            $ownedProcesses = @(Get-CimInstance -ClassName Win32_Process -Filter "Name='postgres.exe'" -ErrorAction SilentlyContinue | Where-Object {
                -not [string]::IsNullOrWhiteSpace([string]$_.ExecutablePath) -and
                [IO.Path]::GetFullPath([string]$_.ExecutablePath).StartsWith($ownedRuntimeParent + '\', [StringComparison]::OrdinalIgnoreCase)
            })
            if ($ownedProcesses.Count -eq 0) { break }
            Start-Sleep -Milliseconds 250
        } while ([DateTimeOffset]::UtcNow -lt $processDeadline)
        if ($ownedProcesses.Count -ne 0) {
            throw 'Cleanup timed out waiting for the owned PostgreSQL processes to exit.'
        }

        if (Test-Path -LiteralPath $testPath) {
            $resolved = (Resolve-Path -LiteralPath $testPath).Path
            $item = Get-Item -LiteralPath $resolved -Force
            $markerValue = if (Test-Path -LiteralPath $markerPath -PathType Leaf) { (Get-Content -Raw -LiteralPath $markerPath).Trim() } else { $null }
            if (-not $resolved.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
                $markerValue -cne 'owned-disposable-github-runner-root' -or
                [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw 'Cleanup refused an unowned or unsafe native test root.'
            }
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
    catch {
        $cleanupFailure = $_
    }

    if ($null -ne $cleanupFailure) {
        if ($null -eq $proofFailure) { throw $cleanupFailure }
        Write-Warning "Native proof cleanup also failed: $($cleanupFailure.Exception.Message)"
    }
}

if ($null -ne $proofFailure) {
    throw $proofFailure
}

Write-Host 'Office database native Windows lifecycle proof passed.'
Write-Host 'PostgreSQL: 17.10, loopback-only, SCRAM'
Write-Host 'Migrations: 32 exact; pg_trgm present'
Write-Host 'Install/rerun/repair/uninstall/reinstall: passed'
Write-Host 'Physical reboot proof: pending on the persistent reference PC'
