#Requires -Version 5.1

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$lifecycleModule = Import-Module (Join-Path $PSScriptRoot 'OfficeDatabaseLifecycle.psm1') -Force -PassThru
$boundaryModule = Import-Module (Join-Path $PSScriptRoot 'OfficePostgresLifecycleBoundaryDiagnostic.psm1') -Force -PassThru

function Assert-BoundaryTest {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )
    if (-not $Condition) { throw $Message }
}

function Copy-BoundaryTestDictionary {
    param([Parameter(Mandatory = $true)][Collections.IDictionary]$Value)

    $copy = [ordered]@{}
    foreach ($key in $Value.Keys) {
        $copy[$key] = $Value[$key]
    }
    return $copy
}

function New-BoundaryTestProbe {
    param(
        [Parameter(Mandatory = $true)][int]$Sequence,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string]$LaunchContext,
        [Parameter(Mandatory = $true)][bool]$Ready
    )

    $fresh = $Phase -ne 'InstalledAfterLifecycle'
    return [pscustomobject][ordered]@{
        sequence = $Sequence
        phase = $Phase
        runtimeClass = if ($fresh) { 'FreshExtracted' } else { 'Installed' }
        aclClass = if ($Phase -eq 'FreshBeforeAcl') { 'InheritedRunnerAcl' } else { 'RestrictedRuntimeAcl' }
        launchContext = $LaunchContext
        executable = $Executable
        completed = $true
        exitCode = if ($Ready) { 0 } else { -1073741515 }
        exitCodeHex = if ($Ready) { '0x00000000' } else { '0xC0000135' }
        versionMatched = $Ready
        issueCode = $null
    }
}

function New-BoundaryTestProbeMatrix {
    param(
        [bool]$FreshBeforeReady = $true,
        [bool]$FreshAfterReady = $true,
        [bool]$InstalledReady = $true,
        [string]$WorkingDirectoryDifferencePhase,
        [string]$WorkingDirectoryDifferenceExecutable
    )

    $probes = [Collections.Generic.List[object]]::new()
    foreach ($phase in @('FreshBeforeAcl', 'FreshAfterAcl', 'InstalledAfterLifecycle')) {
        foreach ($executable in @('initdb.exe', 'postgres.exe')) {
            foreach ($launchContext in @('InheritedProcess', 'RuntimeBin')) {
                $ready = switch ($phase) {
                    'FreshBeforeAcl' { $FreshBeforeReady }
                    'FreshAfterAcl' { $FreshAfterReady }
                    default { $InstalledReady }
                }
                if ($phase -eq $WorkingDirectoryDifferencePhase -and
                    $executable -eq $WorkingDirectoryDifferenceExecutable -and
                    $launchContext -eq 'InheritedProcess') {
                    $ready = $false
                }
                [void]$probes.Add((New-BoundaryTestProbe `
                    -Sequence ($probes.Count + 1) `
                    -Phase $phase `
                    -Executable $executable `
                    -LaunchContext $launchContext `
                    -Ready $ready))
            }
        }
    }
    return @($probes)
}

function Get-BoundaryTestDecision {
    param([object[]]$Transitions, [object[]]$Probes)

    return & $boundaryModule {
        param($TransitionValues, $ProbeValues)
        Get-OfficePostgresLifecycleBoundaryOutcome -Transitions $TransitionValues -Probes $ProbeValues
    } $Transitions $Probes
}

function New-BoundaryFullInitdbTrial {
    param(
        [Parameter(Mandatory = $true)][int]$Sequence,
        [Parameter(Mandatory = $true)][string]$Mode,
        [bool]$Succeeded = $false,
        [bool]$Completed = $true,
        [int]$FailureExitCode = -1073741515,
        [bool]$ChildFailureObserved = $false,
        [string]$LastStage = 'Bootstrap'
    )

    return [pscustomobject][ordered]@{
        sequence = $Sequence
        mode = $Mode
        completed = $Completed
        exitCode = if (-not $Completed) { $null } elseif ($Succeeded) { 0 } else { $FailureExitCode }
        exitCodeHex = if (-not $Completed) { $null } elseif ($Succeeded) { '0x00000000' } elseif ($FailureExitCode -eq -1073741515) { '0xC0000135' } else { '0x00000001' }
        succeeded = $Succeeded
        issueCode = if ($Completed) { $null } else { 'InvocationFailed' }
        lastStage = if (-not $Completed) { 'NotObserved' } elseif ($Succeeded) { 'Completed' } else { $LastStage }
        childFailureObserved = if ($Completed -and -not $Succeeded) { $ChildFailureObserved } else { $false }
        childFailureExitCodeHex = if ($Completed -and -not $Succeeded -and $ChildFailureObserved) { '0xC0000135' } else { $null }
    }
}

function Get-BoundaryFullInitdbTestDecision {
    param([object[]]$Trials)

    return & $boundaryModule {
        param($TrialValues)
        Get-OfficePostgresFullInitdbBoundaryOutcome -Trials $TrialValues
    } $Trials
}

function Get-BoundaryInitdbProcessTestDecision {
    param([object[]]$Trials)

    return & $boundaryModule {
        param($TrialValues)
        Get-BoundaryInitdbProcessBoundary -Trials $TrialValues
    } $Trials
}

function New-BoundaryInitdbActivationTrial {
    param(
        [Parameter(Mandatory = $true)][int]$Sequence,
        [Parameter(Mandatory = $true)][string]$Profile,
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [bool]$Completed = $true
    )

    $classification = if (-not $Completed) {
        'InvocationFailed'
    }
    elseif ($ExitCode -eq 0) {
        'NormalSuccess'
    }
    elseif ($ExitCode -eq -1073741515) {
        'DirectLoaderException'
    }
    else {
        'NormalFailure'
    }
    return [pscustomobject][ordered]@{
        sequence = $Sequence
        profile = $Profile
        completed = $Completed
        exitCode = if ($Completed) { $ExitCode } else { $null }
        exitCodeHex = if (-not $Completed) { $null } elseif ($ExitCode -eq 0) { '0x00000000' } elseif ($ExitCode -eq -1073741515) { '0xC0000135' } else { '0x00000001' }
        classification = $classification
    }
}

function Get-BoundaryInitdbActivationTestDecision {
    param([object[]]$Trials)

    return & $boundaryModule {
        param($TrialValues)
        Get-BoundaryInitdbActivationOutcome -Trials $TrialValues
    } $Trials
}

$emptyProbeSet = @(& $boundaryModule {
    param($Module)

    $probes = [Collections.Generic.List[object]]::new()
    Add-BoundaryProbeSet `
        -Probes $probes `
        -LifecycleModule $Module `
        -RuntimeRoot 'boundary-empty-list-runtime' `
        -Phase FreshBeforeAcl `
        -RuntimeClass FreshExtracted `
        -AclClass InheritedRunnerAcl `
        -PostgresVersion '17.10'
    return @($probes)
} $lifecycleModule)
Assert-BoundaryTest `
    -Condition ($emptyProbeSet.Count -eq 4) `
    -Message 'The real probe-set builder did not accept and populate an empty typed list.'
Assert-BoundaryTest `
    -Condition ((@($emptyProbeSet | ForEach-Object { [int]$_.sequence }) -join '|') -ceq '1|2|3|4') `
    -Message 'The real probe-set builder did not retain the exact probe sequence.'
Assert-BoundaryTest `
    -Condition (@($emptyProbeSet | Where-Object { $_.completed -or $_.issueCode -cne 'ProbeInvocationFailed' }).Count -eq 0) `
    -Message 'The missing-runtime regression produced unsafe or unexpected probe evidence.'

$transitionCases = @(
    [pscustomobject]@{ Name = 'AlreadySatisfied'; Before = '14.60.0.0'; ExitCode = $null; After = '14.60.0.0'; Throws = $false },
    [pscustomobject]@{ Name = 'InstalledNoReboot'; Before = $null; ExitCode = 0; After = '14.60.0.0'; Throws = $false },
    [pscustomobject]@{ Name = 'AnotherVersionInstalled'; Before = '14.40.0.0'; ExitCode = 1638; After = '14.60.0.0'; Throws = $false },
    [pscustomobject]@{ Name = 'InstalledRebootRequired'; Before = '14.40.0.0'; ExitCode = 3010; After = '14.60.0.0'; Throws = $false },
    [pscustomobject]@{ Name = 'InstallerFailed'; Before = '14.40.0.0'; ExitCode = 1603; After = '14.40.0.0'; Throws = $true }
)

$transitionResults = & $lifecycleModule {
    param($Cases)

    $originalVersionReader = ${function:Get-OfficeInstalledVisualCppRuntimeVersion}
    $originalNativeCommand = ${function:Invoke-OfficeNativeCommand}
    $existingSignatureFunction = if (Test-Path Function:\Get-AuthenticodeSignature) {
        ${function:Get-AuthenticodeSignature}
    }
    else {
        $null
    }
    $results = [Collections.Generic.List[object]]::new()
    try {
        function Get-OfficeInstalledVisualCppRuntimeVersion {
            if ($script:BoundaryTestVersions.Count -eq 0) {
                throw 'The test version queue is empty.'
            }
            return [Version]$script:BoundaryTestVersions.Dequeue()
        }
        function Get-AuthenticodeSignature {
            param([string]$LiteralPath)
            return [pscustomobject]@{
                Status = 'Valid'
                SignerCertificate = [pscustomobject]@{ Subject = 'CN=Microsoft Corporation' }
            }
        }
        function Invoke-OfficeNativeCommand {
            param(
                [string]$FilePath,
                [string[]]$Arguments,
                [hashtable]$Environment,
                [string]$StandardInput,
                [string]$WorkingDirectory,
                [int]$TimeoutSeconds,
                [switch]$AllowFailure
            )
            $script:BoundaryTestInstallerInvoked = $true
            return [pscustomobject]@{
                ExitCode = [int]$script:BoundaryTestInstallerExitCode
                StandardOutput = 'discarded-output-marker'
                StandardError = 'discarded-error-marker'
            }
        }

        foreach ($case in $Cases) {
            $script:VisualCppRuntimeTransitions.Clear()
            $script:BoundaryTestVersions = [Collections.Generic.Queue[object]]::new()
            $script:BoundaryTestVersions.Enqueue($(if ($null -eq $case.Before) { $null } else { [string]$case.Before }))
            if ($null -ne $case.ExitCode) {
                $script:BoundaryTestVersions.Enqueue([string]$case.After)
            }
            $script:BoundaryTestInstallerExitCode = if ($null -eq $case.ExitCode) { 0 } else { [int]$case.ExitCode }
            $script:BoundaryTestInstallerInvoked = $false
            $context = [pscustomobject]@{
                Distribution = [pscustomobject]@{
                    visualCppRuntime = [pscustomobject]@{
                        minimumVersion = '14.51.36247.0'
                        archiveFileName = 'vc_redist.x64.exe'
                    }
                }
                Paths = [pscustomobject]@{ DatabasePackageDirectory = 'boundary-test-package' }
            }
            $threw = $false
            $pipelineOutput = @()
            try {
                $pipelineOutput = @(Install-OfficeVisualCppRuntime -Context $context)
            }
            catch {
                $threw = $true
            }
            $evidence = @(Get-OfficeVisualCppRuntimeTransitionEvidence)
            [void]$results.Add([pscustomobject]@{
                Name = [string]$case.Name
                Threw = $threw
                InstallerInvoked = [bool]$script:BoundaryTestInstallerInvoked
                PipelineOutputCount = $pipelineOutput.Count
                Evidence = @($evidence)
            })
        }
    }
    finally {
        Set-Item -Path Function:\Get-OfficeInstalledVisualCppRuntimeVersion -Value $originalVersionReader
        Set-Item -Path Function:\Invoke-OfficeNativeCommand -Value $originalNativeCommand
        if ($null -eq $existingSignatureFunction) {
            Remove-Item -Path Function:\Get-AuthenticodeSignature -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -Path Function:\Get-AuthenticodeSignature -Value $existingSignatureFunction
        }
        Remove-Variable -Scope Script -Name BoundaryTestVersions -ErrorAction SilentlyContinue
        Remove-Variable -Scope Script -Name BoundaryTestInstallerExitCode -ErrorAction SilentlyContinue
        Remove-Variable -Scope Script -Name BoundaryTestInstallerInvoked -ErrorAction SilentlyContinue
    }
    return @($results)
} $transitionCases

foreach ($case in $transitionCases) {
    $result = @($transitionResults | Where-Object { $_.Name -eq $case.Name })
    Assert-BoundaryTest -Condition ($result.Count -eq 1) -Message 'A VC++ transition test result is missing.'
    Assert-BoundaryTest -Condition ($result[0].Threw -eq [bool]$case.Throws) -Message 'A VC++ transition used the wrong failure classification.'
    Assert-BoundaryTest -Condition ($result[0].PipelineOutputCount -eq 0) -Message 'VC++ transition diagnostics changed lifecycle pipeline output.'
    Assert-BoundaryTest -Condition (@($result[0].Evidence).Count -eq 1) -Message 'A VC++ transition did not produce one in-memory record.'
    Assert-BoundaryTest -Condition ([string]$result[0].Evidence[0].classification -eq [string]$case.Name) -Message 'A VC++ transition retained the wrong classification.'
    Assert-BoundaryTest -Condition ($result[0].InstallerInvoked -eq ($null -ne $case.ExitCode)) -Message 'A VC++ transition changed the installer decision.'
}
$safeTransitionCopy = @(& $boundaryModule {
    param($Module)
    Get-BoundaryPrerequisiteTransitions -LifecycleModule $Module
} $lifecycleModule)
Assert-BoundaryTest `
    -Condition ($safeTransitionCopy.Count -eq 1 -and $safeTransitionCopy[0].classification -eq 'InstallerFailed') `
    -Message 'The boundary diagnostic could not read the packaged module transition safely.'

$rebootTransition = [pscustomobject]@{ rebootRequired = $true; classification = 'InstalledRebootRequired' }
$normalTransition = [pscustomobject]@{ rebootRequired = $false; classification = 'AlreadySatisfied' }
$decision = Get-BoundaryTestDecision -Transitions @($rebootTransition) -Probes (New-BoundaryTestProbeMatrix -FreshBeforeReady $false)
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'PrerequisiteRebootBoundary') -Message 'The reboot boundary was not classified.'
$decision = Get-BoundaryTestDecision -Transitions @($normalTransition) -Probes (New-BoundaryTestProbeMatrix -FreshBeforeReady $false)
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'PostLifecycleMachineBoundary') -Message 'The post-lifecycle machine boundary was not classified.'
$decision = Get-BoundaryTestDecision -Transitions @($normalTransition) -Probes (New-BoundaryTestProbeMatrix -FreshAfterReady $false)
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'RestrictedAclBoundary') -Message 'The restricted ACL boundary was not classified.'
$decision = Get-BoundaryTestDecision `
    -Transitions @($normalTransition) `
    -Probes (New-BoundaryTestProbeMatrix -WorkingDirectoryDifferencePhase FreshAfterAcl -WorkingDirectoryDifferenceExecutable initdb.exe)
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'WorkingDirectoryBoundary') -Message 'The working-directory boundary was not classified.'
$decision = Get-BoundaryTestDecision `
    -Transitions @($rebootTransition) `
    -Probes (New-BoundaryTestProbeMatrix -WorkingDirectoryDifferencePhase FreshBeforeAcl -WorkingDirectoryDifferenceExecutable initdb.exe)
Assert-BoundaryTest `
    -Condition ($decision.Outcome -eq 'WorkingDirectoryBoundary') `
    -Message 'A fresh-runtime working-directory difference was hidden by the reboot observation.'
$decision = Get-BoundaryTestDecision -Transitions @($normalTransition) -Probes (New-BoundaryTestProbeMatrix -InstalledReady $false)
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'InstalledRuntimeBoundary') -Message 'The installed-runtime boundary was not classified.'
$readyMatrix = New-BoundaryTestProbeMatrix
$decision = Get-BoundaryTestDecision -Transitions @($normalTransition) -Probes $readyMatrix
Assert-BoundaryTest -Condition ($decision.Outcome -eq 'FullInitdbInvocationBoundary') -Message 'The full-initdb boundary was not classified.'
$incompleteMatrix = New-BoundaryTestProbeMatrix
$incompleteMatrix[0].completed = $false
$incompleteMatrix[0].exitCode = $null
$incompleteMatrix[0].exitCodeHex = $null
$incompleteMatrix[0].versionMatched = $false
$incompleteMatrix[0].issueCode = 'ProbeInvocationFailed'
$decision = Get-BoundaryTestDecision -Transitions @($rebootTransition) -Probes $incompleteMatrix
Assert-BoundaryTest `
    -Condition ($decision.Outcome -eq 'Inconclusive') `
    -Message 'An incomplete probe was misclassified as a lifecycle boundary.'
$unexpectedExitMatrix = New-BoundaryTestProbeMatrix
$unexpectedExitMatrix[0].exitCode = 1
$unexpectedExitMatrix[0].exitCodeHex = '0x00000001'
$unexpectedExitMatrix[0].versionMatched = $false
$decision = Get-BoundaryTestDecision -Transitions @($normalTransition) -Probes $unexpectedExitMatrix
Assert-BoundaryTest `
    -Condition ($decision.Outcome -eq 'Inconclusive') `
    -Message 'An unexpected probe exit was misclassified as the loader boundary.'

$failedFullInitdbTrials = @(
    New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd
    New-BoundaryFullInitdbTrial -Sequence 2 -Mode RuntimeBinCwd
    New-BoundaryFullInitdbTrial -Sequence 3 -Mode ApprovedRuntimeRoots
)
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials @(
    New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd -Succeeded $true
    New-BoundaryFullInitdbTrial -Sequence 2 -Mode RuntimeBinCwd
    New-BoundaryFullInitdbTrial -Sequence 3 -Mode ApprovedRuntimeRoots
)
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'BaselineDidNotReproduce') -Message 'A successful baseline full-initdb trial was misclassified.'
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials @(
    New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd
    New-BoundaryFullInitdbTrial -Sequence 2 -Mode RuntimeBinCwd -Succeeded $true
    New-BoundaryFullInitdbTrial -Sequence 3 -Mode ApprovedRuntimeRoots
)
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'WorkingDirectoryBoundary') -Message 'The full-initdb working-directory boundary was not classified.'
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials @(
    New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd
    New-BoundaryFullInitdbTrial -Sequence 2 -Mode RuntimeBinCwd
    New-BoundaryFullInitdbTrial -Sequence 3 -Mode ApprovedRuntimeRoots -Succeeded $true
)
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'ApprovedRuntimeRootsBoundary') -Message 'The approved-runtime-roots boundary was not classified.'
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials $failedFullInitdbTrials
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'ApprovedRuntimeRootsHypothesisDisproved') -Message 'The failed approved-runtime-roots hypothesis was not classified.'
$incompleteFullInitdbTrials = @($failedFullInitdbTrials)
$incompleteFullInitdbTrials[0] = New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd -Completed $false
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials $incompleteFullInitdbTrials
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'Inconclusive') -Message 'An incomplete full-initdb trial was not inconclusive.'
$unexpectedFullInitdbTrials = @($failedFullInitdbTrials)
$unexpectedFullInitdbTrials[0] = New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd -FailureExitCode 1
$fullDecision = Get-BoundaryFullInitdbTestDecision -Trials $unexpectedFullInitdbTrials
Assert-BoundaryTest -Condition ($fullDecision.Outcome -eq 'Inconclusive') -Message 'An unexpected full-initdb exit was not inconclusive.'

$outputClassification = & $boundaryModule {
    Get-BoundaryInitdbOutputClassification `
        -StandardOutput "creating configuration files ... ok`nrunning bootstrap script ..." `
        -StandardError 'child process was terminated by exception 0xc0000135' `
        -ExitCode 1
}
Assert-BoundaryTest `
    -Condition ($outputClassification.lastStage -eq 'Bootstrap' -and
        $outputClassification.childFailureObserved -and
        $outputClassification.childFailureExitCodeHex -eq '0xC0000135') `
    -Message 'The in-memory classifier did not retain the allowlisted spawned-backend signature.'
$classificationJson = $outputClassification | ConvertTo-Json -Compress
Assert-BoundaryTest `
    -Condition ($classificationJson -notmatch '(?i)creating configuration|running bootstrap|child process') `
    -Message 'The in-memory classifier retained raw native output.'
$outputClassification = & $boundaryModule {
    Get-BoundaryInitdbOutputClassification `
        -StandardOutput 'performing post-bootstrap initialization ...' `
        -StandardError $null `
        -ExitCode -1073741515
}
Assert-BoundaryTest `
    -Condition ($outputClassification.lastStage -eq 'PostBootstrap' -and
        -not $outputClassification.childFailureObserved -and
        $null -eq $outputClassification.childFailureExitCodeHex) `
    -Message 'The in-memory classifier invented a spawned-backend failure.'

$processDecision = Get-BoundaryInitdbProcessTestDecision -Trials $failedFullInitdbTrials
Assert-BoundaryTest `
    -Condition ($processDecision.Outcome -eq 'TopLevelInitdb' -and $processDecision.LastStage -eq 'Bootstrap') `
    -Message 'The direct initdb exception boundary was not classified.'
$spawnedBackendTrials = @(
    New-BoundaryFullInitdbTrial -Sequence 1 -Mode InheritedCwd -FailureExitCode 1 -ChildFailureObserved $true -LastStage Bootstrap
    New-BoundaryFullInitdbTrial -Sequence 2 -Mode RuntimeBinCwd
    New-BoundaryFullInitdbTrial -Sequence 3 -Mode ApprovedRuntimeRoots
)
$processDecision = Get-BoundaryInitdbProcessTestDecision -Trials $spawnedBackendTrials
Assert-BoundaryTest `
    -Condition ($processDecision.Outcome -eq 'SpawnedBackend' -and $processDecision.LastStage -eq 'Bootstrap') `
    -Message 'The spawned-backend exception boundary was not classified.'
$processDecision = Get-BoundaryInitdbProcessTestDecision -Trials $unexpectedFullInitdbTrials
Assert-BoundaryTest -Condition ($processDecision.Outcome -eq 'Inconclusive') -Message 'An unrecognized process failure was not inconclusive.'

$generalActivationTrials = @(
    New-BoundaryInitdbActivationTrial -Sequence 1 -Profile HelpOnly -ExitCode 0
    New-BoundaryInitdbActivationTrial -Sequence 2 -Profile MissingDataValidation -ExitCode 1
    New-BoundaryInitdbActivationTrial -Sequence 3 -Profile MinimalTrust -ExitCode -1073741515
    New-BoundaryInitdbActivationTrial -Sequence 4 -Profile ExactScram -ExitCode -1073741515
)
$activationDecision = Get-BoundaryInitdbActivationTestDecision -Trials $generalActivationTrials
Assert-BoundaryTest -Condition ($activationDecision -eq 'GeneralClusterActivationBoundary') -Message 'The general cluster-activation boundary was not classified.'
$authenticationActivationTrials = @(
    New-BoundaryInitdbActivationTrial -Sequence 1 -Profile HelpOnly -ExitCode 0
    New-BoundaryInitdbActivationTrial -Sequence 2 -Profile MissingDataValidation -ExitCode 1
    New-BoundaryInitdbActivationTrial -Sequence 3 -Profile MinimalTrust -ExitCode 0
    New-BoundaryInitdbActivationTrial -Sequence 4 -Profile ExactScram -ExitCode -1073741515
)
$activationDecision = Get-BoundaryInitdbActivationTestDecision -Trials $authenticationActivationTrials
Assert-BoundaryTest -Condition ($activationDecision -eq 'AuthenticationBoundary') -Message 'The authentication activation boundary was not classified.'
$preActivationTrials = @($generalActivationTrials)
$preActivationTrials[0] = New-BoundaryInitdbActivationTrial -Sequence 1 -Profile HelpOnly -ExitCode -1073741515
$activationDecision = Get-BoundaryInitdbActivationTestDecision -Trials $preActivationTrials
Assert-BoundaryTest -Condition ($activationDecision -eq 'PreActivationBoundary') -Message 'The pre-activation boundary was not classified.'
$incompleteActivationTrials = @($generalActivationTrials)
$incompleteActivationTrials[0] = New-BoundaryInitdbActivationTrial -Sequence 1 -Profile HelpOnly -ExitCode 0 -Completed $false
$activationDecision = Get-BoundaryInitdbActivationTestDecision -Trials $incompleteActivationTrials
Assert-BoundaryTest -Condition ($activationDecision -eq 'Inconclusive') -Message 'An incomplete activation ladder was not inconclusive.'

$priorRunnerTemp = $env:RUNNER_TEMP
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "safarsuite-boundary-hermetic-$([Guid]::NewGuid().ToString('N'))"
$markerPath = Join-Path $testRoot '.safarsuite-boundary-hermetic-marker'
try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    Set-Content -LiteralPath $markerPath -Value 'owned-boundary-hermetic-root'
    $approvedRuntimeRoot = Join-Path $testRoot 'approved-runtime'
    $approvedRuntimeBin = Join-Path $approvedRuntimeRoot 'bin'
    $approvedRuntimeLib = Join-Path $approvedRuntimeRoot 'lib'
    New-Item -ItemType Directory -Path $approvedRuntimeBin | Out-Null
    New-Item -ItemType Directory -Path $approvedRuntimeLib | Out-Null
    $approvedRuntimeManifest = [pscustomobject]@{
        postgresql = [pscustomobject]@{
            adminRole = 'safarsuite_admin'
            runtimeFileSha256 = [pscustomobject]@{
                'bin/initdb.exe' = ('A' * 64 -join '')
                'lib/libpq.dll' = ('B' * 64 -join '')
            }
        }
    }
    $approvedRuntimeSearchValue = & $boundaryModule {
        param($Manifest, $RuntimeRoot)
        Get-BoundaryApprovedRuntimeSearchValue -PackageManifest $Manifest -RuntimeRoot $RuntimeRoot
    } $approvedRuntimeManifest $approvedRuntimeRoot
    $approvedRuntimeSearchRoots = @($approvedRuntimeSearchValue -split ';')
    Assert-BoundaryTest `
        -Condition ($approvedRuntimeSearchRoots.Count -eq 4 -and
            $approvedRuntimeSearchRoots[0] -ceq [IO.Path]::GetFullPath($approvedRuntimeBin) -and
            $approvedRuntimeSearchRoots[1] -ceq [IO.Path]::GetFullPath($approvedRuntimeLib)) `
        -Message 'The approved runtime search value did not retain the finite validated roots.'
    $capturedTrial = & $lifecycleModule {
        param($BoundaryModule, $Manifest, $RuntimeRoot, $TrialRoot)

        $originalPassword = ${function:New-OfficeDatabasePassword}
        $originalAcl = ${function:Set-OfficeRestrictedAcl}
        $originalContent = ${function:Set-OfficeUtf8NoBomContent}
        $originalNative = ${function:Invoke-OfficeNativeCommand}
        $moduleInfo = $ExecutionContext.SessionState.Module
        try {
            function New-OfficeDatabasePassword { return 'ephemeral-test-value' }
            function Set-OfficeRestrictedAcl {
                param([string]$Path, [string]$ServiceSid, [string]$Profile)
            }
            function Set-OfficeUtf8NoBomContent {
                param([string]$Path, [string]$Value)
                [IO.File]::WriteAllText($Path, $Value, [Text.UTF8Encoding]::new($false))
            }
            function Invoke-OfficeNativeCommand {
                param(
                    [string]$FilePath,
                    [string[]]$Arguments,
                    [hashtable]$Environment,
                    [string]$StandardInput,
                    [string]$WorkingDirectory,
                    [int]$TimeoutSeconds,
                    [switch]$AllowFailure
                )
                return [pscustomobject]@{
                    ExitCode = -1073741515
                    StandardOutput = "creating configuration files ... ok`nrunning bootstrap script ..."
                    StandardError = $null
                }
            }
            return & $BoundaryModule {
                param($Module, $PackageManifest, $Paths, $Root)
                $fullTrial = Invoke-BoundaryFullInitdbTrial `
                    -LifecycleModule $Module `
                    -PackageManifest $PackageManifest `
                    -Paths $Paths `
                    -TestRoot $Root `
                    -Mode InheritedCwd `
                    -Sequence 1
                $activationTrial = Invoke-BoundaryInitdbActivationTrial `
                    -LifecycleModule $Module `
                    -PackageManifest $PackageManifest `
                    -Paths $Paths `
                    -TestRoot $Root `
                    -Profile ExactScram `
                    -Sequence 4
                return [pscustomobject]@{ Full = $fullTrial; Activation = $activationTrial }
            } $moduleInfo $Manifest ([pscustomobject]@{ RuntimeRoot = $RuntimeRoot }) $TrialRoot
        }
        finally {
            Set-Item -Path Function:\New-OfficeDatabasePassword -Value $originalPassword
            Set-Item -Path Function:\Set-OfficeRestrictedAcl -Value $originalAcl
            Set-Item -Path Function:\Set-OfficeUtf8NoBomContent -Value $originalContent
            Set-Item -Path Function:\Invoke-OfficeNativeCommand -Value $originalNative
        }
    } $boundaryModule $approvedRuntimeManifest $approvedRuntimeRoot $testRoot
    Assert-BoundaryTest `
        -Condition ($capturedTrial.Full.completed -and $capturedTrial.Full.exitCodeHex -eq '0xC0000135' -and
            $capturedTrial.Full.lastStage -eq 'Bootstrap' -and -not $capturedTrial.Full.childFailureObserved) `
        -Message 'The real full-initdb trial did not reduce native output to the finite classification.'
    Assert-BoundaryTest `
        -Condition ($capturedTrial.Activation.completed -and
            $capturedTrial.Activation.profile -eq 'ExactScram' -and
            $capturedTrial.Activation.classification -eq 'DirectLoaderException') `
        -Message 'The real activation trial did not retain the finite loader classification.'
    Assert-BoundaryTest `
        -Condition (($capturedTrial | ConvertTo-Json -Compress) -notmatch '(?i)creating configuration|running bootstrap|ephemeral-test-value') `
        -Message 'The real full-initdb trial retained raw output or bootstrap material.'
    $env:RUNNER_TEMP = $testRoot
    $evidencePath = Join-Path $testRoot 'runtime-stage-boundary.json'
    $expectedSourceRevision = ('a' * 40 -join '')
    $expectedOfficeArchiveSha256 = ('A' * 64 -join '')
    $expectedInvocationNonce = ('1' * 32 -join '')
    $validationArguments = @{
        EvidencePath = $evidencePath
        ExpectedSourceRevision = $expectedSourceRevision
        ExpectedOfficePackageArchiveSha256 = $expectedOfficeArchiveSha256
        ExpectedInvocationNonce = $expectedInvocationNonce
    }
    $validEvidence = [ordered]@{
        schemaVersion = 1
        proof = 'safarsuite-office-postgresql-lifecycle-boundary'
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        sourceRevision = $expectedSourceRevision
        invocationNonce = $expectedInvocationNonce
        runner = [ordered]@{ imageOs = 'win22'; imageVersion = 'test.1'; osVersion = '10.0.20348.0' }
        package = [ordered]@{
            officeArchiveSha256 = $expectedOfficeArchiveSha256
            postgresRuntimeSha256 = ('B' * 64 -join '')
            postgresVersion = '17.10'
        }
        lifecycleFailure = [ordered]@{
            observed = $true
            executable = 'initdb.exe'
            exitCode = -1073741515
            exitCodeHex = '0xC0000135'
        }
        visualCppTransitions = @([ordered]@{
            sequence = 1
            minimumVersion = '14.51.36247.0'
            versionBefore = '14.60.0.0'
            installerInvoked = $false
            installerExitCode = $null
            installerExitCodeHex = $null
            rebootRequired = $false
            versionAfter = '14.60.0.0'
            minimumSatisfied = $true
            classification = 'AlreadySatisfied'
        })
        probes = @($readyMatrix)
        issueCodes = @('VersionProbesPassButLifecycleFails')
        outcome = 'FullInitdbInvocationBoundary'
        fullInitdbDiagnosticStage = 'Completed'
        fullInitdbTrials = @($failedFullInitdbTrials)
        fullInitdbIssueCodes = @('ApprovedRuntimeRootsDidNotChangeResult')
        fullInitdbOutcome = 'ApprovedRuntimeRootsHypothesisDisproved'
        initdbProcessBoundary = 'TopLevelInitdb'
        initdbLastStage = 'Bootstrap'
        initdbActivationStage = 'Completed'
        initdbActivationTrials = @($generalActivationTrials)
        initdbActivationOutcome = 'GeneralClusterActivationBoundary'
    }
    & $boundaryModule {
        param($Path, $Evidence)
        Write-BoundaryEvidence -EvidencePath $Path -Evidence $Evidence
    } $evidencePath $validEvidence
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'Valid boundary evidence was rejected.'

    $staleEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $staleEvidence.invocationNonce = ('2' * 32 -join '')
    $staleJson = $staleEvidence | ConvertTo-Json -Depth 10
    [IO.File]::WriteAllText($evidencePath, $staleJson, [Text.UTF8Encoding]::new($false))
    $injectedFailureObserved = $false
    try {
        & $boundaryModule {
            param($Path, $Evidence)
            Write-BoundaryEvidence -EvidencePath $Path -Evidence $Evidence -BeforePublish {
                throw 'InjectedBeforeAtomicPublish'
            }
        } $evidencePath $validEvidence
    }
    catch {
        $injectedFailureObserved = $true
    }
    Assert-BoundaryTest -Condition $injectedFailureObserved -Message 'The interrupted evidence publication test did not fail.'
    Assert-BoundaryTest `
        -Condition ((Get-Content -Raw -LiteralPath $evidencePath) -ceq $staleJson) `
        -Message 'Interrupted evidence publication changed the existing final artifact.'
    Assert-BoundaryTest `
        -Condition (@(Get-ChildItem -LiteralPath $testRoot -File | Where-Object {
            $_.Name -match '^\.runtime-stage-boundary\.json\.[0-9a-f]{32}\.(?:tmp|bak)$'
        }).Count -eq 0) `
        -Message 'Interrupted evidence publication left a transient artifact.'
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Stale invocation evidence was accepted.'

    & $boundaryModule {
        param($Path, $Evidence)
        Write-BoundaryEvidence -EvidencePath $Path -Evidence $Evidence
    } $evidencePath $validEvidence
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'Atomic evidence replacement was rejected.'
    Assert-BoundaryTest `
        -Condition (@(Get-ChildItem -LiteralPath $testRoot -File | Where-Object {
            $_.Name -match '^\.runtime-stage-boundary\.json\.[0-9a-f]{32}\.(?:tmp|bak)$'
        }).Count -eq 0) `
        -Message 'Atomic evidence replacement left a transient artifact.'

    $validation = Test-OfficePostgresLifecycleBoundaryEvidence `
        -EvidencePath $evidencePath `
        -LifecycleOutcome failure `
        -ExpectedSourceRevision ('b' * 40 -join '') `
        -ExpectedOfficePackageArchiveSha256 $expectedOfficeArchiveSha256 `
        -ExpectedInvocationNonce $expectedInvocationNonce
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence from another source revision was accepted.'
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence `
        -EvidencePath $evidencePath `
        -LifecycleOutcome failure `
        -ExpectedSourceRevision $expectedSourceRevision `
        -ExpectedOfficePackageArchiveSha256 ('C' * 64 -join '') `
        -ExpectedInvocationNonce $expectedInvocationNonce
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence from another office archive was accepted.'

    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome success
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Failure evidence was accepted for a successful lifecycle.'

    $decisionMismatchEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $decisionMismatchEvidence.outcome = 'RestrictedAclBoundary'
    $decisionMismatchEvidence.issueCodes = @('AclChangedLoaderResult')
    [IO.File]::WriteAllText($evidencePath, ($decisionMismatchEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a false boundary decision was accepted.'

    $duplicateMatrixEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $duplicateMatrix = New-BoundaryTestProbeMatrix
    $duplicateMatrix[1].launchContext = 'InheritedProcess'
    $duplicateMatrixEvidence.probes = @($duplicateMatrix)
    [IO.File]::WriteAllText($evidencePath, ($duplicateMatrixEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a duplicate matrix combination was accepted.'

    $falseFullDecisionEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $falseFullDecisionEvidence.fullInitdbOutcome = 'ApprovedRuntimeRootsBoundary'
    $falseFullDecisionEvidence.fullInitdbIssueCodes = @('ApprovedRuntimeRootsChangedResult')
    [IO.File]::WriteAllText($evidencePath, ($falseFullDecisionEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a false full-initdb decision was accepted.'

    $falseProcessDecisionEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $falseProcessDecisionEvidence.initdbProcessBoundary = 'SpawnedBackend'
    [IO.File]::WriteAllText($evidencePath, ($falseProcessDecisionEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a false initdb process decision was accepted.'

    $forgedChildEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $forgedChildTrials = @($failedFullInitdbTrials)
    $forgedChildTrials[0] = New-BoundaryFullInitdbTrial `
        -Sequence 1 `
        -Mode InheritedCwd `
        -ChildFailureObserved $true `
        -LastStage Bootstrap
    $forgedChildTrials[0].childFailureExitCodeHex = $null
    $forgedChildEvidence.fullInitdbTrials = @($forgedChildTrials)
    $forgedChildEvidence.initdbProcessBoundary = 'SpawnedBackend'
    [IO.File]::WriteAllText($evidencePath, ($forgedChildEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with an unbound child exception was accepted.'

    $falseActivationDecisionEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $falseActivationDecisionEvidence.initdbActivationOutcome = 'AuthenticationBoundary'
    [IO.File]::WriteAllText($evidencePath, ($falseActivationDecisionEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a false activation decision was accepted.'

    $duplicateFullMatrixEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $duplicateFullMatrix = @($failedFullInitdbTrials)
    $duplicateFullMatrix[2] = New-BoundaryFullInitdbTrial -Sequence 3 -Mode RuntimeBinCwd
    $duplicateFullMatrixEvidence.fullInitdbTrials = @($duplicateFullMatrix)
    [IO.File]::WriteAllText($evidencePath, ($duplicateFullMatrixEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a duplicate full-initdb matrix combination was accepted.'

    $unsafeFullEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $unsafeFullEvidence.fullInitdbTrials = @([ordered]@{
        sequence = 1
        mode = 'InheritedCwd'
        completed = $false
        exitCode = $null
        exitCodeHex = $null
        succeeded = $false
        issueCode = 'InvocationFailed'
        secret = 'forbidden-marker'
    })
    [IO.File]::WriteAllText($evidencePath, ($unsafeFullEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Full-initdb evidence with forbidden material was accepted.'

    $unsafeEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $unsafeEvidence['password'] = 'randomized-sensitive-marker'
    [IO.File]::WriteAllText($evidencePath, ($unsafeEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence with a forbidden key was accepted.'

    $pathEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $pathEvidence.runner = [ordered]@{ imageOs = 'win22'; imageVersion = 'C:\unsafe-marker'; osVersion = '10.0.20348.0' }
    [IO.File]::WriteAllText($evidencePath, ($pathEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Evidence containing a full path was accepted.'

    $inconsistentTransitionEvidence = Copy-BoundaryTestDictionary -Value $validEvidence
    $inconsistentTransitionEvidence.visualCppTransitions = @([ordered]@{
        sequence = 1
        minimumVersion = '14.51.36247.0'
        versionBefore = '14.60.0.0'
        installerInvoked = $false
        installerExitCode = $null
        installerExitCodeHex = $null
        rebootRequired = $true
        versionAfter = '14.60.0.0'
        minimumSatisfied = $true
        classification = 'AlreadySatisfied'
    })
    [IO.File]::WriteAllText($evidencePath, ($inconsistentTransitionEvidence | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Inconsistent prerequisite evidence was accepted.'

    $activationDiagnosticFailure = Copy-BoundaryTestDictionary -Value $validEvidence
    $activationDiagnosticFailure.initdbActivationStage = 'RunMinimalTrust'
    $activationDiagnosticFailure.initdbActivationTrials = @()
    $activationDiagnosticFailure.initdbActivationOutcome = 'DiagnosticFailed'
    [IO.File]::WriteAllText($evidencePath, ($activationDiagnosticFailure | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'A safe activation diagnostic-failure record was rejected.'

    $fullDiagnosticFailure = Copy-BoundaryTestDictionary -Value $validEvidence
    $fullDiagnosticFailure.fullInitdbDiagnosticStage = 'RunApprovedRuntimeRoots'
    $fullDiagnosticFailure.fullInitdbTrials = @()
    $fullDiagnosticFailure.fullInitdbIssueCodes = @('FullInitdbDiagnosticInternalFailure')
    $fullDiagnosticFailure.fullInitdbOutcome = 'DiagnosticFailed'
    $fullDiagnosticFailure.initdbProcessBoundary = 'DiagnosticFailed'
    $fullDiagnosticFailure.initdbLastStage = 'NotObserved'
    $fullDiagnosticFailure.initdbActivationStage = 'NotRun'
    $fullDiagnosticFailure.initdbActivationTrials = @()
    $fullDiagnosticFailure.initdbActivationOutcome = 'NotRequired'
    [IO.File]::WriteAllText($evidencePath, ($fullDiagnosticFailure | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'A safe full-initdb diagnostic-failure record was rejected.'

    $diagnosticFailure = Copy-BoundaryTestDictionary -Value $validEvidence
    $diagnosticFailure.visualCppTransitions = @()
    $diagnosticFailure.probes = @()
    $diagnosticFailure.issueCodes = @('DiagnosticInternalFailure')
    $diagnosticFailure.outcome = 'DiagnosticFailed'
    $diagnosticFailure.fullInitdbDiagnosticStage = 'NotRun'
    $diagnosticFailure.fullInitdbTrials = @()
    $diagnosticFailure.fullInitdbIssueCodes = @()
    $diagnosticFailure.fullInitdbOutcome = 'NotRequired'
    $diagnosticFailure.initdbProcessBoundary = 'NotRequired'
    $diagnosticFailure.initdbLastStage = 'NotObserved'
    $diagnosticFailure.initdbActivationStage = 'NotRun'
    $diagnosticFailure.initdbActivationTrials = @()
    $diagnosticFailure.initdbActivationOutcome = 'NotRequired'
    [IO.File]::WriteAllText($evidencePath, ($diagnosticFailure | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'A safe diagnostic-failure record was rejected.'

    Remove-Item -LiteralPath $evidencePath -Force
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome failure
    Assert-BoundaryTest -Condition (-not $validation.IsValid) -Message 'Missing failure evidence was accepted.'
    $validation = Test-OfficePostgresLifecycleBoundaryEvidence @validationArguments -LifecycleOutcome success
    Assert-BoundaryTest -Condition $validation.IsValid -Message 'A successful lifecycle incorrectly required failure evidence.'
}
finally {
    $env:RUNNER_TEMP = $priorRunnerTemp
    if (Test-Path -LiteralPath $testRoot) {
        $resolvedRoot = (Resolve-Path -LiteralPath $testRoot).Path
        $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
        if (-not $resolvedRoot.StartsWith($tempRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
            throw 'Boundary hermetic cleanup safety check failed.'
        }
        Remove-Item -LiteralPath $resolvedRoot -Recurse -Force
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$workflowText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot '.github\workflows\ci.yml')
$nativeStepIndex = $workflowText.IndexOf('id: native_database_lifecycle', [StringComparison]::Ordinal)
$cleanupStepIndex = $workflowText.IndexOf('name: Clean disposable native database proof', [StringComparison]::Ordinal)
$validationStepIndex = $workflowText.IndexOf('id: database_boundary_evidence_validation', [StringComparison]::Ordinal)
$uploadStepIndex = $workflowText.IndexOf('name: Upload native database lifecycle evidence', [StringComparison]::Ordinal)
Assert-BoundaryTest `
    -Condition ($nativeStepIndex -ge 0 -and $cleanupStepIndex -gt $nativeStepIndex -and
        $validationStepIndex -gt $cleanupStepIndex -and $uploadStepIndex -gt $validationStepIndex) `
    -Message 'The lifecycle diagnostic, cleanup, validation, and upload steps are out of order.'
Assert-BoundaryTest `
    -Condition ($workflowText -match "steps\.database_boundary_evidence_validation\.outcome == 'success'") `
    -Message 'Database evidence upload is not gated by the safety validator.'
Assert-BoundaryTest `
    -Condition ($workflowText -notmatch '(?m)^\s*continue-on-error\s*:') `
    -Message 'A workflow step can hide the native lifecycle failure.'

Write-Host 'Office PostgreSQL lifecycle boundary diagnostics hermetic proof passed.'
Write-Host 'VC++ transition classifications: covered'
Write-Host 'Fresh, ACL, working-directory, and installed boundaries: covered'
Write-Host 'Full initdb working-directory and approved-runtime-roots comparison: covered'
Write-Host 'In-memory initdb stage and process-boundary classification: covered'
Write-Host 'Finite initdb activation ladder and decision recomputation: covered'
Write-Host 'Evidence schema and forbidden-material rejection: covered'
Write-Host 'Native failure, cleanup, validation, and upload ordering: covered'
