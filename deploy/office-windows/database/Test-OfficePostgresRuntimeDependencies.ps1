[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageDirectory,
    [Parameter(Mandatory = $true)][string]$OfficePackageArchivePath,
    [Parameter(Mandatory = $true)][string]$OfficePackageArchiveSha256,
    [Parameter(Mandatory = $true)][string]$ProbeRoot,
    [Parameter(Mandatory = $true)][string]$EvidencePath,
    [string]$DependencyToolPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function ConvertTo-ProbeWindowsCommandLineArgument {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value)

    if ($Value.Length -gt 0 -and $Value -notmatch '[\s"]') {
        return $Value
    }

    $builder = [Text.StringBuilder]::new()
    [void]$builder.Append([char]34)
    $backslashCount = 0
    foreach ($character in $Value.ToCharArray()) {
        if ($character -eq '\') {
            $backslashCount++
            continue
        }
        if ($character -eq [char]34) {
            [void]$builder.Append(('\' * (($backslashCount * 2) + 1)))
            [void]$builder.Append([char]34)
            $backslashCount = 0
            continue
        }
        if ($backslashCount -gt 0) {
            [void]$builder.Append(('\' * $backslashCount))
            $backslashCount = 0
        }
        [void]$builder.Append($character)
    }
    if ($backslashCount -gt 0) {
        [void]$builder.Append(('\' * ($backslashCount * 2)))
    }
    [void]$builder.Append([char]34)
    return $builder.ToString()
}

function ConvertTo-ProbeExitCodeHex {
    param([Parameter(Mandatory = $true)][int]$ExitCode)

    $unsigned = [BitConverter]::ToUInt32([BitConverter]::GetBytes($ExitCode), 0)
    return "0x$($unsigned.ToString('X8'))"
}

function Invoke-ProbeNativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory,
        [int]$TimeoutSeconds = 30
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = (($Arguments | ForEach-Object {
        ConvertTo-ProbeWindowsCommandLineArgument -Value ([string]$_)
    }) -join ' ')
    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $startInfo.WorkingDirectory = $WorkingDirectory
    }

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw "A dependency-probe process could not start."
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill()
            $process.WaitForExit()
            throw "A dependency-probe process timed out."
        }
        return [pscustomobject]@{
            ExitCode = [int]$process.ExitCode
            StandardOutput = $stdoutTask.Result
            StandardError = $stderrTask.Result
        }
    }
    finally {
        $process.Dispose()
    }
}

function Get-UniqueArchiveEntry {
    param(
        [Parameter(Mandatory = $true)][IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $matches = @($Archive.Entries | Where-Object {
        $normalizedEntryPath = ([string]$_.FullName).Replace('\', '/')
        [string]::Equals($normalizedEntryPath, $ExpectedPath, [StringComparison]::OrdinalIgnoreCase)
    })
    if ($matches.Count -ne 1) {
        throw "The sealed package does not contain exactly one required database entry."
    }
    return $matches[0]
}

function Get-ProbeDependencyTool {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = (Resolve-Path -LiteralPath $RequestedPath -ErrorAction Stop).Path
        if ([IO.Path]::GetFileName($resolved) -ine 'dumpbin.exe' -or
            -not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "The dependency inspection tool must be dumpbin.exe."
        }
        return $resolved
    }

    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    $vswherePath = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswherePath -PathType Leaf)) {
        throw "The dependency inspection tool locator is unavailable."
    }
    $locator = Invoke-ProbeNativeCommand `
        -FilePath $vswherePath `
        -Arguments @(
            '-latest', '-products', '*',
            '-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64',
            '-find', 'VC\Tools\MSVC\**\bin\Hostx64\x64\dumpbin.exe'
        )
    if ($locator.ExitCode -ne 0) {
        throw "The dependency inspection tool locator failed with exit code $($locator.ExitCode)."
    }
    $candidates = @($locator.StandardOutput -split "`r?`n" | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_.Trim() -PathType Leaf)
    })
    if ($candidates.Count -lt 1) {
        throw "The dependency inspection tool is unavailable."
    }
    return $candidates[0].Trim()
}

function Get-ProbeImageDependencies {
    param(
        [Parameter(Mandatory = $true)][string]$ToolPath,
        [Parameter(Mandatory = $true)][string]$ImagePath
    )

    $toolResult = Invoke-ProbeNativeCommand `
        -FilePath $ToolPath `
        -Arguments @('/NOLOGO', '/DEPENDENTS', $ImagePath)
    $dependencies = [Collections.Generic.List[object]]::new()
    if ($toolResult.ExitCode -ne 0) {
        return [pscustomobject]@{
            ExitCode = $toolResult.ExitCode
            Dependencies = @()
            HeaderSeen = $false
            FileTypeSeen = $false
            SummarySeen = $false
            RejectedEntryCount = 0
            ParseSucceeded = $false
        }
    }

    $mode = $null
    $headerSeen = $false
    $fileTypeSeen = $false
    $summarySeen = $false
    $rejectedEntryCount = 0
    foreach ($line in @($toolResult.StandardOutput -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -match '^File Type: (?i:EXECUTABLE IMAGE|DLL|DRIVER)$') {
            $fileTypeSeen = $true
            continue
        }
        if ($trimmed -eq 'Image has the following dependencies:') {
            $mode = 'Direct'
            $headerSeen = $true
            continue
        }
        if ($trimmed -eq 'Image has the following delay load dependencies:') {
            $mode = 'DelayLoad'
            $headerSeen = $true
            continue
        }
        if ($trimmed -eq 'Summary') {
            $summarySeen = $true
            $mode = $null
            continue
        }
        if ($null -eq $mode -or [string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }
        if ($trimmed -notmatch '^[A-Za-z0-9][A-Za-z0-9._+-]*\.(?i:dll|drv)$' -or
            [IO.Path]::GetFileName($trimmed) -cne $trimmed -or
            $trimmed.IndexOfAny(@([char]'\', [char]'/', [char]':')) -ge 0) {
            $rejectedEntryCount++
            continue
        }
        $dependencies.Add([pscustomobject]@{
            Name = $trimmed
            DelayLoad = $mode -eq 'DelayLoad'
        })
    }

    return [pscustomobject]@{
        ExitCode = $toolResult.ExitCode
        Dependencies = @($dependencies | Sort-Object Name, DelayLoad -Unique)
        HeaderSeen = $headerSeen
        FileTypeSeen = $fileTypeSeen
        SummarySeen = $summarySeen
        RejectedEntryCount = $rejectedEntryCount
        ParseSucceeded = $fileTypeSeen -and $summarySeen -and $rejectedEntryCount -eq 0
    }
}

function Get-ProbeKnownDllNames {
    $known = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $registryPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs'
    $values = Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue
    if ($null -eq $values) {
        return ,$known
    }
    foreach ($property in @($values.PSObject.Properties)) {
        if ($property.Name -in @('PSPath', 'PSParentPath', 'PSChildName', 'PSDrive', 'PSProvider')) {
            continue
        }
        if ($property.Name -match '^[A-Za-z0-9][A-Za-z0-9._+-]*\.(?i:dll|drv)$') {
            [void]$known.Add($property.Name)
        }
        $valueLeaf = [IO.Path]::GetFileName([string]$property.Value)
        if ($valueLeaf -match '^[A-Za-z0-9][A-Za-z0-9._+-]*\.(?i:dll|drv)$') {
            [void]$known.Add($valueLeaf)
        }
    }
    return ,$known
}

function Write-ProbeEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    $parent = Split-Path -Parent ([IO.Path]::GetFullPath($Path))
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    [IO.File]::WriteAllText(
        [IO.Path]::GetFullPath($Path),
        ($Value | ConvertTo-Json -Depth 10),
        [Text.UTF8Encoding]::new($false))
}

function Copy-ProbeArchiveEntry {
    param(
        [Parameter(Mandatory = $true)][IO.Compression.ZipArchiveEntry]$Entry,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    $inputStream = $Entry.Open()
    try {
        $outputStream = [IO.File]::Open(
            $DestinationPath,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None)
        try {
            $inputStream.CopyTo($outputStream)
        }
        finally {
            $outputStream.Dispose()
        }
    }
    finally {
        $inputStream.Dispose()
    }
}

$evidence = $null
$safeFailure = $null
$actualOuterHash = $null
$validatedManifestHash = $null
$actualRuntimeHash = $null
$actualInitdbHash = $null
$postgresVersion = $null
$initdbResult = $null
$versionMatched = $false
$toolAvailable = $false
$ownedProbeFullPath = $null
$probeParent = $null

try {
    $packagePath = (Resolve-Path -LiteralPath $PackageDirectory -ErrorAction Stop).Path
    $archivePath = (Resolve-Path -LiteralPath $OfficePackageArchivePath -ErrorAction Stop).Path
    $expectedOuterHash = $OfficePackageArchiveSha256.ToUpperInvariant()
    if ($expectedOuterHash -notmatch '^[A-F0-9]{64}$') {
        throw "The expected sealed package SHA-256 is invalid."
    }
    $actualOuterHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash.ToUpperInvariant()
    if ($actualOuterHash -cne $expectedOuterHash) {
        throw "The sealed office package changed before dependency inspection."
    }

    $trustedLifecycleModule = Join-Path $PSScriptRoot 'OfficeDatabaseLifecycle.psm1'
    Import-Module -Name $trustedLifecycleModule -Force
    $validatedManifest = Test-OfficeDatabasePackage -PackageDirectory $packagePath
    $validatedManifestPath = Join-Path $packagePath 'database\database-package-manifest.json'
    $validatedManifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $validatedManifestPath).Hash.ToUpperInvariant()
    $postgresVersion = [string]$validatedManifest.postgresql.version
    $runtimeArchiveName = [string]$validatedManifest.postgresql.runtimeArchiveFileName
    if ([IO.Path]::GetFileName($runtimeArchiveName) -cne $runtimeArchiveName -or
        [string]::IsNullOrWhiteSpace($runtimeArchiveName)) {
        throw "The database package manifest contains an unsafe runtime archive name."
    }

    $probeParent = [IO.Path]::GetFullPath($ProbeRoot)
    New-Item -ItemType Directory -Force -Path $probeParent | Out-Null
    $ownedProbeRoot = Join-Path $probeParent "probe-$([Guid]::NewGuid().ToString('N'))"
    $ownedProbeFullPath = [IO.Path]::GetFullPath($ownedProbeRoot)
    if (-not $ownedProbeFullPath.StartsWith($probeParent.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "The dependency probe working directory escaped its parent."
    }
    $evidenceFullPath = [IO.Path]::GetFullPath($EvidencePath)
    if ($evidenceFullPath.StartsWith($ownedProbeFullPath.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "The dependency probe evidence path cannot be inside its disposable working directory."
    }
    New-Item -ItemType Directory -Path $ownedProbeFullPath | Out-Null

    $sealedManifestPath = Join-Path $ownedProbeFullPath 'database-package-manifest.json'
    $runtimeArchivePath = Join-Path $ownedProbeFullPath 'postgresql-runtime.zip'
    $runtimeRoot = Join-Path $ownedProbeFullPath 'runtime'
    $outerArchive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $manifestEntry = Get-UniqueArchiveEntry `
            -Archive $outerArchive `
            -ExpectedPath 'database/database-package-manifest.json'
        Copy-ProbeArchiveEntry -Entry $manifestEntry -DestinationPath $sealedManifestPath
        $runtimeEntry = Get-UniqueArchiveEntry `
            -Archive $outerArchive `
            -ExpectedPath "database/$runtimeArchiveName"
        Copy-ProbeArchiveEntry -Entry $runtimeEntry -DestinationPath $runtimeArchivePath
    }
    finally {
        $outerArchive.Dispose()
    }

    $sealedManifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sealedManifestPath).Hash.ToUpperInvariant()
    if ($sealedManifestHash -cne $validatedManifestHash) {
        throw "The sealed database manifest does not match the package that passed validation."
    }

    $expectedRuntimeHash = ([string]$validatedManifest.postgresql.runtimeSha256).ToUpperInvariant()
    $actualRuntimeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimeArchivePath).Hash.ToUpperInvariant()
    if ($actualRuntimeHash -cne $expectedRuntimeHash) {
        throw "The sealed PostgreSQL runtime changed before dependency inspection."
    }

    New-Item -ItemType Directory -Path $runtimeRoot | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($runtimeArchivePath, $runtimeRoot)
    $runtimeBin = Join-Path $runtimeRoot 'bin'
    $initdbPath = Join-Path $runtimeBin 'initdb.exe'
    if (-not (Test-Path -LiteralPath $initdbPath -PathType Leaf)) {
        throw "The sealed PostgreSQL runtime does not contain initdb.exe."
    }
    $expectedInitdbHashProperty = $validatedManifest.postgresql.requiredRuntimeFileSha256.PSObject.Properties['bin/initdb.exe']
    if ($null -eq $expectedInitdbHashProperty) {
        throw "The database package manifest does not pin initdb.exe."
    }
    $actualInitdbHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $initdbPath).Hash.ToUpperInvariant()
    if ($actualInitdbHash -cne ([string]$expectedInitdbHashProperty.Value).ToUpperInvariant()) {
        throw "The extracted initdb.exe does not match the sealed package manifest."
    }

    $initdbResult = Invoke-ProbeNativeCommand `
        -FilePath $initdbPath `
        -Arguments @('--version') `
        -WorkingDirectory $runtimeBin
    $expectedVersionOutput = "initdb (PostgreSQL) $postgresVersion"
    $versionMatched = $initdbResult.ExitCode -eq 0 -and
        $initdbResult.StandardOutput.Trim() -ceq $expectedVersionOutput

    $toolPath = Get-ProbeDependencyTool -RequestedPath $DependencyToolPath
    $toolAvailable = $true
    $runtimeBinFiles = @{}
    foreach ($file in @(Get-ChildItem -LiteralPath $runtimeBin -File -Force)) {
        if ($runtimeBinFiles.ContainsKey($file.Name)) {
            throw "The PostgreSQL runtime contains an ambiguous executable-directory leaf name."
        }
        $runtimeBinFiles[$file.Name] = $file.FullName
    }
    $outsideBinNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @(Get-ChildItem -LiteralPath $runtimeRoot -Recurse -File -Force)) {
        if ([IO.Path]::GetDirectoryName($file.FullName) -ine $runtimeBin) {
            [void]$outsideBinNames.Add($file.Name)
        }
    }

    $knownDlls = Get-ProbeKnownDllNames
    $systemDirectory = [Environment]::SystemDirectory
    $windowsDirectory = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
    $externalPathDirectories = @([Environment]::GetEnvironmentVariable('PATH', 'Process') -split ';' | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_) -and
        -not [string]::Equals($_.Trim(), $runtimeBin, [StringComparison]::OrdinalIgnoreCase) -and
        -not [string]::Equals($_.Trim(), $systemDirectory, [StringComparison]::OrdinalIgnoreCase) -and
        -not [string]::Equals($_.Trim(), $windowsDirectory, [StringComparison]::OrdinalIgnoreCase)
    })

    $queue = [Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($initdbPath)
    $visited = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $edges = [Collections.Generic.List[object]]::new()
    $toolFailures = [Collections.Generic.List[object]]::new()
    $unresolvedCount = 0
    $externalCount = 0

    while ($queue.Count -gt 0) {
        $imagePath = $queue.Dequeue()
        $importer = [IO.Path]::GetFileName($imagePath)
        if (-not $visited.Add($importer)) {
            continue
        }
        $dependencyResult = Get-ProbeImageDependencies -ToolPath $toolPath -ImagePath $imagePath
        if ($dependencyResult.ExitCode -ne 0) {
            $toolFailures.Add([ordered]@{
                importer = $importer
                reason = 'ToolExitCode'
                exitCode = [int]$dependencyResult.ExitCode
                exitCodeHex = ConvertTo-ProbeExitCodeHex -ExitCode ([int]$dependencyResult.ExitCode)
            })
            continue
        }
        if (-not $dependencyResult.ParseSucceeded -or
            ($importer -ieq 'initdb.exe' -and
                (-not $dependencyResult.HeaderSeen -or @($dependencyResult.Dependencies).Count -eq 0))) {
            $toolFailures.Add([ordered]@{
                importer = $importer
                reason = 'OutputParseFailure'
                exitCode = [int]$dependencyResult.ExitCode
                exitCodeHex = ConvertTo-ProbeExitCodeHex -ExitCode ([int]$dependencyResult.ExitCode)
                rejectedEntryCount = [int]$dependencyResult.RejectedEntryCount
            })
            continue
        }
        foreach ($dependency in @($dependencyResult.Dependencies)) {
            $dependencyName = [string]$dependency.Name
            $resolution = $null
            if ($runtimeBinFiles.ContainsKey($dependencyName)) {
                $resolution = 'PackagedRuntimeBin'
                $queue.Enqueue([string]$runtimeBinFiles[$dependencyName])
            }
            elseif ($dependencyName -match '^(?i:(?:api|ext)-ms-win-[a-z0-9][a-z0-9-]*-l[0-9]+-[0-9]+-[0-9]+\.dll)$') {
                $resolution = 'WindowsApiSet'
            }
            elseif ($knownDlls.Contains($dependencyName)) {
                $resolution = 'WindowsKnownDll'
            }
            elseif (Test-Path -LiteralPath (Join-Path $systemDirectory $dependencyName) -PathType Leaf) {
                $resolution = 'WindowsSystemDirectory'
            }
            elseif (Test-Path -LiteralPath (Join-Path $windowsDirectory $dependencyName) -PathType Leaf) {
                $resolution = 'WindowsDirectory'
            }
            elseif ($outsideBinNames.Contains($dependencyName)) {
                $resolution = 'PackagedOutsideExecutableDirectory'
                $unresolvedCount++
            }
            else {
                $externalMatch = $false
                foreach ($externalDirectory in $externalPathDirectories) {
                    try {
                        if (Test-Path -LiteralPath (Join-Path $externalDirectory.Trim() $dependencyName) -PathType Leaf) {
                            $externalMatch = $true
                            break
                        }
                    }
                    catch {
                        continue
                    }
                }
                if ($externalMatch) {
                    $resolution = 'ExternalPathOnly'
                    $externalCount++
                }
                else {
                    $resolution = 'Unresolved'
                    $unresolvedCount++
                }
            }
            $edges.Add([ordered]@{
                importer = $importer
                dependency = $dependencyName
                delayLoad = [bool]$dependency.DelayLoad
                resolution = $resolution
            })
        }
    }

    $issueCodes = [Collections.Generic.List[string]]::new()
    if ($initdbResult.ExitCode -ne 0) {
        $issueCodes.Add('RawInitdbLoaderFailure')
    }
    elseif (-not $versionMatched) {
        $issueCodes.Add('InitdbVersionMismatch')
    }
    if ($toolFailures.Count -gt 0) {
        $issueCodes.Add('DependencyToolFailure')
    }
    if ($externalCount -gt 0) {
        $issueCodes.Add('ExternalPathDependency')
    }
    if ($unresolvedCount -gt 0) {
        $issueCodes.Add('UnresolvedDependency')
    }
    $outcome = if ($issueCodes.Count -eq 0) { 'Ready' } else { 'Failed' }

    $evidence = [ordered]@{
        schemaVersion = 1
        proof = 'safarsuite-office-postgresql-runtime-dependencies'
        recordedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        sourceRevision = [string]$env:GITHUB_SHA
        runner = [ordered]@{
            imageOs = [string]$env:ImageOS
            imageVersion = [string]$env:ImageVersion
            osVersion = [Environment]::OSVersion.Version.ToString()
        }
        package = [ordered]@{
            officeArchiveSha256 = $actualOuterHash
            databaseManifestSha256 = $validatedManifestHash
            postgresRuntimeSha256 = $actualRuntimeHash
            initdbSha256 = $actualInitdbHash
            postgresVersion = $postgresVersion
        }
        tool = [ordered]@{
            executable = [IO.Path]::GetFileName($toolPath)
            available = $true
            failures = @($toolFailures)
        }
        initdb = [ordered]@{
            executable = 'initdb.exe'
            completed = $true
            exitCode = [int]$initdbResult.ExitCode
            exitCodeHex = ConvertTo-ProbeExitCodeHex -ExitCode ([int]$initdbResult.ExitCode)
            versionMatched = [bool]$versionMatched
        }
        inspectedImageCount = $visited.Count
        edges = @($edges | Sort-Object importer, dependency, delayLoad)
        unresolvedCount = $unresolvedCount
        externalPathDependencyCount = $externalCount
        issueCodes = @($issueCodes)
        outcome = $outcome
    }
    Write-ProbeEvidence -Path $EvidencePath -Value $evidence

    Write-Host "Raw PostgreSQL probe: executable='initdb.exe'; exit code $($initdbResult.ExitCode); hexadecimal exit code $(ConvertTo-ProbeExitCodeHex -ExitCode ([int]$initdbResult.ExitCode)); version matched=$versionMatched."
    foreach ($edge in @($edges | Where-Object {
        $_.resolution -in @('PackagedOutsideExecutableDirectory', 'ExternalPathOnly', 'Unresolved')
    } | Sort-Object importer, dependency, delayLoad)) {
        Write-Host "Dependency finding: importer='$($edge.importer)'; dependency='$($edge.dependency)'; resolution='$($edge.resolution)'; delay load=$($edge.delayLoad)."
    }

    if ($outcome -ne 'Ready') {
        $safeFailure = "The raw packaged PostgreSQL dependency probe failed. Executable 'initdb.exe'; exit code $($initdbResult.ExitCode); hexadecimal exit code $(ConvertTo-ProbeExitCodeHex -ExitCode ([int]$initdbResult.ExitCode)); unresolved dependencies=$unresolvedCount; external PATH dependencies=$externalCount; dependency-tool failures=$($toolFailures.Count)."
    }
}
catch {
    if ($null -eq $evidence) {
        $fallbackPackage = [ordered]@{}
        if ([string]$actualOuterHash -match '^[A-F0-9]{64}$') {
            $fallbackPackage['officeArchiveSha256'] = $actualOuterHash
        }
        if ([string]$validatedManifestHash -match '^[A-F0-9]{64}$') {
            $fallbackPackage['databaseManifestSha256'] = $validatedManifestHash
        }
        if ([string]$actualRuntimeHash -match '^[A-F0-9]{64}$') {
            $fallbackPackage['postgresRuntimeSha256'] = $actualRuntimeHash
        }
        if ([string]$actualInitdbHash -match '^[A-F0-9]{64}$') {
            $fallbackPackage['initdbSha256'] = $actualInitdbHash
        }
        if ([string]$postgresVersion -match '^17\.[0-9]+$') {
            $fallbackPackage['postgresVersion'] = $postgresVersion
        }
        $fallbackIssueCodes = [Collections.Generic.List[string]]::new()
        if ($null -ne $initdbResult) {
            if ($initdbResult.ExitCode -ne 0) {
                $fallbackIssueCodes.Add('RawInitdbLoaderFailure')
            }
            elseif (-not $versionMatched) {
                $fallbackIssueCodes.Add('InitdbVersionMismatch')
            }
        }
        $fallbackIssueCodes.Add('DiagnosticInternalFailure')
        $fallbackInitdb = [ordered]@{
            executable = 'initdb.exe'
            completed = $null -ne $initdbResult
        }
        if ($null -ne $initdbResult) {
            $fallbackInitdb['exitCode'] = [int]$initdbResult.ExitCode
            $fallbackInitdb['exitCodeHex'] = ConvertTo-ProbeExitCodeHex -ExitCode ([int]$initdbResult.ExitCode)
            $fallbackInitdb['versionMatched'] = [bool]$versionMatched
        }
        $evidence = [ordered]@{
            schemaVersion = 1
            proof = 'safarsuite-office-postgresql-runtime-dependencies'
            recordedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
            sourceRevision = [string]$env:GITHUB_SHA
            runner = [ordered]@{
                imageOs = [string]$env:ImageOS
                imageVersion = [string]$env:ImageVersion
                osVersion = [Environment]::OSVersion.Version.ToString()
            }
            package = $fallbackPackage
            tool = [ordered]@{
                executable = 'dumpbin.exe'
                available = [bool]$toolAvailable
            }
            initdb = $fallbackInitdb
            issueCodes = @($fallbackIssueCodes)
            outcome = 'Failed'
        }
    }
    try {
        Write-ProbeEvidence -Path $EvidencePath -Value $evidence
    }
    catch {
    }
    $safeFailure = 'The raw packaged PostgreSQL dependency probe could not complete safely.'
}
finally {
    if ($null -ne $ownedProbeFullPath -and
        (Test-Path -LiteralPath $ownedProbeFullPath -PathType Container)) {
        try {
            $resolvedOwnedRoot = (Resolve-Path -LiteralPath $ownedProbeFullPath).Path
            if (-not $resolvedOwnedRoot.StartsWith($probeParent.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase) -or
                [IO.Path]::GetFileName($resolvedOwnedRoot) -notmatch '^probe-[0-9a-f]{32}$') {
                throw "Unsafe cleanup target."
            }
            Remove-Item -LiteralPath $resolvedOwnedRoot -Recurse -Force
        }
        catch {
            throw "The dependency probe could not clean its disposable working directory safely."
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($safeFailure)) {
    throw $safeFailure
}

Write-Host 'Raw packaged PostgreSQL dependency probe passed.'
