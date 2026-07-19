#Requires -Version 5.1

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
Import-Module (Join-Path $PSScriptRoot 'OfficeDatabaseLifecycle.psm1') -Force

function Assert-OfficeTest {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )
    if (-not $Condition) { throw $Message }
}

function Assert-OfficeThrows {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$Pattern
    )
    $thrown = $false
    try { $null = & $Action }
    catch {
        $thrown = $true
        if ($_.Exception.Message -notmatch $Pattern) {
            throw "Expected failure '$Pattern', received '$($_.Exception.Message)'."
        }
    }
    if (-not $thrown) { throw "Expected failure '$Pattern' was not raised." }
}

function New-FakeLifecycle {
    param(
        [Parameter(Mandatory = $true)][string]$InitialState,
        [switch]$FailMigration
    )

    $state = [pscustomobject]@{ Value = $InitialState }
    $calls = [Collections.Generic.List[string]]::new()
    $clusterId = [Guid]::NewGuid().ToString('N')
    $sentinel = [Guid]::NewGuid().ToString('N')
    $add = { param($name) [void]$calls.Add($name) }.GetNewClosure()
    $adapter = @{
        Inspect = { param($ctx) & $add 'Inspect'; return $state.Value }
        StageRuntime = { param($ctx) & $add 'StageRuntime' }
        RecoverInitialization = { param($ctx) & $add 'RecoverInitialization'; $state.Value = 'Absent'; $ctx.Secrets = $null }
        InitializeCluster = {
            param($ctx)
            & $add 'InitializeCluster'
            $ctx.Secrets = [pscustomobject]@{ Admin = 'admin-secret'; Migrator = 'migrator-secret'; Application = 'application-secret' }
            $state.Value = 'MissingService'
        }
        Configure = { param($ctx) & $add 'Configure' }
        ConfigurePermissions = { param($ctx) & $add 'ConfigurePermissions'; $state.Value = 'MigrationMismatch' }
        ConfigureService = { param($ctx) & $add 'ConfigureService' }
        RegisterService = { param($ctx) & $add 'RegisterService'; $state.Value = 'StoppedService' }
        EnsureStarted = { param($ctx) & $add 'EnsureStarted'; $state.Value = 'MigrationMismatch' }
        Provision = { param($ctx) & $add 'Provision' }
        Migrate = {
            param($ctx)
            & $add 'Migrate'
            if ($FailMigration) { throw 'Injected migration failure' }
            $state.Value = 'Ready'
        }
        Verify = {
            param($ctx)
            & $add 'Verify'
            return [pscustomobject]@{
                MigrationCount = 32
                MigrationTarget = '20260713220254_AddPortalPaymentBoundary'
                RequiredExtension = 'pg_trgm'
                ListenerAddresses = @('127.0.0.1')
            }
        }
        Activate = { param($ctx, $verification) & $add 'Activate' }
        Restart = { param($ctx) & $add 'Restart'; $state.Value = 'MigrationMismatch' }
        Unregister = { param($ctx) & $add 'Unregister' }
        RemoveRuntime = { param($ctx) & $add 'RemoveRuntime'; $state.Value = 'PreservedData' }
    }
    foreach ($operation in @($adapter.Keys)) {
        $adapter[$operation] = $adapter[$operation].GetNewClosure()
    }
    return [pscustomobject]@{
        Adapter = $adapter
        State = $state
        Calls = $calls
        ClusterId = $clusterId
        Sentinel = $sentinel
    }
}

function New-TestArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][Collections.IDictionary]$Entries
    )
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($name in @($Entries.Keys | Sort-Object)) {
            $entry = $archive.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]::Parse('2026-07-19T00:00:00Z')
            $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
            try { $writer.Write([string]$Entries[$name]) }
            finally { $writer.Dispose() }
        }
    }
    finally {
        $archive.Dispose()
    }
}

$lifecycleModule = Get-Module OfficeDatabaseLifecycle
$windowsPowerShell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$nativeEnvironmentProof = & $lifecycleModule {
    param($Executable)
    Invoke-OfficeNativeCommand `
        -FilePath $Executable `
        -Arguments @('-NoProfile', '-Command', '[Environment]::GetEnvironmentVariable(''SAFARSUITE_HERMETIC_ENV'', ''Process'')') `
        -Environment @{ SAFARSUITE_HERMETIC_ENV = 'environment-propagated' }
} $windowsPowerShell
Assert-OfficeTest -Condition ($nativeEnvironmentProof.StandardOutput.Trim() -eq 'environment-propagated') -Message 'Windows PowerShell native environment propagation failed.'
Assert-OfficeTest -Condition ([Environment]::GetEnvironmentVariable('SAFARSUITE_HERMETIC_ENV', 'Process') -ne 'environment-propagated') -Message 'Native environment propagation leaked into the lifecycle host.'
$verboseNativeProof = & $lifecycleModule {
    param($Executable)
    Invoke-OfficeNativeCommand `
        -FilePath $Executable `
        -Arguments @('-NoProfile', '-Command', '1..12000 | ForEach-Object { [Console]::Out.WriteLine("out-$_"); [Console]::Error.WriteLine("err-$_") }') `
        -TimeoutSeconds 30
} $windowsPowerShell
Assert-OfficeTest -Condition ($verboseNativeProof.StandardOutput -match 'out-12000' -and $verboseNativeProof.StandardError -match 'err-12000') -Message 'Native process output was not drained without deadlock.'
$nativeFailureMessage = & $lifecycleModule {
    param($Executable)
    try {
        Invoke-OfficeNativeCommand `
            -FilePath $Executable `
            -Arguments @('-NoProfile', '-Command', '$null = ''SAFARSUITE_SECRET_ARGUMENT_MARKER''; [Environment]::Exit(-1073741515)') | Out-Null
        throw 'The controlled native failure unexpectedly succeeded.'
    }
    catch {
        return $_.Exception.Message
    }
} $windowsPowerShell
Assert-OfficeTest `
    -Condition ($nativeFailureMessage -ceq "A required database lifecycle process failed with exit code -1073741515. Executable 'powershell.exe'; hexadecimal exit code 0xC0000135.") `
    -Message 'Native process failure evidence did not retain only the executable identity and signed/hexadecimal exit code.'
$serviceRecoveryProof = & $lifecycleModule {
    $bytes = New-Object byte[] 44
    [Buffer]::BlockCopy([BitConverter]::GetBytes([uint32]86400), 0, $bytes, 0, 4)
    [Buffer]::BlockCopy([BitConverter]::GetBytes([uint32]3), 0, $bytes, 12, 4)
    [Buffer]::BlockCopy([BitConverter]::GetBytes([uint32]20), 0, $bytes, 16, 4)
    foreach ($entry in @(
        @{ Offset = 20; Delay = 5000 },
        @{ Offset = 28; Delay = 15000 },
        @{ Offset = 36; Delay = 60000 }
    )) {
        [Buffer]::BlockCopy([BitConverter]::GetBytes([uint32]1), 0, $bytes, $entry.Offset, 4)
        [Buffer]::BlockCopy([BitConverter]::GetBytes([uint32]$entry.Delay), 0, $bytes, $entry.Offset + 4, 4)
    }
    $valid = Test-OfficeServiceFailureActions -Value $bytes
    $bytes[40] = 0
    $invalid = Test-OfficeServiceFailureActions -Value $bytes
    return [pscustomobject]@{ Valid = $valid; Invalid = $invalid }
}
Assert-OfficeTest -Condition ($serviceRecoveryProof.Valid -and -not $serviceRecoveryProof.Invalid) -Message 'Exact Windows service recovery-action parsing failed.'

$psqlFailureClassifications = & $lifecycleModule {
    return @(
        Get-OfficePsqlFailureClassification -StandardError 'connection failed: fe_sendauth: no password supplied'
        Get-OfficePsqlFailureClassification -StandardError 'password authentication failed for user redacted'
        Get-OfficePsqlFailureClassification -StandardError 'no pg_hba.conf entry for host redacted'
        Get-OfficePsqlFailureClassification -StandardError 'could not connect to server'
        Get-OfficePsqlFailureClassification -StandardError 'unexpected finite-safe test input'
    )
}
Assert-OfficeTest `
    -Condition (($psqlFailureClassifications -join '|') -eq 'PasswordNotSupplied|PasswordAuthenticationFailed|HbaRejected|ConnectionUnavailable|Unclassified') `
    -Message 'Secret-safe psql failure classification changed unexpectedly.'

$serviceConfigArguments = & $lifecycleModule {
    $context = [pscustomobject]@{
        Distribution = [pscustomobject]@{
            serviceName = 'SafarSuiteControlDeskPostgreSQL'
            serviceAccount = 'NT SERVICE\SafarSuiteControlDeskPostgreSQL'
            serviceDisplayName = 'SafarSuite Control Desk PostgreSQL 17'
        }
    }
    return @(Get-OfficePostgresServiceConfigArguments -Context $context -Mode Pending)
}
Assert-OfficeTest `
    -Condition (($serviceConfigArguments -join '|') -eq
        'config|SafarSuiteControlDeskPostgreSQL|start=|demand|obj=|NT SERVICE\SafarSuiteControlDeskPostgreSQL|depend=|/|DisplayName=|SafarSuite Control Desk PostgreSQL 17') `
    -Message 'The PostgreSQL virtual-account service configuration arguments changed unexpectedly.'
Assert-OfficeTest `
    -Condition (-not ($serviceConfigArguments -contains 'password=')) `
    -Message 'Virtual service account configuration supplied a password instead of the required null password pointer.'
$serviceDependencyShapes = & $lifecycleModule {
    $missing = @(Get-OfficeServiceDependenciesFromRegistry -Registry ([pscustomobject]@{}))
    $empty = @(Get-OfficeServiceDependenciesFromRegistry -Registry ([pscustomobject]@{ DependOnService = '' }))
    $scalar = @(Get-OfficeServiceDependenciesFromRegistry -Registry ([pscustomobject]@{ DependOnService = 'RpcSs' }))
    return [pscustomobject]@{
        MissingCount = $missing.Count
        EmptyCount = $empty.Count
        ScalarCount = $scalar.Count
        ScalarValue = if ($scalar.Count -eq 1) { [string]$scalar[0] } else { '' }
    }
}
Assert-OfficeTest `
    -Condition ($serviceDependencyShapes.MissingCount -eq 0 -and $serviceDependencyShapes.EmptyCount -eq 0) `
    -Message 'Missing or empty service dependencies did not remain an empty array under strict mode.'
Assert-OfficeTest `
    -Condition ($serviceDependencyShapes.ScalarCount -eq 1 -and $serviceDependencyShapes.ScalarValue -eq 'RpcSs') `
    -Message 'A scalar service dependency did not remain a one-item array under strict mode.'

$initializationAclRights = & $lifecycleModule {
    $runtime = Get-OfficeInitializationAclRights -Profile Runtime
    $data = Get-OfficeInitializationAclRights -Profile Data
    $secrets = Get-OfficeInitializationAclRights -Profile Secrets
    return [pscustomobject]@{
        Runtime = [int]$runtime
        Data = [int]$data
        Secrets = [int]$secrets
        NormalizedRuntime = [int](Get-OfficeNormalizedAllowAclRights -Rights $runtime)
        NormalizedData = [int](Get-OfficeNormalizedAllowAclRights -Rights $data)
        NormalizedSecrets = [int](Get-OfficeNormalizedAllowAclRights -Rights $secrets)
    }
}
Assert-OfficeTest `
    -Condition ($initializationAclRights.Runtime -eq [int][Security.AccessControl.FileSystemRights]::ReadAndExecute) `
    -Message 'The temporary initdb runtime bridge grants more or less than read/execute.'
Assert-OfficeTest `
    -Condition ($initializationAclRights.Data -eq [int][Security.AccessControl.FileSystemRights]::Modify) `
    -Message 'The temporary initdb staging-data bridge grants more or less than modify.'
Assert-OfficeTest `
    -Condition ($initializationAclRights.Secrets -eq [int][Security.AccessControl.FileSystemRights]::Read) `
    -Message 'The temporary initdb bootstrap bridge grants more or less than read.'
$synchronize = [int][Security.AccessControl.FileSystemRights]::Synchronize
Assert-OfficeTest `
    -Condition ($initializationAclRights.NormalizedRuntime -eq ($initializationAclRights.Runtime -bor $synchronize) -and
        $initializationAclRights.NormalizedData -eq ($initializationAclRights.Data -bor $synchronize) -and
        $initializationAclRights.NormalizedSecrets -eq ($initializationAclRights.Secrets -bor $synchronize)) `
    -Message 'Windows allow-rule Synchronize normalization is not represented in exact ACL verification.'

$expectedMigrations = 1..32 | ForEach-Object { 'migration-{0:d2}' -f $_ }
Assert-OfficeTest `
    -Condition ((Compare-OfficeMigrationLedger -Expected $expectedMigrations -Applied @()) -eq 'Prefix') `
    -Message 'An empty database must be an accepted exact prefix.'
Assert-OfficeTest `
    -Condition ((Compare-OfficeMigrationLedger -Expected $expectedMigrations -Applied @($expectedMigrations[0..14])) -eq 'Prefix') `
    -Message 'An ordered partial ledger must be accepted as a prefix.'
Assert-OfficeTest `
    -Condition ((Compare-OfficeMigrationLedger -Expected $expectedMigrations -Applied $expectedMigrations) -eq 'Exact') `
    -Message 'The complete ordered ledger must be exact.'
Assert-OfficeTest `
    -Condition ((Compare-OfficeMigrationLedger -Expected $expectedMigrations -Applied @('migration-02')) -eq 'Diverged') `
    -Message 'An out-of-order ledger must diverge.'
Assert-OfficeTest `
    -Condition ((Compare-OfficeMigrationLedger -Expected $expectedMigrations -Applied @($expectedMigrations + 'unknown')) -eq 'Diverged') `
    -Message 'An ahead/unknown ledger must diverge.'

$context = [pscustomobject]@{ Secrets = $null }
$fresh = New-FakeLifecycle -InitialState 'Absent'
$freshResult = Invoke-OfficeDatabaseLifecycleCore -Action Install -Adapter $fresh.Adapter -Context $context
Assert-OfficeTest -Condition ($freshResult.FinalState -eq 'Ready') -Message 'Fresh install did not converge to Ready.'
$expectedFreshCalls = @('Inspect', 'StageRuntime', 'InitializeCluster', 'Configure', 'RegisterService', 'EnsureStarted', 'Provision', 'Migrate', 'Verify', 'Activate', 'Inspect')
Assert-OfficeTest -Condition (($fresh.Calls -join '|') -eq ($expectedFreshCalls -join '|')) -Message 'Fresh install operations ran out of order.'
$originalClusterId = $fresh.ClusterId
$originalSentinel = $fresh.Sentinel
$fresh.Calls.Clear()
$rerunContext = [pscustomobject]@{ Secrets = $null }
$rerun = Invoke-OfficeDatabaseLifecycleCore -Action Install -Adapter $fresh.Adapter -Context $rerunContext
Assert-OfficeTest -Condition ($rerun.FinalState -eq 'Ready') -Message 'Install rerun did not remain Ready.'
Assert-OfficeTest -Condition (($fresh.Calls -join '|') -eq 'Inspect|Migrate|Verify|Activate|Inspect') -Message 'Install rerun performed destructive initialization work.'
Assert-OfficeTest -Condition ($fresh.ClusterId -eq $originalClusterId -and $fresh.Sentinel -eq $originalSentinel) -Message 'Install rerun changed preserved database identity.'

$repairCases = [ordered]@{
    MissingPrerequisite = @('Inspect', 'StageRuntime', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    MissingService = @('Inspect', 'StageRuntime', 'Inspect', 'Configure', 'RegisterService', 'EnsureStarted', 'Provision', 'Migrate', 'Verify', 'Activate', 'Inspect')
    StoppedService = @('Inspect', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    CorruptConfiguration = @('Inspect', 'Configure', 'Restart', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    CorruptPermissions = @('Inspect', 'ConfigurePermissions', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    CorruptServiceConfiguration = @('Inspect', 'ConfigureService', 'Restart', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    UnavailableDatabase = @('Inspect', 'Restart', 'EnsureStarted', 'Migrate', 'Verify', 'Activate', 'Inspect')
    InitializationIncomplete = @('Inspect', 'EnsureStarted', 'Provision', 'Migrate', 'Verify', 'Activate', 'Inspect')
    MigrationMismatch = @('Inspect', 'Migrate', 'Verify', 'Activate', 'Inspect')
    SecurityDrift = @('Inspect', 'Provision', 'Migrate', 'Verify', 'Activate', 'Inspect')
    MissingExtension = @('Inspect', 'Migrate', 'Verify', 'Activate', 'Inspect')
}
foreach ($entry in $repairCases.GetEnumerator()) {
    $fake = New-FakeLifecycle -InitialState $entry.Key
    $repairSecrets = if ($entry.Key -in @('MissingService', 'InitializationIncomplete', 'SecurityDrift')) {
        [pscustomobject]@{ Admin = 'admin-secret'; Migrator = 'migrator-secret'; Application = 'application-secret' }
    }
    else { $null }
    $result = Invoke-OfficeDatabaseLifecycleCore -Action Repair -Adapter $fake.Adapter -Context ([pscustomobject]@{ Secrets = $repairSecrets })
    Assert-OfficeTest -Condition ($result.InitialState -eq $entry.Key -and $result.FinalState -eq 'Ready') -Message "Repair classification '$($entry.Key)' did not converge."
    Assert-OfficeTest -Condition (($fake.Calls -join '|') -eq ($entry.Value -join '|')) -Message "Repair classification '$($entry.Key)' ran the wrong operations."
}

foreach ($unsafeState in @('ForeignCluster', 'ForeignService', 'MigrationDiverged', 'MissingCredentials', 'PortCollision', 'UnsafeInitializationState', 'UnsafePath', 'UnsupportedCluster')) {
    $fake = New-FakeLifecycle -InitialState $unsafeState
    Assert-OfficeThrows -Pattern $unsafeState -Action {
        Invoke-OfficeDatabaseLifecycleCore -Action Repair -Adapter $fake.Adapter -Context ([pscustomobject]@{ Secrets = $null })
    }
    Assert-OfficeTest -Condition (($fake.Calls -join '|') -eq 'Inspect') -Message "Unsafe state '$unsafeState' mutated the host."
}

$interrupted = New-FakeLifecycle -InitialState 'InterruptedInitialization'
$interruptedResult = Invoke-OfficeDatabaseLifecycleCore -Action Install -Adapter $interrupted.Adapter -Context ([pscustomobject]@{ Secrets = $null })
Assert-OfficeTest -Condition ($interruptedResult.InitialState -eq 'InterruptedInitialization' -and $interruptedResult.FinalState -eq 'Ready') -Message 'Interrupted initialization did not resume to Ready.'
$expectedInterruptedCalls = @('Inspect', 'RecoverInitialization', 'Inspect', 'StageRuntime', 'InitializeCluster', 'Configure', 'RegisterService', 'EnsureStarted', 'Provision', 'Migrate', 'Verify', 'Activate', 'Inspect')
Assert-OfficeTest -Condition (($interrupted.Calls -join '|') -eq ($expectedInterruptedCalls -join '|')) -Message 'Interrupted initialization recovery ran the wrong operations.'

$failedMigration = New-FakeLifecycle -InitialState 'MigrationMismatch' -FailMigration
Assert-OfficeThrows -Pattern 'Injected migration failure' -Action {
    Invoke-OfficeDatabaseLifecycleCore -Action Repair -Adapter $failedMigration.Adapter -Context ([pscustomobject]@{ Secrets = $null })
}
Assert-OfficeTest -Condition (-not ($failedMigration.Calls -contains 'Activate')) -Message 'Activation ran after a failed migration.'

$uninstall = New-FakeLifecycle -InitialState 'Ready'
$uninstallResult = Invoke-OfficeDatabaseLifecycleCore -Action Uninstall -Adapter $uninstall.Adapter -Context ([pscustomobject]@{ Secrets = $null })
Assert-OfficeTest -Condition ($uninstallResult.FinalState -eq 'PreservedData') -Message 'Default uninstall did not preserve data.'
Assert-OfficeTest -Condition (($uninstall.Calls -join '|') -eq 'Inspect|Unregister|RemoveRuntime') -Message 'Default uninstall ran an unexpected destructive operation.'
Assert-OfficeTest -Condition (-not (($uninstall.Calls -join '|') -match 'Purge|DeleteData|DeleteSecrets')) -Message 'Default uninstall attempted data purge.'

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("safarsuite-office-db-hermetic-$([Guid]::NewGuid().ToString('N'))")
New-Item -ItemType Directory -Path $testRoot | Out-Null
Set-Content -LiteralPath (Join-Path $testRoot '.safarsuite-hermetic-marker') -Value 'owned-test-root'
try {
    $atomicJsonPath = Join-Path $testRoot 'atomic-replace.json'
    & $lifecycleModule {
        param($Path)
        Set-OfficeAtomicJsonFile -Path $Path -Value ([ordered]@{ phase = 'Promoting' })
        Set-OfficeAtomicJsonFile -Path $Path -Value ([ordered]@{ phase = 'Installed' })
    } $atomicJsonPath
    $atomicJsonBytes = [IO.File]::ReadAllBytes($atomicJsonPath)
    $atomicJson = Get-Content -Raw -LiteralPath $atomicJsonPath | ConvertFrom-Json
    Assert-OfficeTest -Condition ($atomicJson.phase -eq 'Installed') -Message 'Atomic JSON replacement did not retain the second value.'
    Assert-OfficeTest `
        -Condition ($atomicJsonBytes.Length -lt 3 -or
            -not ($atomicJsonBytes[0] -eq 0xEF -and $atomicJsonBytes[1] -eq 0xBB -and $atomicJsonBytes[2] -eq 0xBF)) `
        -Message 'Atomic JSON replacement emitted a UTF-8 BOM.'
    Assert-OfficeTest `
        -Condition (@(Get-ChildItem -LiteralPath $testRoot -Filter '.safarsuite-write-*.tmp' -File).Count -eq 0) `
        -Message 'Atomic JSON replacement left a temporary file behind.'

    $baseEntries = @{
        'pgsql/bin/initdb.exe' = 'initdb'
        'pgsql/bin/pg_controldata.exe' = 'pg_controldata'
        'pgsql/bin/pg_ctl.exe' = 'pg_ctl'
        'pgsql/bin/pg_isready.exe' = 'pg_isready'
        'pgsql/bin/postgres.exe' = 'postgres'
        'pgsql/bin/psql.exe' = 'psql'
        'pgsql/bin/libcrypto-3-x64.dll' = 'crypto-runtime-dependency'
        'pgsql/lib/pg_trgm.dll' = 'pg_trgm'
        'pgsql/share/extension/pg_trgm.control' = 'control'
        'pgsql/share/extension/pg_trgm--1.3.sql' = 'base'
        'pgsql/share/extension/pg_trgm--1.3--1.4.sql' = 'upgrade'
        'pgsql/share/extension/pg_trgm--1.4--1.5.sql' = 'upgrade'
        'pgsql/share/extension/pg_trgm--1.5--1.6.sql' = 'upgrade'
        'pgsql/server_license.txt' = 'license'
        'pgsql/commandlinetools_3rd_party_licenses.txt' = 'notices'
        'pgsql/pgAdmin 4/bin/pgadmin4.exe' = 'excluded'
    }
    $sourceArchive = Join-Path $testRoot 'source.zip'
    New-TestArchive -Path $sourceArchive -Entries $baseEntries
    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourceArchive).Hash
    $manifest = @{
        majorVersion = 17
        version = '17.10'
        sha256 = $sourceHash
        sourceArchiveRoot = 'pgsql/'
        runtimeIncludeRoots = @('bin/', 'lib/', 'share/')
        runtimeIncludeFiles = @('server_license.txt', 'commandlinetools_3rd_party_licenses.txt')
        requiredRuntimeFiles = @(
            'bin/initdb.exe', 'bin/pg_controldata.exe', 'bin/pg_ctl.exe', 'bin/pg_isready.exe', 'bin/postgres.exe', 'bin/psql.exe',
            'lib/pg_trgm.dll', 'share/extension/pg_trgm.control', 'share/extension/pg_trgm--1.3.sql',
            'share/extension/pg_trgm--1.3--1.4.sql', 'share/extension/pg_trgm--1.4--1.5.sql', 'share/extension/pg_trgm--1.5--1.6.sql',
            'server_license.txt', 'commandlinetools_3rd_party_licenses.txt'
        )
    }
    $manifestPath = Join-Path $testRoot 'manifest.json'
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    $runtimeOne = Join-Path $testRoot 'runtime-one.zip'
    $runtimeTwo = Join-Path $testRoot 'runtime-two.zip'
    $runtimeBuild = & (Join-Path $PSScriptRoot 'New-OfficePostgresRuntimePayload.ps1') -SourceArchivePath $sourceArchive -OutputArchivePath $runtimeOne -DistributionManifestPath $manifestPath
    $secondRuntimeBuild = & (Join-Path $PSScriptRoot 'New-OfficePostgresRuntimePayload.ps1') -SourceArchivePath $sourceArchive -OutputArchivePath $runtimeTwo -DistributionManifestPath $manifestPath
    Assert-OfficeTest -Condition ((Get-FileHash $runtimeOne).Hash -eq (Get-FileHash $runtimeTwo).Hash) -Message 'Runtime payload generation is not deterministic.'
    Assert-OfficeTest `
        -Condition ($runtimeBuild.RuntimeFileCount -eq 15 -and $secondRuntimeBuild.RuntimeFileCount -eq 15) `
        -Message 'Runtime payload generation did not inventory every included file.'
    Assert-OfficeTest `
        -Condition ($runtimeBuild.RuntimeFileSha256.Contains('bin/libcrypto-3-x64.dll') -and
            -not $runtimeBuild.RequiredFileSha256.Contains('bin/libcrypto-3-x64.dll')) `
        -Message 'A non-required runtime dependency is absent from the complete inventory.'
    $runtimeZip = [IO.Compression.ZipFile]::OpenRead($runtimeOne)
    try {
        Assert-OfficeTest -Condition ($null -eq $runtimeZip.GetEntry('pgAdmin 4/bin/pgadmin4.exe')) -Message 'Excluded pgAdmin content entered the runtime.'
        foreach ($required in $manifest.requiredRuntimeFiles) {
            Assert-OfficeTest -Condition ($null -ne $runtimeZip.GetEntry([string]$required)) -Message "Runtime omitted '$required'."
        }
    }
    finally { $runtimeZip.Dispose() }

    $installedRuntime = Join-Path $testRoot 'installed-runtime'
    [IO.Compression.ZipFile]::ExtractToDirectory($runtimeOne, $installedRuntime)
    $runtimeInventory = ($runtimeBuild.RuntimeFileSha256 | ConvertTo-Json -Depth 4) | ConvertFrom-Json
    $runtimeDistribution = [pscustomobject]@{
        version = '17.10'
        runtimeFileCount = [int]$runtimeBuild.RuntimeFileCount
        runtimeFileSha256 = $runtimeInventory
    }
    $runtimePackageManifest = [pscustomobject]@{
        postgresql = [pscustomobject]@{ runtimeSha256 = [string]$runtimeBuild.RuntimeSha256 }
    }
    $runtimeReceipt = [ordered]@{
        schemaVersion = 1
        product = 'SafarSuite Control Desk'
        phase = 'Installed'
        installedDirectory = $installedRuntime
        postgresVersion = '17.10'
        runtimeSha256 = [string]$runtimeBuild.RuntimeSha256
    }
    [IO.File]::WriteAllText(
        (Join-Path $installedRuntime '.safarsuite-runtime-receipt.json'),
        ($runtimeReceipt | ConvertTo-Json -Depth 4),
        [Text.UTF8Encoding]::new($false))
    $installedRuntimeContext = [pscustomobject]@{
        Distribution = $runtimeDistribution
        PackageManifest = $runtimePackageManifest
        Paths = [pscustomobject]@{ RuntimeRoot = $installedRuntime }
    }
    $testInstalledRuntime = {
        param($Context)
        & $lifecycleModule { param($ctx) Test-OfficeInstalledRuntimeIntegrity -Context $ctx } $Context
    }
    Assert-OfficeTest `
        -Condition (& $testInstalledRuntime $installedRuntimeContext) `
        -Message 'The exact installed runtime inventory was not accepted.'

    $nonRequiredRuntimePath = Join-Path $installedRuntime 'bin\libcrypto-3-x64.dll'
    [IO.File]::WriteAllText($nonRequiredRuntimePath, 'tampered-non-required-dependency', [Text.UTF8Encoding]::new($false))
    Assert-OfficeTest `
        -Condition (-not (& $testInstalledRuntime $installedRuntimeContext)) `
        -Message 'Installed verification accepted a tampered non-required runtime dependency.'
    [IO.File]::WriteAllText($nonRequiredRuntimePath, 'crypto-runtime-dependency', [Text.UTF8Encoding]::new($false))
    Assert-OfficeTest `
        -Condition (& $testInstalledRuntime $installedRuntimeContext) `
        -Message 'Restoring a non-required runtime dependency did not restore exact integrity.'

    $unexpectedRuntimePath = Join-Path $installedRuntime 'bin\unexpected-runtime.dll'
    [IO.File]::WriteAllText($unexpectedRuntimePath, 'unexpected', [Text.UTF8Encoding]::new($false))
    Assert-OfficeTest `
        -Condition (-not (& $testInstalledRuntime $installedRuntimeContext)) `
        -Message 'Installed verification accepted an unexpected runtime file.'
    Remove-Item -LiteralPath $unexpectedRuntimePath -Force
    Assert-OfficeTest `
        -Condition (& $testInstalledRuntime $installedRuntimeContext) `
        -Message 'Removing an unexpected runtime file did not restore exact integrity.'

    $badHashManifest = $manifest.Clone()
    $badHashManifest.sha256 = '0' * 64
    $badHashPath = Join-Path $testRoot 'bad-hash.json'
    $badHashManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $badHashPath -Encoding utf8
    $badHashOutput = Join-Path $testRoot 'bad-hash-output.zip'
    Assert-OfficeThrows -Pattern 'SHA-256' -Action {
        & (Join-Path $PSScriptRoot 'New-OfficePostgresRuntimePayload.ps1') -SourceArchivePath $sourceArchive -OutputArchivePath $badHashOutput -DistributionManifestPath $badHashPath
    }
    Assert-OfficeTest -Condition (-not (Test-Path -LiteralPath $badHashOutput)) -Message 'Rejected hash left a partial runtime archive.'

    $collisionArchive = Join-Path $testRoot 'collision.zip'
    New-TestArchive -Path $collisionArchive -Entries $baseEntries
    $collisionZip = [IO.Compression.ZipFile]::Open($collisionArchive, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $collisionEntry = $collisionZip.CreateEntry('pgsql/bin/INITDB.exe')
        $collisionEntry.LastWriteTime = [DateTimeOffset]::Parse('2026-07-19T00:00:00Z')
        $collisionWriter = [IO.StreamWriter]::new($collisionEntry.Open(), [Text.UTF8Encoding]::new($false))
        try { $collisionWriter.Write('collision') }
        finally { $collisionWriter.Dispose() }
    }
    finally { $collisionZip.Dispose() }
    $collisionManifest = $manifest.Clone()
    $collisionManifest.sha256 = (Get-FileHash $collisionArchive).Hash
    $collisionManifestPath = Join-Path $testRoot 'collision.json'
    $collisionManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $collisionManifestPath -Encoding utf8
    Assert-OfficeThrows -Pattern 'case-colliding' -Action {
        & (Join-Path $PSScriptRoot 'New-OfficePostgresRuntimePayload.ps1') -SourceArchivePath $collisionArchive -OutputArchivePath (Join-Path $testRoot 'collision-output.zip') -DistributionManifestPath $collisionManifestPath
    }

    $unsafeArchive = Join-Path $testRoot 'unsafe.zip'
    $unsafeEntries = $baseEntries.Clone()
    $unsafeEntries['pgsql/bin/../escape.dll'] = 'unsafe'
    New-TestArchive -Path $unsafeArchive -Entries $unsafeEntries
    $unsafeManifest = $manifest.Clone()
    $unsafeManifest.sha256 = (Get-FileHash $unsafeArchive).Hash
    $unsafeManifestPath = Join-Path $testRoot 'unsafe.json'
    $unsafeManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $unsafeManifestPath -Encoding utf8
    Assert-OfficeThrows -Pattern 'unsafe' -Action {
        & (Join-Path $PSScriptRoot 'New-OfficePostgresRuntimePayload.ps1') -SourceArchivePath $unsafeArchive -OutputArchivePath (Join-Path $testRoot 'unsafe-output.zip') -DistributionManifestPath $unsafeManifestPath
    }
}
finally {
    $resolvedTestRoot = (Resolve-Path -LiteralPath $testRoot).Path
    $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
    if (-not $resolvedTestRoot.StartsWith($resolvedTempRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath (Join-Path $resolvedTestRoot '.safarsuite-hermetic-marker'))) {
        throw 'Hermetic cleanup safety check failed.'
    }
    Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
}

Write-Host 'Office database lifecycle hermetic proof passed.'
Write-Host 'Fresh install and rerun: idempotent'
Write-Host 'Repair classifications: covered'
Write-Host 'Migration divergence and activation failure: fail closed'
Write-Host 'Runtime payload integrity and filtering: verified'
