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

$emptyListBuilderProof = & $boundaryModule {
    param($Module)

    $originalVersionProbe = ${function:Invoke-BoundaryVersionProbe}
    try {
        function Invoke-BoundaryVersionProbe {
            param(
                [Management.Automation.PSModuleInfo]$LifecycleModule,
                [string]$RuntimeRoot,
                [string]$Phase,
                [string]$RuntimeClass,
                [string]$AclClass,
                [string]$LaunchContext,
                [string]$Executable,
                [string]$PostgresVersion,
                [int]$Sequence
            )

            return [ordered]@{
                sequence = $Sequence
                phase = $Phase
                runtimeClass = $RuntimeClass
                aclClass = $AclClass
                launchContext = $LaunchContext
                executable = $Executable
                completed = $true
                exitCode = 0
                exitCodeHex = '0x00000000'
                versionMatched = $true
                issueCode = $null
            }
        }

        $probeList = [Collections.Generic.List[object]]::new()
        $initialCount = $probeList.Count
        Add-BoundaryProbeSet `
            -Probes $probeList `
            -LifecycleModule $Module `
            -RuntimeRoot 'empty-list-binding-runtime' `
            -Phase FreshBeforeAcl `
            -RuntimeClass FreshExtracted `
            -AclClass InheritedRunnerAcl `
            -PostgresVersion '17.10'
        return [pscustomobject]@{
            InitialCount = $initialCount
            Probes = @($probeList)
        }
    }
    finally {
        Set-Item -Path Function:\Invoke-BoundaryVersionProbe -Value $originalVersionProbe
    }
} $lifecycleModule

$builtProbes = @($emptyListBuilderProof.Probes)
Assert-BoundaryTest -Condition ($emptyListBuilderProof.InitialCount -eq 0) -Message 'The real probe-set binding test did not start empty.'
Assert-BoundaryTest -Condition ($builtProbes.Count -eq 4) -Message 'The real probe-set builder did not add four probes to an empty typed list.'
Assert-BoundaryTest `
    -Condition ((@($builtProbes.sequence) -join ',') -ceq '1,2,3,4') `
    -Message 'The real probe-set builder did not retain the exact probe sequence.'
Assert-BoundaryTest `
    -Condition ((@($builtProbes | ForEach-Object { "$($_.executable)|$($_.launchContext)" }) -join ',') -ceq
        'initdb.exe|InheritedProcess,initdb.exe|RuntimeBin,postgres.exe|InheritedProcess,postgres.exe|RuntimeBin') `
    -Message 'The real probe-set builder did not retain the exact executable and launch-context matrix.'

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

$priorRunnerTemp = $env:RUNNER_TEMP
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "safarsuite-boundary-hermetic-$([Guid]::NewGuid().ToString('N'))"
$markerPath = Join-Path $testRoot '.safarsuite-boundary-hermetic-marker'
try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null
    Set-Content -LiteralPath $markerPath -Value 'owned-boundary-hermetic-root'
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

    $diagnosticFailure = Copy-BoundaryTestDictionary -Value $validEvidence
    $diagnosticFailure.visualCppTransitions = @()
    $diagnosticFailure.probes = @()
    $diagnosticFailure.issueCodes = @('DiagnosticInternalFailure')
    $diagnosticFailure.outcome = 'DiagnosticFailed'
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
Write-Host 'Empty typed probe-set binding: covered'
Write-Host 'VC++ transition classifications: covered'
Write-Host 'Fresh, ACL, working-directory, and installed boundaries: covered'
Write-Host 'Evidence schema and forbidden-material rejection: covered'
Write-Host 'Native failure, cleanup, validation, and upload ordering: covered'
