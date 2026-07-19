#Requires -Version 5.1

Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$script:BoundaryProofName = 'safarsuite-office-postgresql-lifecycle-boundary'
$script:BoundaryEvidenceFileName = 'runtime-stage-boundary.json'
$script:ExecutableNames = @('initdb.exe', 'postgres.exe')
$script:PhaseNames = @('FreshBeforeAcl', 'FreshAfterAcl', 'InstalledAfterLifecycle')
$script:RuntimeClasses = @('FreshExtracted', 'Installed')
$script:AclClasses = @('InheritedRunnerAcl', 'RestrictedRuntimeAcl')
$script:LaunchContexts = @('InheritedProcess', 'RuntimeBin')
$script:TransitionClassifications = @(
    'AlreadySatisfied',
    'InstalledNoReboot',
    'AnotherVersionInstalled',
    'InstalledRebootRequired',
    'InstallerFailed'
)
$script:BoundaryOutcomes = @(
    'PrerequisiteRebootBoundary',
    'PostLifecycleMachineBoundary',
    'RestrictedAclBoundary',
    'WorkingDirectoryBoundary',
    'InstalledRuntimeBoundary',
    'FullInitdbInvocationBoundary',
    'Inconclusive',
    'DiagnosticFailed'
)
$script:BoundaryIssueCodes = @(
    'VisualCppRebootRequired',
    'FreshRuntimeLoaderFailure',
    'AclChangedLoaderResult',
    'WorkingDirectoryChangedLoaderResult',
    'InstalledRuntimeDiffersFromFreshRuntime',
    'VersionProbesPassButLifecycleFails',
    'BoundaryInconclusive',
    'DiagnosticInternalFailure'
)
$script:FullInitdbModes = @('InheritedCwd', 'RuntimeBinCwd', 'ApprovedRuntimeRoots')
$script:FullInitdbOutcomes = @(
    'NotRequired',
    'BaselineDidNotReproduce',
    'WorkingDirectoryBoundary',
    'ApprovedRuntimeRootsBoundary',
    'ApprovedRuntimeRootsHypothesisDisproved',
    'Inconclusive',
    'DiagnosticFailed'
)
$script:FullInitdbIssueCodes = @(
    'BaselineDidNotReproduce',
    'RuntimeBinCwdChangedResult',
    'ApprovedRuntimeRootsChangedResult',
    'ApprovedRuntimeRootsDidNotChangeResult',
    'FullInitdbComparisonInconclusive',
    'FullInitdbDiagnosticInternalFailure'
)
$script:FullInitdbStages = @(
    'NotRun',
    'PrepareTrials',
    'RunInheritedCwd',
    'RunRuntimeBinCwd',
    'RunApprovedRuntimeRoots',
    'Classify',
    'Completed'
)

function ConvertTo-BoundaryExitCodeHex {
    param([Parameter(Mandatory = $true)][int]$ExitCode)

    $unsigned = [BitConverter]::ToUInt32([BitConverter]::GetBytes($ExitCode), 0)
    return "0x$($unsigned.ToString('X8'))"
}

function Test-BoundarySafeVersion {
    param([AllowNull()][string]$Version)

    return $null -eq $Version -or $Version -match '^[0-9]+(?:\.[0-9]+){1,3}$'
}

function Get-BoundarySafeRunnerValue {
    param([AllowNull()][string]$Value)

    if (-not [string]::IsNullOrWhiteSpace($Value) -and $Value -match '^[A-Za-z0-9._-]{1,80}$') {
        return $Value
    }
    return 'Unknown'
}

function Test-BoundaryTransitionConsistency {
    param([Parameter(Mandatory = $true)]$Transition)

    $classification = [string]$Transition.classification
    $installerInvoked = [bool]$Transition.installerInvoked
    $rebootRequired = [bool]$Transition.rebootRequired
    $minimumSatisfied = [bool]$Transition.minimumSatisfied
    if ($classification -eq 'AlreadySatisfied') {
        return -not $installerInvoked -and $null -eq $Transition.installerExitCode -and
            -not $rebootRequired -and $minimumSatisfied
    }
    if (-not $installerInvoked -or $null -eq $Transition.installerExitCode) {
        return $false
    }
    $exitCode = [int]$Transition.installerExitCode
    switch ($classification) {
        'InstalledNoReboot' { return $exitCode -eq 0 -and -not $rebootRequired -and $minimumSatisfied }
        'AnotherVersionInstalled' { return $exitCode -eq 1638 -and -not $rebootRequired -and $minimumSatisfied }
        'InstalledRebootRequired' { return $exitCode -eq 3010 -and $rebootRequired -and $minimumSatisfied }
        'InstallerFailed' { return $exitCode -notin @(0, 1638, 3010) -and -not $rebootRequired }
        default { return $false }
    }
}

function Get-BoundaryFailureEvidence {
    param([Parameter(Mandatory = $true)]$LifecycleFailure)

    $message = [string]$LifecycleFailure.Exception.Message
    $match = [regex]::Match(
        $message,
        "^A required database lifecycle process failed with exit code (?<code>-?[0-9]+)\. Executable '(?<executable>[A-Za-z0-9._-]+)'; hexadecimal exit code (?<hex>0x[0-9A-F]{8})\.$",
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success -or $match.Groups['executable'].Value -notin $script:ExecutableNames) {
        return [ordered]@{
            observed = $false
            executable = $null
            exitCode = $null
            exitCodeHex = $null
        }
    }

    $exitCode = 0
    if (-not [int]::TryParse(
        $match.Groups['code'].Value,
        [Globalization.NumberStyles]::Integer,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$exitCode)) {
        return [ordered]@{
            observed = $false
            executable = $null
            exitCode = $null
            exitCodeHex = $null
        }
    }
    $expectedHex = ConvertTo-BoundaryExitCodeHex -ExitCode $exitCode
    if ($match.Groups['hex'].Value -cne $expectedHex) {
        return [ordered]@{
            observed = $false
            executable = $null
            exitCode = $null
            exitCodeHex = $null
        }
    }

    return [ordered]@{
        observed = $true
        executable = $match.Groups['executable'].Value
        exitCode = $exitCode
        exitCodeHex = $expectedHex
    }
}

function Get-BoundaryPrerequisiteTransitions {
    param([Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule)

    $rawTransitions = @(& $LifecycleModule { Get-OfficeVisualCppRuntimeTransitionEvidence })
    $safeTransitions = [Collections.Generic.List[object]]::new()
    $sequence = 0
    foreach ($transition in $rawTransitions) {
        $sequence++
        $minimumVersion = [string]$transition.minimumVersion
        $versionBefore = if ($null -eq $transition.versionBefore) { $null } else { [string]$transition.versionBefore }
        $versionAfter = if ($null -eq $transition.versionAfter) { $null } else { [string]$transition.versionAfter }
        $classification = [string]$transition.classification
        if (-not (Test-BoundarySafeVersion -Version $minimumVersion) -or
            -not (Test-BoundarySafeVersion -Version $versionBefore) -or
            -not (Test-BoundarySafeVersion -Version $versionAfter) -or
            $classification -notin $script:TransitionClassifications) {
            throw 'The prerequisite transition contained an unsafe diagnostic value.'
        }

        $installerExitCode = $null
        $installerExitCodeHex = $null
        if ([bool]$transition.installerInvoked) {
            $installerExitCode = [int]$transition.installerExitCode
            $installerExitCodeHex = ConvertTo-BoundaryExitCodeHex -ExitCode $installerExitCode
            if ([string]$transition.installerExitCodeHex -cne $installerExitCodeHex) {
                throw 'The prerequisite transition exit classifications disagree.'
            }
        }
        elseif ($null -ne $transition.installerExitCode -or -not [string]::IsNullOrEmpty([string]$transition.installerExitCodeHex)) {
            throw 'A skipped prerequisite transition retained an installer exit code.'
        }

        $safeTransition = [pscustomobject][ordered]@{
            sequence = $sequence
            minimumVersion = $minimumVersion
            versionBefore = $versionBefore
            installerInvoked = [bool]$transition.installerInvoked
            installerExitCode = $installerExitCode
            installerExitCodeHex = $installerExitCodeHex
            rebootRequired = [bool]$transition.rebootRequired
            versionAfter = $versionAfter
            minimumSatisfied = [bool]$transition.minimumSatisfied
            classification = $classification
        }
        if (-not (Test-BoundaryTransitionConsistency -Transition $safeTransition)) {
            throw 'The prerequisite transition classification is inconsistent.'
        }
        [void]$safeTransitions.Add($safeTransition)
    }
    return @($safeTransitions)
}

function Invoke-BoundaryVersionProbe {
    param(
        [Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule,
        [Parameter(Mandatory = $true)][string]$RuntimeRoot,
        [Parameter(Mandatory = $true)][ValidateSet('FreshBeforeAcl', 'FreshAfterAcl', 'InstalledAfterLifecycle')][string]$Phase,
        [Parameter(Mandatory = $true)][ValidateSet('FreshExtracted', 'Installed')][string]$RuntimeClass,
        [Parameter(Mandatory = $true)][ValidateSet('InheritedRunnerAcl', 'RestrictedRuntimeAcl')][string]$AclClass,
        [Parameter(Mandatory = $true)][ValidateSet('InheritedProcess', 'RuntimeBin')][string]$LaunchContext,
        [Parameter(Mandatory = $true)][ValidateSet('initdb.exe', 'postgres.exe')][string]$Executable,
        [Parameter(Mandatory = $true)][string]$PostgresVersion,
        [Parameter(Mandatory = $true)][int]$Sequence
    )

    $runtimeBin = Join-Path $RuntimeRoot 'bin'
    $executablePath = Join-Path $runtimeBin $Executable
    try {
        $result = & $LifecycleModule {
            param($Path, $RuntimeBin, $UseRuntimeBin)
            if ($UseRuntimeBin) {
                return Invoke-OfficeNativeCommand `
                    -FilePath $Path `
                    -Arguments @('--version') `
                    -WorkingDirectory $RuntimeBin `
                    -TimeoutSeconds 30 `
                    -AllowFailure
            }
            return Invoke-OfficeNativeCommand `
                -FilePath $Path `
                -Arguments @('--version') `
                -TimeoutSeconds 30 `
                -AllowFailure
        } $executablePath $runtimeBin ($LaunchContext -eq 'RuntimeBin')
        if ($null -eq $result) {
            throw 'The version probe returned no process result.'
        }
        $exitCode = [int]$result.ExitCode
        $expectedOutput = if ($Executable -eq 'initdb.exe') {
            "initdb (PostgreSQL) $PostgresVersion"
        }
        else {
            "postgres (PostgreSQL) $PostgresVersion"
        }
        $versionMatched = $exitCode -eq 0 -and ([string]$result.StandardOutput).Trim() -ceq $expectedOutput
        return [ordered]@{
            sequence = $Sequence
            phase = $Phase
            runtimeClass = $RuntimeClass
            aclClass = $AclClass
            launchContext = $LaunchContext
            executable = $Executable
            completed = $true
            exitCode = $exitCode
            exitCodeHex = ConvertTo-BoundaryExitCodeHex -ExitCode $exitCode
            versionMatched = [bool]$versionMatched
            issueCode = $null
        }
    }
    catch {
        return [ordered]@{
            sequence = $Sequence
            phase = $Phase
            runtimeClass = $RuntimeClass
            aclClass = $AclClass
            launchContext = $LaunchContext
            executable = $Executable
            completed = $false
            exitCode = $null
            exitCodeHex = $null
            versionMatched = $false
            issueCode = 'ProbeInvocationFailed'
        }
    }
}

function Add-BoundaryProbeSet {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][Collections.Generic.List[object]]$Probes,
        [Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule,
        [Parameter(Mandatory = $true)][string]$RuntimeRoot,
        [Parameter(Mandatory = $true)][ValidateSet('FreshBeforeAcl', 'FreshAfterAcl', 'InstalledAfterLifecycle')][string]$Phase,
        [Parameter(Mandatory = $true)][ValidateSet('FreshExtracted', 'Installed')][string]$RuntimeClass,
        [Parameter(Mandatory = $true)][ValidateSet('InheritedRunnerAcl', 'RestrictedRuntimeAcl')][string]$AclClass,
        [Parameter(Mandatory = $true)][string]$PostgresVersion
    )

    foreach ($executable in $script:ExecutableNames) {
        foreach ($launchContext in $script:LaunchContexts) {
            [void]$Probes.Add((Invoke-BoundaryVersionProbe `
                -LifecycleModule $LifecycleModule `
                -RuntimeRoot $RuntimeRoot `
                -Phase $Phase `
                -RuntimeClass $RuntimeClass `
                -AclClass $AclClass `
                -LaunchContext $launchContext `
                -Executable $executable `
                -PostgresVersion $PostgresVersion `
                -Sequence ($Probes.Count + 1)))
        }
    }
}

function Test-BoundaryProbeReady {
    param([Parameter(Mandatory = $true)]$Probe)

    return [bool]$Probe.completed -and [int]$Probe.exitCode -eq 0 -and [bool]$Probe.versionMatched
}

function Get-OfficePostgresLifecycleBoundaryOutcome {
    param(
        [Parameter(Mandatory = $true)][object[]]$Transitions,
        [Parameter(Mandatory = $true)][object[]]$Probes
    )

    $issueCodes = [Collections.Generic.List[string]]::new()
    $freshBeforeAcl = @($Probes | Where-Object { $_.phase -eq 'FreshBeforeAcl' })
    $freshAfterAcl = @($Probes | Where-Object { $_.phase -eq 'FreshAfterAcl' })
    $installed = @($Probes | Where-Object { $_.phase -eq 'InstalledAfterLifecycle' })
    $invalidProbes = @($Probes | Where-Object {
        -not [bool]$_.completed -or
        ([int]$_.exitCode -eq 0 -and -not [bool]$_.versionMatched) -or
        ([int]$_.exitCode -ne 0 -and [string]$_.exitCodeHex -cne '0xC0000135')
    })
    if ($invalidProbes.Count -gt 0) {
        $issueCodes.Add('BoundaryInconclusive')
        return [pscustomobject]@{ Outcome = 'Inconclusive'; IssueCodes = @($issueCodes) }
    }
    $freshBeforeReady = $freshBeforeAcl.Count -eq 4 -and @($freshBeforeAcl | Where-Object { -not (Test-BoundaryProbeReady -Probe $_) }).Count -eq 0
    $freshAfterReady = $freshAfterAcl.Count -eq 4 -and @($freshAfterAcl | Where-Object { -not (Test-BoundaryProbeReady -Probe $_) }).Count -eq 0
    $installedReady = $installed.Count -eq 4 -and @($installed | Where-Object { -not (Test-BoundaryProbeReady -Probe $_) }).Count -eq 0
    $rebootRequired = @($Transitions | Where-Object { $_.rebootRequired -or $_.classification -eq 'InstalledRebootRequired' }).Count -gt 0

    if ($rebootRequired) {
        $issueCodes.Add('VisualCppRebootRequired')
    }
    foreach ($phase in $script:PhaseNames) {
        foreach ($executable in $script:ExecutableNames) {
            $inherited = @($Probes | Where-Object {
                $_.phase -eq $phase -and $_.executable -eq $executable -and $_.launchContext -eq 'InheritedProcess'
            })
            $runtimeBin = @($Probes | Where-Object {
                $_.phase -eq $phase -and $_.executable -eq $executable -and $_.launchContext -eq 'RuntimeBin'
            })
            if ($inherited.Count -eq 1 -and $runtimeBin.Count -eq 1 -and
                (Test-BoundaryProbeReady -Probe $inherited[0]) -ne (Test-BoundaryProbeReady -Probe $runtimeBin[0])) {
                $issueCodes.Add('WorkingDirectoryChangedLoaderResult')
                return [pscustomobject]@{ Outcome = 'WorkingDirectoryBoundary'; IssueCodes = @($issueCodes) }
            }
        }
    }
    if (-not $freshBeforeReady) {
        $issueCodes.Add('FreshRuntimeLoaderFailure')
        $outcome = if ($rebootRequired) { 'PrerequisiteRebootBoundary' } else { 'PostLifecycleMachineBoundary' }
        return [pscustomobject]@{ Outcome = $outcome; IssueCodes = @($issueCodes) }
    }

    $aclChangedResult = $false
    foreach ($before in $freshBeforeAcl) {
        $after = @($freshAfterAcl | Where-Object {
            $_.executable -eq $before.executable -and $_.launchContext -eq $before.launchContext
        })
        if ($after.Count -eq 1 -and (Test-BoundaryProbeReady -Probe $before) -ne (Test-BoundaryProbeReady -Probe $after[0])) {
            $aclChangedResult = $true
        }
    }
    if ($aclChangedResult) {
        $issueCodes.Add('AclChangedLoaderResult')
        return [pscustomobject]@{ Outcome = 'RestrictedAclBoundary'; IssueCodes = @($issueCodes) }
    }

    if ($freshAfterReady -and -not $installedReady) {
        $issueCodes.Add('InstalledRuntimeDiffersFromFreshRuntime')
        return [pscustomobject]@{ Outcome = 'InstalledRuntimeBoundary'; IssueCodes = @($issueCodes) }
    }
    if ($freshBeforeReady -and $freshAfterReady -and $installedReady) {
        $issueCodes.Add('VersionProbesPassButLifecycleFails')
        return [pscustomobject]@{ Outcome = 'FullInitdbInvocationBoundary'; IssueCodes = @($issueCodes) }
    }

    $issueCodes.Add('BoundaryInconclusive')
    return [pscustomobject]@{ Outcome = 'Inconclusive'; IssueCodes = @($issueCodes) }
}

function Test-BoundaryFullInitdbTrialSucceeded {
    param([Parameter(Mandatory = $true)]$Trial)

    return [bool]$Trial.completed -and [int]$Trial.exitCode -eq 0 -and [bool]$Trial.succeeded
}

function Get-OfficePostgresFullInitdbBoundaryOutcome {
    param([Parameter(Mandatory = $true)][object[]]$Trials)

    if ($Trials.Count -ne 3 -or @($Trials | Where-Object { -not [bool]$_.completed }).Count -gt 0) {
        return [pscustomobject]@{ Outcome = 'Inconclusive'; IssueCodes = @('FullInitdbComparisonInconclusive') }
    }
    foreach ($trial in $Trials) {
        $exitCode = [int]$trial.exitCode
        if (($exitCode -eq 0 -and -not [bool]$trial.succeeded) -or
            ($exitCode -ne 0 -and [bool]$trial.succeeded) -or
            ($exitCode -ne 0 -and [string]$trial.exitCodeHex -cne '0xC0000135')) {
            return [pscustomobject]@{ Outcome = 'Inconclusive'; IssueCodes = @('FullInitdbComparisonInconclusive') }
        }
    }

    $inherited = @($Trials | Where-Object { $_.mode -eq 'InheritedCwd' })
    $runtimeBin = @($Trials | Where-Object { $_.mode -eq 'RuntimeBinCwd' })
    $approvedRoots = @($Trials | Where-Object { $_.mode -eq 'ApprovedRuntimeRoots' })
    if ($inherited.Count -ne 1 -or $runtimeBin.Count -ne 1 -or $approvedRoots.Count -ne 1) {
        return [pscustomobject]@{ Outcome = 'Inconclusive'; IssueCodes = @('FullInitdbComparisonInconclusive') }
    }
    if (Test-BoundaryFullInitdbTrialSucceeded -Trial $inherited[0]) {
        return [pscustomobject]@{ Outcome = 'BaselineDidNotReproduce'; IssueCodes = @('BaselineDidNotReproduce') }
    }
    if (Test-BoundaryFullInitdbTrialSucceeded -Trial $runtimeBin[0]) {
        return [pscustomobject]@{ Outcome = 'WorkingDirectoryBoundary'; IssueCodes = @('RuntimeBinCwdChangedResult') }
    }
    if (Test-BoundaryFullInitdbTrialSucceeded -Trial $approvedRoots[0]) {
        return [pscustomobject]@{ Outcome = 'ApprovedRuntimeRootsBoundary'; IssueCodes = @('ApprovedRuntimeRootsChangedResult') }
    }
    return [pscustomobject]@{
        Outcome = 'ApprovedRuntimeRootsHypothesisDisproved'
        IssueCodes = @('ApprovedRuntimeRootsDidNotChangeResult')
    }
}

function Get-BoundaryApprovedRuntimeSearchValue {
    param(
        [Parameter(Mandatory = $true)]$PackageManifest,
        [Parameter(Mandatory = $true)][string]$RuntimeRoot
    )

    $runtimePath = [IO.Path]::GetFullPath($RuntimeRoot).TrimEnd('\')
    $inventoryProperty = $PackageManifest.postgresql.PSObject.Properties['runtimeFileSha256']
    if ($null -eq $inventoryProperty -or $null -eq $inventoryProperty.Value) {
        throw 'The validated PostgreSQL runtime inventory is unavailable.'
    }
    $inventoryNames = @($inventoryProperty.Value.PSObject.Properties.Name)
    $approved = [Collections.Generic.List[string]]::new()
    foreach ($relativeRoot in @('bin', 'lib')) {
        if (@($inventoryNames | Where-Object { $_.StartsWith("$relativeRoot/", [StringComparison]::Ordinal) }).Count -lt 1) {
            throw 'A manifest-approved PostgreSQL runtime root is absent from the inventory.'
        }
        $fullRoot = [IO.Path]::GetFullPath((Join-Path $runtimePath $relativeRoot))
        if (-not $fullRoot.StartsWith($runtimePath + '\', [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
            throw 'A manifest-approved PostgreSQL runtime root is unsafe or missing.'
        }
        [void]$approved.Add($fullRoot)
    }
    foreach ($systemRoot in @(
        [Environment]::SystemDirectory,
        [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    )) {
        $fullRoot = [IO.Path]::GetFullPath($systemRoot)
        if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
            throw 'A required Windows loader root is unavailable.'
        }
        [void]$approved.Add($fullRoot)
    }
    return ($approved -join ';')
}

function Invoke-BoundaryFullInitdbTrial {
    param(
        [Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule,
        [Parameter(Mandatory = $true)]$PackageManifest,
        [Parameter(Mandatory = $true)]$Paths,
        [Parameter(Mandatory = $true)][string]$TestRoot,
        [Parameter(Mandatory = $true)][ValidateSet('InheritedCwd', 'RuntimeBinCwd', 'ApprovedRuntimeRoots')][string]$Mode,
        [Parameter(Mandatory = $true)][int]$Sequence,
        [string]$ApprovedRuntimeSearchValue
    )

    $testPath = [IO.Path]::GetFullPath($TestRoot).TrimEnd('\')
    $trialRoot = Join-Path $testPath "full-initdb-$([Guid]::NewGuid().ToString('N'))"
    $trialPath = [IO.Path]::GetFullPath($trialRoot)
    if (-not $trialPath.StartsWith($testPath + '\', [StringComparison]::OrdinalIgnoreCase) -or
        (Test-Path -LiteralPath $trialPath)) {
        throw 'The full initialization trial root is unsafe or already exists.'
    }
    [void](New-Item -ItemType Directory -Path $trialPath)
    $dataPath = Join-Path $trialPath 'data'
    $bootstrapPath = Join-Path $trialPath 'bootstrap.txt'
    $trialPassword = $null
    try {
        $trialPassword = & $LifecycleModule { New-OfficeDatabasePassword }
        & $LifecycleModule {
            param($DataPath, $BootstrapPath, $Password)
            [void](New-Item -ItemType Directory -Path $DataPath)
            Set-OfficeRestrictedAcl -Path $DataPath -Profile Data
            Set-OfficeUtf8NoBomContent -Path $BootstrapPath -Value $Password
            Set-OfficeRestrictedAcl -Path $BootstrapPath -Profile Secrets
        } $dataPath $bootstrapPath $trialPassword

        $runtimeBin = Join-Path ([string]$Paths.RuntimeRoot) 'bin'
        $initdbPath = Join-Path $runtimeBin 'initdb.exe'
        $arguments = @(
            '-D', $dataPath,
            '-U', [string]$PackageManifest.postgresql.adminRole,
            '--auth-host=scram-sha-256', '--auth-local=scram-sha-256',
            '--encoding=UTF8', '--locale=C', "--pwfile=$bootstrapPath"
        )
        $overrides = @{}
        if ($Mode -eq 'ApprovedRuntimeRoots') {
            if ([string]::IsNullOrWhiteSpace($ApprovedRuntimeSearchValue)) {
                throw 'The approved runtime search value is missing.'
            }
            $overrides['PATH'] = $ApprovedRuntimeSearchValue
        }
        try {
            $exitCode = & $LifecycleModule {
                param($ExecutablePath, $Arguments, $Overrides, $RuntimeBin, $UseRuntimeBin)
                $parameters = @{
                    FilePath = $ExecutablePath
                    Arguments = $Arguments
                    Environment = $Overrides
                    TimeoutSeconds = 180
                    AllowFailure = $true
                }
                if ($UseRuntimeBin) {
                    $parameters['WorkingDirectory'] = $RuntimeBin
                }
                $result = Invoke-OfficeNativeCommand @parameters
                return [int]$result.ExitCode
            } $initdbPath $arguments $overrides $runtimeBin ($Mode -ne 'InheritedCwd')
            return [ordered]@{
                sequence = $Sequence
                mode = $Mode
                completed = $true
                exitCode = [int]$exitCode
                exitCodeHex = ConvertTo-BoundaryExitCodeHex -ExitCode ([int]$exitCode)
                succeeded = [int]$exitCode -eq 0
                issueCode = $null
            }
        }
        catch {
            return [ordered]@{
                sequence = $Sequence
                mode = $Mode
                completed = $false
                exitCode = $null
                exitCodeHex = $null
                succeeded = $false
                issueCode = 'InvocationFailed'
            }
        }
    }
    finally {
        if (Test-Path -LiteralPath $bootstrapPath -PathType Leaf) {
            Remove-Item -LiteralPath $bootstrapPath -Force
        }
        $trialPassword = $null
    }
}

function Set-BoundaryRuntimeAcl {
    param(
        [Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule,
        [Parameter(Mandatory = $true)][string]$RuntimeRoot
    )

    & $LifecycleModule {
        param($Root)
        Set-OfficeRestrictedAcl -Path $Root -Profile Runtime
        $descendants = @(Get-OfficeManagedTreeEntriesNoReparse -Root $Root)
        foreach ($descendant in $descendants) {
            Set-OfficeInheritedAcl -Path $descendant
        }
    } $RuntimeRoot
}

function Write-BoundaryEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidencePath,
        [Parameter(Mandatory = $true)]$Evidence,
        [scriptblock]$BeforePublish
    )

    $evidenceDirectory = Split-Path -Parent $EvidencePath
    New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null
    $json = $Evidence | ConvertTo-Json -Depth 10
    $encoding = [Text.UTF8Encoding]::new($false)
    $bytes = $encoding.GetBytes($json)
    $leafName = Split-Path -Leaf $EvidencePath
    $replacementId = [Guid]::NewGuid().ToString('N')
    $temporaryPath = Join-Path $evidenceDirectory ".$leafName.$replacementId.tmp"
    $backupPath = Join-Path $evidenceDirectory ".$leafName.$replacementId.bak"
    try {
        $stream = [IO.FileStream]::new(
            $temporaryPath,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None)
        try {
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)
        }
        finally {
            $stream.Dispose()
        }

        if ($PSBoundParameters.ContainsKey('BeforePublish')) {
            & $BeforePublish
        }

        if (Test-Path -LiteralPath $EvidencePath -PathType Leaf) {
            [IO.File]::Replace($temporaryPath, $EvidencePath, $backupPath, $true)
            Remove-Item -LiteralPath $backupPath -Force
        }
        else {
            [IO.File]::Move($temporaryPath, $EvidencePath)
        }
    }
    finally {
        foreach ($transientPath in @($temporaryPath, $backupPath)) {
            if (Test-Path -LiteralPath $transientPath) {
                Remove-Item -LiteralPath $transientPath -Force
            }
        }
    }
}

function Copy-BoundaryDictionary {
    param([Parameter(Mandatory = $true)][Collections.IDictionary]$Value)

    $copy = [ordered]@{}
    foreach ($key in $Value.Keys) {
        $copy[$key] = $Value[$key]
    }
    return $copy
}

function Invoke-OfficePostgresLifecycleBoundaryDiagnostic {
    param(
        [Parameter(Mandatory = $true)][Management.Automation.PSModuleInfo]$LifecycleModule,
        [Parameter(Mandatory = $true)]$PackageManifest,
        [Parameter(Mandatory = $true)]$Paths,
        [Parameter(Mandatory = $true)][string]$TestRoot,
        [Parameter(Mandatory = $true)][string]$EvidencePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$OfficePackageArchiveSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{32}$')][string]$InvocationNonce,
        [Parameter(Mandatory = $true)]$LifecycleFailure
    )

    if ($env:GITHUB_ACTIONS -ne 'true' -or [string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
        throw 'The lifecycle boundary diagnostic may run only on a disposable GitHub Actions runner.'
    }
    $runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd('\')
    $testPath = [IO.Path]::GetFullPath($TestRoot).TrimEnd('\')
    $evidenceFilePath = [IO.Path]::GetFullPath($EvidencePath)
    if (-not $testPath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $testPath) -notmatch '^safarsuite-office-db-[0-9a-f]{32}$' -or
        -not (Test-Path -LiteralPath (Join-Path $testPath '.safarsuite-native-ci-marker') -PathType Leaf)) {
        throw 'The lifecycle boundary diagnostic requires the owned native CI root.'
    }
    if (-not $evidenceFilePath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $evidenceFilePath) -cne $script:BoundaryEvidenceFileName) {
        throw 'The lifecycle boundary evidence path is outside its fixed CI location.'
    }

    $sourceRevision = [string]$PackageManifest.sourceRevision
    $runtimeSha256 = ([string]$PackageManifest.postgresql.runtimeSha256).ToUpperInvariant()
    $postgresVersion = [string]$PackageManifest.postgresql.version
    if ($sourceRevision -notmatch '^[0-9a-f]{40}$' -or
        $runtimeSha256 -notmatch '^[A-F0-9]{64}$' -or
        $postgresVersion -notmatch '^[0-9]+\.[0-9]+$') {
        throw 'The validated package manifest contains invalid boundary identifiers.'
    }

    $failureEvidence = Get-BoundaryFailureEvidence -LifecycleFailure $LifecycleFailure
    $baseEvidence = [ordered]@{
        schemaVersion = 1
        proof = $script:BoundaryProofName
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        sourceRevision = $sourceRevision
        invocationNonce = $InvocationNonce
        runner = [ordered]@{
            imageOs = Get-BoundarySafeRunnerValue -Value ([string]$env:ImageOS)
            imageVersion = Get-BoundarySafeRunnerValue -Value ([string]$env:ImageVersion)
            osVersion = Get-BoundarySafeRunnerValue -Value ([Environment]::OSVersion.Version.ToString())
        }
        package = [ordered]@{
            officeArchiveSha256 = $OfficePackageArchiveSha256.ToUpperInvariant()
            postgresRuntimeSha256 = $runtimeSha256
            postgresVersion = $postgresVersion
        }
        lifecycleFailure = $failureEvidence
    }

    try {
        $transitions = @(Get-BoundaryPrerequisiteTransitions -LifecycleModule $LifecycleModule)
        $runtimeArchivePath = [IO.Path]::GetFullPath([string]$Paths.RuntimeArchivePath)
        if (-not (Test-Path -LiteralPath $runtimeArchivePath -PathType Leaf) -or
            (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimeArchivePath).Hash.ToUpperInvariant() -cne $runtimeSha256) {
            throw 'The lifecycle boundary runtime archive is not the validated package payload.'
        }

        $freshRoot = Join-Path $testPath "runtime-boundary-$([Guid]::NewGuid().ToString('N'))"
        if (Test-Path -LiteralPath $freshRoot) {
            throw 'The lifecycle boundary runtime root already exists.'
        }
        [IO.Compression.ZipFile]::ExtractToDirectory($runtimeArchivePath, $freshRoot)
        $freshReceipt = [ordered]@{
            schemaVersion = 1
            product = 'SafarSuite Control Desk'
            phase = 'Promoting'
            stagingDirectory = $freshRoot
            installedDirectory = $freshRoot
            postgresVersion = $postgresVersion
            runtimeSha256 = $runtimeSha256
        }
        [IO.File]::WriteAllText(
            (Join-Path $freshRoot '.safarsuite-runtime-receipt.json'),
            ($freshReceipt | ConvertTo-Json -Depth 4),
            [Text.UTF8Encoding]::new($false))
        $freshContext = [pscustomobject]@{
            Distribution = $PackageManifest.postgresql
            PackageManifest = $PackageManifest
            Paths = [pscustomobject]@{ RuntimeRoot = $freshRoot }
        }
        $freshIntegrity = & $LifecycleModule {
            param($Context)
            Test-OfficeInstalledRuntimeIntegrity -Context $Context
        } $freshContext
        if (-not $freshIntegrity) {
            throw 'The fresh lifecycle boundary runtime failed package integrity verification.'
        }

        $probes = [Collections.Generic.List[object]]::new()
        Add-BoundaryProbeSet `
            -Probes $probes `
            -LifecycleModule $LifecycleModule `
            -RuntimeRoot $freshRoot `
            -Phase FreshBeforeAcl `
            -RuntimeClass FreshExtracted `
            -AclClass InheritedRunnerAcl `
            -PostgresVersion $postgresVersion
        Set-BoundaryRuntimeAcl -LifecycleModule $LifecycleModule -RuntimeRoot $freshRoot
        Add-BoundaryProbeSet `
            -Probes $probes `
            -LifecycleModule $LifecycleModule `
            -RuntimeRoot $freshRoot `
            -Phase FreshAfterAcl `
            -RuntimeClass FreshExtracted `
            -AclClass RestrictedRuntimeAcl `
            -PostgresVersion $postgresVersion
        Add-BoundaryProbeSet `
            -Probes $probes `
            -LifecycleModule $LifecycleModule `
            -RuntimeRoot ([string]$Paths.RuntimeRoot) `
            -Phase InstalledAfterLifecycle `
            -RuntimeClass Installed `
            -AclClass RestrictedRuntimeAcl `
            -PostgresVersion $postgresVersion

        $decision = Get-OfficePostgresLifecycleBoundaryOutcome -Transitions $transitions -Probes @($probes)
        $fullInitdbStage = 'NotRun'
        $fullInitdbTrials = @()
        $fullInitdbDecision = [pscustomobject]@{ Outcome = 'NotRequired'; IssueCodes = @() }
        if ([string]$decision.Outcome -eq 'FullInitdbInvocationBoundary') {
            $trialList = [Collections.Generic.List[object]]::new()
            try {
                $fullInitdbStage = 'PrepareTrials'
                $fullInitdbStage = 'RunInheritedCwd'
                [void]$trialList.Add((Invoke-BoundaryFullInitdbTrial `
                    -LifecycleModule $LifecycleModule `
                    -PackageManifest $PackageManifest `
                    -Paths $Paths `
                    -TestRoot $testPath `
                    -Mode InheritedCwd `
                    -Sequence 1))
                $fullInitdbStage = 'RunRuntimeBinCwd'
                [void]$trialList.Add((Invoke-BoundaryFullInitdbTrial `
                    -LifecycleModule $LifecycleModule `
                    -PackageManifest $PackageManifest `
                    -Paths $Paths `
                    -TestRoot $testPath `
                    -Mode RuntimeBinCwd `
                    -Sequence 2))
                $fullInitdbStage = 'RunApprovedRuntimeRoots'
                $approvedRuntimeSearchValue = Get-BoundaryApprovedRuntimeSearchValue `
                    -PackageManifest $PackageManifest `
                    -RuntimeRoot ([string]$Paths.RuntimeRoot)
                [void]$trialList.Add((Invoke-BoundaryFullInitdbTrial `
                    -LifecycleModule $LifecycleModule `
                    -PackageManifest $PackageManifest `
                    -Paths $Paths `
                    -TestRoot $testPath `
                    -Mode ApprovedRuntimeRoots `
                    -Sequence 3 `
                    -ApprovedRuntimeSearchValue $approvedRuntimeSearchValue))
                $approvedRuntimeSearchValue = $null
                $fullInitdbStage = 'Classify'
                $fullInitdbTrials = @($trialList)
                $fullInitdbDecision = Get-OfficePostgresFullInitdbBoundaryOutcome -Trials $fullInitdbTrials
                $fullInitdbStage = 'Completed'
            }
            catch {
                $approvedRuntimeSearchValue = $null
                $fullInitdbTrials = @()
                $fullInitdbDecision = [pscustomobject]@{
                    Outcome = 'DiagnosticFailed'
                    IssueCodes = @('FullInitdbDiagnosticInternalFailure')
                }
            }
        }
        $evidence = Copy-BoundaryDictionary -Value $baseEvidence
        $evidence['visualCppTransitions'] = @($transitions)
        $evidence['probes'] = @($probes)
        $evidence['issueCodes'] = @($decision.IssueCodes)
        $evidence['outcome'] = [string]$decision.Outcome
        $evidence['fullInitdbDiagnosticStage'] = $fullInitdbStage
        $evidence['fullInitdbTrials'] = @($fullInitdbTrials)
        $evidence['fullInitdbIssueCodes'] = @($fullInitdbDecision.IssueCodes)
        $evidence['fullInitdbOutcome'] = [string]$fullInitdbDecision.Outcome
        Write-BoundaryEvidence -EvidencePath $evidenceFilePath -Evidence $evidence
        return [pscustomobject]@{
            Outcome = [string]$decision.Outcome
            FullInitdbOutcome = [string]$fullInitdbDecision.Outcome
            EvidenceWritten = $true
        }
    }
    catch {
        $fallback = Copy-BoundaryDictionary -Value $baseEvidence
        $fallback['visualCppTransitions'] = @()
        $fallback['probes'] = @()
        $fallback['issueCodes'] = @('DiagnosticInternalFailure')
        $fallback['outcome'] = 'DiagnosticFailed'
        $fallback['fullInitdbDiagnosticStage'] = 'NotRun'
        $fallback['fullInitdbTrials'] = @()
        $fallback['fullInitdbIssueCodes'] = @()
        $fallback['fullInitdbOutcome'] = 'NotRequired'
        Write-BoundaryEvidence -EvidencePath $evidenceFilePath -Evidence $fallback
        return [pscustomobject]@{
            Outcome = 'DiagnosticFailed'
            FullInitdbOutcome = 'NotRequired'
            EvidenceWritten = $true
        }
    }
}

function Test-BoundaryExactProperties {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string[]]$Expected
    )

    $actual = @($Value.PSObject.Properties.Name | Sort-Object)
    $wanted = @($Expected | Sort-Object)
    return ($actual -join '|') -ceq ($wanted -join '|')
}

function Test-OfficePostgresLifecycleBoundaryEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidencePath,
        [Parameter(Mandatory = $true)][ValidateSet('success', 'failure', 'cancelled', 'skipped')][string]$LifecycleOutcome,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{40}$')][string]$ExpectedSourceRevision,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{64}$')][string]$ExpectedOfficePackageArchiveSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{32}$')][string]$ExpectedInvocationNonce
    )

    $issues = [Collections.Generic.List[string]]::new()
    try {
        if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
            throw 'RunnerUnavailable'
        }
        $runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd('\')
        $evidenceFilePath = [IO.Path]::GetFullPath($EvidencePath)
        if (-not $evidenceFilePath.StartsWith($runnerTemp + '\', [StringComparison]::OrdinalIgnoreCase) -or
            (Split-Path -Leaf $evidenceFilePath) -cne $script:BoundaryEvidenceFileName) {
            throw 'UnsafeEvidencePath'
        }
        if (-not (Test-Path -LiteralPath $evidenceFilePath -PathType Leaf)) {
            if ($LifecycleOutcome -eq 'failure') {
                throw 'MissingFailureEvidence'
            }
            return [pscustomobject]@{ IsValid = $true; IssueCodes = @() }
        }
        if ($LifecycleOutcome -ne 'failure') {
            throw 'UnexpectedBoundaryEvidence'
        }
        $item = Get-Item -LiteralPath $evidenceFilePath
        if ($item.Length -le 0 -or $item.Length -gt 131072) {
            throw 'InvalidEvidenceSize'
        }
        $raw = Get-Content -Raw -LiteralPath $evidenceFilePath
        if ($raw -match '(?i)([A-Z]:\\\\|\\\\\\\\|file://|https?://|RUNNER_TEMP|ProgramFiles|ProgramData|password|passfile|connection|string|stdin|stdout|stderr|argument|commandline|environment|secret)') {
            throw 'ForbiddenEvidenceMaterial'
        }
        $evidence = $raw | ConvertFrom-Json
        if (-not (Test-BoundaryExactProperties -Value $evidence -Expected @(
            'schemaVersion', 'proof', 'recordedAtUtc', 'sourceRevision', 'invocationNonce', 'runner', 'package',
            'lifecycleFailure', 'visualCppTransitions', 'probes', 'issueCodes', 'outcome',
            'fullInitdbDiagnosticStage', 'fullInitdbTrials', 'fullInitdbIssueCodes', 'fullInitdbOutcome'))) {
            throw 'InvalidTopLevelSchema'
        }
        if ([int]$evidence.schemaVersion -ne 1 -or [string]$evidence.proof -cne $script:BoundaryProofName -or
            [string]$evidence.sourceRevision -cne $ExpectedSourceRevision -or
            [string]$evidence.invocationNonce -cne $ExpectedInvocationNonce -or
            [string]$evidence.outcome -notin $script:BoundaryOutcomes -or
            [string]$evidence.fullInitdbDiagnosticStage -notin $script:FullInitdbStages -or
            [string]$evidence.fullInitdbOutcome -notin $script:FullInitdbOutcomes) {
            throw 'InvalidIdentityFields'
        }
        $recordedAt = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse(
            [string]$evidence.recordedAtUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$recordedAt)) {
            throw 'InvalidTimestamp'
        }
        if (-not (Test-BoundaryExactProperties -Value $evidence.runner -Expected @('imageOs', 'imageVersion', 'osVersion')) -or
            @($evidence.runner.PSObject.Properties.Value | Where-Object { [string]$_ -notmatch '^[A-Za-z0-9._-]{1,80}$' }).Count -gt 0) {
            throw 'InvalidRunnerSchema'
        }
        if (-not (Test-BoundaryExactProperties -Value $evidence.package -Expected @(
            'officeArchiveSha256', 'postgresRuntimeSha256', 'postgresVersion')) -or
            [string]$evidence.package.officeArchiveSha256 -cne $ExpectedOfficePackageArchiveSha256.ToUpperInvariant() -or
            [string]$evidence.package.postgresRuntimeSha256 -notmatch '^[A-F0-9]{64}$' -or
            [string]$evidence.package.postgresVersion -notmatch '^[0-9]+\.[0-9]+$') {
            throw 'InvalidPackageSchema'
        }
        if (-not (Test-BoundaryExactProperties -Value $evidence.lifecycleFailure -Expected @(
            'observed', 'executable', 'exitCode', 'exitCodeHex'))) {
            throw 'InvalidFailureSchema'
        }
        if ([bool]$evidence.lifecycleFailure.observed) {
            $failureCode = [int]$evidence.lifecycleFailure.exitCode
            if ([string]$evidence.lifecycleFailure.executable -notin $script:ExecutableNames -or
                [string]$evidence.lifecycleFailure.exitCodeHex -cne (ConvertTo-BoundaryExitCodeHex -ExitCode $failureCode)) {
                throw 'InvalidFailureEvidence'
            }
        }
        elseif ($null -ne $evidence.lifecycleFailure.executable -or $null -ne $evidence.lifecycleFailure.exitCode -or
            $null -ne $evidence.lifecycleFailure.exitCodeHex) {
            throw 'UnexpectedFailureValues'
        }

        $transitions = @($evidence.visualCppTransitions)
        $expectedTransitionSequence = 1
        foreach ($transition in $transitions) {
            if (-not (Test-BoundaryExactProperties -Value $transition -Expected @(
                'sequence', 'minimumVersion', 'versionBefore', 'installerInvoked', 'installerExitCode',
                'installerExitCodeHex', 'rebootRequired', 'versionAfter', 'minimumSatisfied', 'classification')) -or
                [int]$transition.sequence -lt 1 -or
                -not (Test-BoundarySafeVersion -Version ([string]$transition.minimumVersion)) -or
                -not (Test-BoundarySafeVersion -Version $(if ($null -eq $transition.versionBefore) { $null } else { [string]$transition.versionBefore })) -or
                -not (Test-BoundarySafeVersion -Version $(if ($null -eq $transition.versionAfter) { $null } else { [string]$transition.versionAfter })) -or
                [string]$transition.classification -notin $script:TransitionClassifications) {
                throw 'InvalidTransitionEvidence'
            }
            if ([bool]$transition.installerInvoked) {
                $transitionExit = [int]$transition.installerExitCode
                if ([string]$transition.installerExitCodeHex -cne (ConvertTo-BoundaryExitCodeHex -ExitCode $transitionExit)) {
                    throw 'InvalidTransitionExitCode'
                }
            }
            elseif ($null -ne $transition.installerExitCode -or $null -ne $transition.installerExitCodeHex) {
                throw 'UnexpectedTransitionExitCode'
            }
            if (-not (Test-BoundaryTransitionConsistency -Transition $transition)) {
                throw 'InconsistentTransitionClassification'
            }
            if ([int]$transition.sequence -ne $expectedTransitionSequence) {
                throw 'InvalidTransitionSequence'
            }
            $expectedTransitionSequence++
        }

        $probes = @($evidence.probes)
        foreach ($probe in $probes) {
            if (-not (Test-BoundaryExactProperties -Value $probe -Expected @(
                'sequence', 'phase', 'runtimeClass', 'aclClass', 'launchContext', 'executable',
                'completed', 'exitCode', 'exitCodeHex', 'versionMatched', 'issueCode')) -or
                [int]$probe.sequence -lt 1 -or
                [string]$probe.phase -notin $script:PhaseNames -or
                [string]$probe.runtimeClass -notin $script:RuntimeClasses -or
                [string]$probe.aclClass -notin $script:AclClasses -or
                [string]$probe.launchContext -notin $script:LaunchContexts -or
                [string]$probe.executable -notin $script:ExecutableNames) {
                throw 'InvalidProbeEvidence'
            }
            if ([bool]$probe.completed) {
                $probeExit = [int]$probe.exitCode
                if ([string]$probe.exitCodeHex -cne (ConvertTo-BoundaryExitCodeHex -ExitCode $probeExit) -or
                    $null -ne $probe.issueCode -or ($probeExit -ne 0 -and [bool]$probe.versionMatched)) {
                    throw 'InvalidProbeExitCode'
                }
            }
            elseif ($null -ne $probe.exitCode -or $null -ne $probe.exitCodeHex -or
                [bool]$probe.versionMatched -or [string]$probe.issueCode -cne 'ProbeInvocationFailed') {
                throw 'InvalidIncompleteProbe'
            }
        }

        $issueCodes = @($evidence.issueCodes)
        if (@($issueCodes | Where-Object { [string]$_ -notin $script:BoundaryIssueCodes }).Count -gt 0) {
            throw 'InvalidIssueCode'
        }
        if ([string]$evidence.outcome -eq 'DiagnosticFailed') {
            if ($transitions.Count -ne 0 -or $probes.Count -ne 0 -or
                ($issueCodes -join '|') -cne 'DiagnosticInternalFailure' -or
                [string]$evidence.fullInitdbDiagnosticStage -cne 'NotRun' -or
                @($evidence.fullInitdbTrials).Count -ne 0 -or
                @($evidence.fullInitdbIssueCodes).Count -ne 0 -or
                [string]$evidence.fullInitdbOutcome -cne 'NotRequired') {
                throw 'InvalidDiagnosticFailure'
            }
        }
        else {
            if ($LifecycleOutcome -eq 'failure' -and -not [bool]$evidence.lifecycleFailure.observed) {
                throw 'MissingLifecycleFailureClassification'
            }
            if ($transitions.Count -lt 1 -or $probes.Count -ne 12) {
                throw 'IncompleteBoundaryMatrix'
            }
            $expectedSequence = 1
            foreach ($probe in @($probes | Sort-Object sequence)) {
                if ([int]$probe.sequence -ne $expectedSequence) {
                    throw 'InvalidProbeSequence'
                }
                $expectedSequence++
            }
            foreach ($phase in $script:PhaseNames) {
                if (@($probes | Where-Object { $_.phase -eq $phase }).Count -ne 4) {
                    throw 'IncompleteBoundaryPhase'
                }
                foreach ($executable in $script:ExecutableNames) {
                    foreach ($launchContext in $script:LaunchContexts) {
                        $matching = @($probes | Where-Object {
                            $_.phase -eq $phase -and $_.executable -eq $executable -and $_.launchContext -eq $launchContext
                        })
                        if ($matching.Count -ne 1) {
                            throw 'InvalidBoundaryMatrixCombination'
                        }
                        $expectedRuntimeClass = if ($phase -eq 'InstalledAfterLifecycle') { 'Installed' } else { 'FreshExtracted' }
                        $expectedAclClass = if ($phase -eq 'FreshBeforeAcl') { 'InheritedRunnerAcl' } else { 'RestrictedRuntimeAcl' }
                        if ([string]$matching[0].runtimeClass -cne $expectedRuntimeClass -or
                            [string]$matching[0].aclClass -cne $expectedAclClass) {
                            throw 'InvalidBoundaryMatrixSemantics'
                        }
                    }
                }
            }
            $recomputed = Get-OfficePostgresLifecycleBoundaryOutcome -Transitions $transitions -Probes $probes
            if ([string]$evidence.outcome -cne [string]$recomputed.Outcome -or
                (@($issueCodes) -join '|') -cne (@($recomputed.IssueCodes) -join '|')) {
                throw 'BoundaryDecisionMismatch'
            }
        }

        $fullTrials = @($evidence.fullInitdbTrials)
        foreach ($trial in $fullTrials) {
            if (-not (Test-BoundaryExactProperties -Value $trial -Expected @(
                'sequence', 'mode', 'completed', 'exitCode', 'exitCodeHex', 'succeeded', 'issueCode')) -or
                [int]$trial.sequence -lt 1 -or [string]$trial.mode -notin $script:FullInitdbModes) {
                throw 'InvalidFullInitdbTrial'
            }
            if ([bool]$trial.completed) {
                $trialExit = [int]$trial.exitCode
                if ([string]$trial.exitCodeHex -cne (ConvertTo-BoundaryExitCodeHex -ExitCode $trialExit) -or
                    [bool]$trial.succeeded -ne ($trialExit -eq 0) -or $null -ne $trial.issueCode) {
                    throw 'InvalidFullInitdbTrialExitCode'
                }
            }
            elseif ($null -ne $trial.exitCode -or $null -ne $trial.exitCodeHex -or
                [bool]$trial.succeeded -or [string]$trial.issueCode -cne 'InvocationFailed') {
                throw 'InvalidIncompleteFullInitdbTrial'
            }
        }
        $fullIssueCodes = @($evidence.fullInitdbIssueCodes)
        if (@($fullIssueCodes | Where-Object { [string]$_ -notin $script:FullInitdbIssueCodes }).Count -gt 0) {
            throw 'InvalidFullInitdbIssueCode'
        }
        if ([string]$evidence.outcome -cne 'FullInitdbInvocationBoundary') {
            if ([string]$evidence.fullInitdbDiagnosticStage -cne 'NotRun' -or $fullTrials.Count -ne 0 -or
                $fullIssueCodes.Count -ne 0 -or [string]$evidence.fullInitdbOutcome -cne 'NotRequired') {
                throw 'UnexpectedFullInitdbDiagnostic'
            }
        }
        elseif ([string]$evidence.fullInitdbOutcome -eq 'DiagnosticFailed') {
            if ([string]$evidence.fullInitdbDiagnosticStage -in @('NotRun', 'Completed') -or
                $fullTrials.Count -ne 0 -or
                ($fullIssueCodes -join '|') -cne 'FullInitdbDiagnosticInternalFailure') {
                throw 'InvalidFullInitdbDiagnosticFailure'
            }
        }
        else {
            if ([string]$evidence.fullInitdbDiagnosticStage -cne 'Completed' -or $fullTrials.Count -ne 3) {
                throw 'IncompleteFullInitdbComparison'
            }
            $expectedFullSequence = 1
            foreach ($trial in @($fullTrials | Sort-Object sequence)) {
                if ([int]$trial.sequence -ne $expectedFullSequence) {
                    throw 'InvalidFullInitdbTrialSequence'
                }
                $expectedFullSequence++
            }
            foreach ($mode in $script:FullInitdbModes) {
                if (@($fullTrials | Where-Object { $_.mode -eq $mode }).Count -ne 1) {
                    throw 'InvalidFullInitdbTrialMatrix'
                }
            }
            $fullRecomputed = Get-OfficePostgresFullInitdbBoundaryOutcome -Trials $fullTrials
            if ([string]$evidence.fullInitdbOutcome -cne [string]$fullRecomputed.Outcome -or
                ($fullIssueCodes -join '|') -cne (@($fullRecomputed.IssueCodes) -join '|')) {
                throw 'FullInitdbDecisionMismatch'
            }
        }
        return [pscustomobject]@{ IsValid = $true; IssueCodes = @() }
    }
    catch {
        $issues.Add('BoundaryEvidenceRejected')
        return [pscustomobject]@{ IsValid = $false; IssueCodes = @($issues) }
    }
}

Export-ModuleMember -Function @(
    'Invoke-OfficePostgresLifecycleBoundaryDiagnostic',
    'Test-OfficePostgresLifecycleBoundaryEvidence'
)
