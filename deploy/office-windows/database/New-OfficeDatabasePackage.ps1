[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true)][string]$PostgresDistributionArchivePath,
    [Parameter(Mandatory = $true)][string]$VisualCppRedistributablePath,
    [Parameter(Mandatory = $true)][string]$SourceRevision
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoPath = (Resolve-Path -LiteralPath $RepoRoot).Path
$packagePath = [IO.Path]::GetFullPath($PackageDirectory)
$databasePackagePath = Join-Path $packagePath 'database'
$sourceDatabasePath = $PSScriptRoot
$distributionManifestPath = Join-Path $sourceDatabasePath 'postgresql-distribution.json'
$distribution = Get-Content -Raw -LiteralPath $distributionManifestPath | ConvertFrom-Json
$postgresArchivePath = (Resolve-Path -LiteralPath $PostgresDistributionArchivePath).Path
$visualCppPath = (Resolve-Path -LiteralPath $VisualCppRedistributablePath).Path
$infrastructureProject = Join-Path $repoPath 'src\SafarSuite.ControlDesk.Infrastructure\SafarSuite.ControlDesk.Infrastructure.csproj'
$apiProject = Join-Path $repoPath 'src\SafarSuite.ControlDesk.Api\SafarSuite.ControlDesk.Api.csproj'
$contextName = 'SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.ControlDeskDbContext'
$bundlePath = Join-Path $databasePackagePath 'SafarSuite.ControlDesk.Migrations.exe'
$runtimeArchivePath = Join-Path $databasePackagePath ([string]$distribution.runtimeArchiveFileName)

function Invoke-DatabasePackageCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [switch]$Capture
    )

    Push-Location $WorkingDirectory
    try {
        $previousErrorActionPreference = $ErrorActionPreference
        try {
            # Windows PowerShell 5.1 turns redirected native stderr into
            # ErrorRecord instances. Capture them and trust the exit code.
            $ErrorActionPreference = 'Continue'
            $output = @(& $FilePath @Arguments 2>&1)
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        if ($exitCode -ne 0) {
            $output | ForEach-Object { Write-Host ([string]$_) }
            throw "$FilePath exited with code $exitCode."
        }
        if ($Capture) {
            return $output
        }
        $output | ForEach-Object { Write-Host ([string]$_) }
    }
    finally {
        Pop-Location
    }
}

foreach ($source in @(
    @{ Path = $postgresArchivePath; Expected = [string]$distribution.sha256; Name = 'PostgreSQL distribution' },
    @{ Path = $visualCppPath; Expected = [string]$distribution.visualCppRuntime.sha256; Name = 'Microsoft Visual C++ runtime' }
)) {
    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source.Path).Hash.ToUpperInvariant()
    if ($actualHash -ne $source.Expected.ToUpperInvariant()) {
        throw "$($source.Name) SHA-256 does not match the pinned distribution policy."
    }
}

$visualCppSignature = Get-AuthenticodeSignature -LiteralPath $visualCppPath
if ($visualCppSignature.Status -ne 'Valid' -or $visualCppSignature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
    throw 'The Microsoft Visual C++ runtime must carry a valid Microsoft Authenticode signature.'
}

if (Test-Path -LiteralPath $databasePackagePath) {
    if (@(Get-ChildItem -LiteralPath $databasePackagePath -Force).Count -gt 0) {
        throw "The database package directory must be absent or empty: $databasePackagePath"
    }
}
else {
    New-Item -ItemType Directory -Path $databasePackagePath | Out-Null
}

$allowlistedLifecycleFiles = @(
    'postgresql-distribution.json',
    'postgresql.conf.template',
    'pg_hba.conf.template',
    'OfficeDatabaseLifecycle.psm1',
    'Install-OfficeDatabase.ps1',
    'Repair-OfficeDatabase.ps1',
    'Uninstall-OfficeDatabase.ps1'
)
foreach ($fileName in $allowlistedLifecycleFiles) {
    Copy-Item -LiteralPath (Join-Path $sourceDatabasePath $fileName) -Destination (Join-Path $databasePackagePath $fileName)
}

Copy-Item -LiteralPath $visualCppPath -Destination (Join-Path $databasePackagePath ([string]$distribution.visualCppRuntime.archiveFileName))

$runtimeBuild = & (Join-Path $sourceDatabasePath 'New-OfficePostgresRuntimePayload.ps1') `
    -SourceArchivePath $postgresArchivePath `
    -OutputArchivePath $runtimeArchivePath `
    -DistributionManifestPath $distributionManifestPath

Invoke-DatabasePackageCommand -FilePath 'dotnet' -Arguments @('tool', 'restore') -WorkingDirectory $repoPath
Invoke-DatabasePackageCommand `
    -FilePath 'dotnet' `
    -Arguments @('build', $apiProject, '--configuration', 'Release', '--no-restore') `
    -WorkingDirectory $repoPath

$previousConnection = [Environment]::GetEnvironmentVariable('SAFARSUITE_CONTROL_DESK_CONNECTION_STRING', 'Process')
try {
    [Environment]::SetEnvironmentVariable(
        'SAFARSUITE_CONTROL_DESK_CONNECTION_STRING',
        'Host=127.0.0.1;Port=1;Database=package_manifest_only;Username=package_manifest_only;Password=package_manifest_only',
        'Process')

    Invoke-DatabasePackageCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'tool', 'run', 'dotnet-ef', 'migrations', 'bundle',
            '--project', $infrastructureProject,
            '--startup-project', $apiProject,
            '--context', $contextName,
            '--configuration', 'Release',
            '--no-build',
            '--self-contained',
            '--target-runtime', 'win-x64',
            '--output', $bundlePath,
            '--force'
        ) `
        -WorkingDirectory $repoPath

    $migrationOutput = Invoke-DatabasePackageCommand `
        -FilePath 'dotnet' `
        -Arguments @(
            'tool', 'run', 'dotnet-ef', 'migrations', 'list',
            '--project', $infrastructureProject,
            '--startup-project', $apiProject,
            '--context', $contextName,
            '--configuration', 'Release',
            '--no-build', '--no-connect', '--json'
        ) `
        -WorkingDirectory $repoPath `
        -Capture
}
finally {
    [Environment]::SetEnvironmentVariable('SAFARSUITE_CONTROL_DESK_CONNECTION_STRING', $previousConnection, 'Process')
}

$migrationOutputText = $migrationOutput -join [Environment]::NewLine
$jsonStart = $migrationOutputText.IndexOf('[')
$jsonEnd = $migrationOutputText.LastIndexOf(']')
if ($jsonStart -lt 0 -or $jsonEnd -le $jsonStart) {
    throw 'The EF migration list did not contain a JSON array.'
}
$parsedMigrationRows = $migrationOutputText.Substring($jsonStart, $jsonEnd - $jsonStart + 1) | ConvertFrom-Json
$migrationRows = @($parsedMigrationRows)
$migrationIds = @($migrationRows | ForEach-Object { [string]$_.id })
$expectedTarget = '20260713220254_AddPortalPaymentBoundary'
if ($migrationIds.Count -ne 32 -or $migrationIds[-1] -ne $expectedTarget -or @($migrationIds | Select-Object -Unique).Count -ne 32) {
    throw 'The Control Desk migration ledger is not the reviewed 32-migration OFFICE-P0-03 target.'
}

Set-Content -LiteralPath (Join-Path $databasePackagePath 'appsettings.json') -Encoding utf8 -Value '{}'

$runtimeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimeArchivePath).Hash.ToUpperInvariant()
$bundleHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $bundlePath).Hash.ToUpperInvariant()
$runtimeBytes = (Get-Item -LiteralPath $runtimeArchivePath).Length
$bundleBytes = (Get-Item -LiteralPath $bundlePath).Length

$packageManifest = [ordered]@{
    schemaVersion = 1
    product = 'SafarSuite Control Desk'
    lifecycle = 'native-postgresql-17'
    sourceRevision = $SourceRevision
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    postgresql = [ordered]@{
        majorVersion = [int]$distribution.majorVersion
        version = [string]$distribution.version
        sourceDistribution = [string]$distribution.distribution
        sourceArchiveFileName = [string]$distribution.archiveFileName
        sourceDownloadUri = [string]$distribution.downloadUri
        sourceSha256 = ([string]$distribution.sha256).ToUpperInvariant()
        runtimeIncluded = $true
        runtimeArchiveFileName = [string]$distribution.runtimeArchiveFileName
        runtimeSha256 = $runtimeHash
        runtimeBytes = [long]$runtimeBytes
        runtimeFileCount = [int]$runtimeBuild.RuntimeFileCount
        runtimeFileSha256 = $runtimeBuild.RuntimeFileSha256
        requiredRuntimeFiles = @($distribution.requiredRuntimeFiles)
        requiredRuntimeFileSha256 = $runtimeBuild.RequiredFileSha256
        visualCppRuntime = [ordered]@{
            product = [string]$distribution.visualCppRuntime.product
            minimumVersion = [string]$distribution.visualCppRuntime.minimumVersion
            archiveFileName = [string]$distribution.visualCppRuntime.archiveFileName
            downloadUri = [string]$distribution.visualCppRuntime.downloadUri
            sha256 = ([string]$distribution.visualCppRuntime.sha256).ToUpperInvariant()
        }
        serviceName = [string]$distribution.serviceName
        serviceDisplayName = [string]$distribution.serviceDisplayName
        serviceAccount = [string]$distribution.serviceAccount
        databaseName = [string]$distribution.databaseName
        adminRole = [string]$distribution.adminRole
        migratorRole = [string]$distribution.migratorRole
        applicationRole = [string]$distribution.applicationRole
        port = [int]$distribution.port
        listenAddresses = @('127.0.0.1')
        passwordEncryption = 'scram-sha-256'
    }
    migrations = [ordered]@{
        bundleFileName = 'SafarSuite.ControlDesk.Migrations.exe'
        bundleSha256 = $bundleHash
        bundleBytes = [long]$bundleBytes
        context = $contextName
        historyTable = 'control.__ef_migrations_history'
        count = $migrationIds.Count
        orderedIds = $migrationIds
        target = $expectedTarget
        requiredExtensions = @('pg_trgm')
        lifecycleMutex = 'Global\SafarSuiteControlDeskDatabaseLifecycle'
        localMutex = 'Global\SafarSuiteControlDeskDatabaseMigration'
        databaseLock = 'Entity Framework migration bundle lock'
    }
    apiServiceContract = [ordered]@{
        serviceName = 'SafarSuiteControlDeskApi'
        dependsOn = @([string]$distribution.serviceName)
        createServiceInWorkPackage = 'OFFICE-P0-05'
        activationRequiresExactMigrationTarget = $true
    }
}

$packageManifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $databasePackagePath 'database-package-manifest.json') -Encoding utf8

return [pscustomobject]@{
    ManifestPath = Join-Path $databasePackagePath 'database-package-manifest.json'
    RuntimeSha256 = $runtimeHash
    RuntimeBytes = [long]$runtimeBytes
    BundleSha256 = $bundleHash
    BundleBytes = [long]$bundleBytes
    MigrationCount = $migrationIds.Count
    MigrationTarget = $expectedTarget
}
