param(
    [Parameter(Mandatory = $true)]
    [string]$SourceArchivePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputArchivePath,

    [string]$DistributionManifestPath = (Join-Path $PSScriptRoot "postgresql-distribution.json")
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$sourcePath = (Resolve-Path -LiteralPath $SourceArchivePath).Path
$manifestPath = (Resolve-Path -LiteralPath $DistributionManifestPath).Path
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
$outputPath = [System.IO.Path]::GetFullPath($OutputArchivePath)
$outputParent = Split-Path -Parent $outputPath

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "The PostgreSQL source archive is missing."
}

if ($manifest.majorVersion -ne 17 -or [string]$manifest.version -notmatch '^17\.[0-9]+$') {
    throw "The PostgreSQL distribution manifest must pin an exact PostgreSQL 17 release."
}

$sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $sourcePath).Hash.ToUpperInvariant()
if ($sourceHash -ne ([string]$manifest.sha256).ToUpperInvariant()) {
    throw "The PostgreSQL source archive SHA-256 does not match the pinned distribution manifest."
}

if (Test-Path -LiteralPath $outputPath) {
    throw "The PostgreSQL runtime output archive already exists."
}

New-Item -ItemType Directory -Force -Path $outputParent | Out-Null

$sourceArchive = [System.IO.Compression.ZipFile]::OpenRead($sourcePath)
$outputStream = $null
$outputArchive = $null

try {
    $sourceRoot = [string]$manifest.sourceArchiveRoot
    $includeRoots = @($manifest.runtimeIncludeRoots | ForEach-Object { [string]$_ })
    $includeFiles = @($manifest.runtimeIncludeFiles | ForEach-Object { [string]$_ })
    $selected = [System.Collections.Generic.List[object]]::new()
    $seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($entry in $sourceArchive.Entries) {
        $entryPath = $entry.FullName
        if (-not $entryPath.StartsWith($sourceRoot, [System.StringComparison]::Ordinal)) {
            continue
        }

        $relativePath = $entryPath.Substring($sourceRoot.Length)
        if ([string]::IsNullOrWhiteSpace($relativePath) -or $relativePath.EndsWith('/')) {
            continue
        }

        $included = $includeFiles -contains $relativePath
        if (-not $included) {
            $included = $includeRoots | Where-Object {
                $relativePath.StartsWith($_, [System.StringComparison]::Ordinal)
            } | Select-Object -First 1
        }

        if (-not $included) {
            continue
        }

        $segments = @($relativePath.Split('/'))
        if ($relativePath.Contains('\') -or
            $relativePath.Contains(':') -or
            $relativePath.StartsWith('/') -or
            $relativePath.EndsWith('/') -or
            @($segments | Where-Object {
                [string]::IsNullOrWhiteSpace($_) -or
                $_ -in @('.', '..') -or
                $_.EndsWith('.') -or
                $_.EndsWith(' ') -or
                $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
                $_ -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$'
            }).Count -gt 0) {
            throw "The PostgreSQL archive contains an unsafe runtime entry."
        }

        if (-not $seenPaths.Add($relativePath)) {
            throw "The PostgreSQL archive contains a duplicate or case-colliding runtime entry."
        }

        $selected.Add([pscustomobject]@{
            Source = $entry
            RelativePath = $relativePath
        })
    }

    foreach ($requiredRuntimeFile in @($manifest.requiredRuntimeFiles)) {
        if (-not $seenPaths.Contains([string]$requiredRuntimeFile)) {
            throw "The PostgreSQL archive is missing a required runtime file."
        }
    }

    $outputStream = [System.IO.File]::Open(
        $outputPath,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::ReadWrite,
        [System.IO.FileShare]::None)
    $outputArchive = [System.IO.Compression.ZipArchive]::new(
        $outputStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $true)

    foreach ($item in @($selected | Sort-Object RelativePath)) {
        $targetEntry = $outputArchive.CreateEntry(
            $item.RelativePath,
            [System.IO.Compression.CompressionLevel]::Optimal)
        $targetEntry.LastWriteTime = $item.Source.LastWriteTime
        $sourceEntryStream = $item.Source.Open()
        $targetEntryStream = $targetEntry.Open()

        try {
            $sourceEntryStream.CopyTo($targetEntryStream)
        }
        finally {
            $targetEntryStream.Dispose()
            $sourceEntryStream.Dispose()
        }
    }
}
catch {
    if ($null -ne $outputArchive) {
        $outputArchive.Dispose()
        $outputArchive = $null
    }

    if ($null -ne $outputStream) {
        $outputStream.Dispose()
        $outputStream = $null
    }

    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Force
    }

    throw
}
finally {
    if ($null -ne $outputArchive) {
        $outputArchive.Dispose()
    }

    if ($null -ne $outputStream) {
        $outputStream.Dispose()
    }

    $sourceArchive.Dispose()
}

$runtimeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $outputPath).Hash.ToUpperInvariant()
$runtimeBytes = (Get-Item -LiteralPath $outputPath).Length
$runtimeFileHashes = [ordered]@{}
$requiredFileHashes = [ordered]@{}
$verificationArchive = [System.IO.Compression.ZipFile]::OpenRead($outputPath)
try {
    $verifiedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($verificationArchive.Entries | Sort-Object FullName)) {
        $relativePath = [string]$entry.FullName
        $segments = @($relativePath.Split('/'))
        if ([string]::IsNullOrWhiteSpace($relativePath) -or
            $relativePath.Contains('\') -or
            $relativePath.Contains(':') -or
            $relativePath.StartsWith('/') -or
            $relativePath.EndsWith('/') -or
            @($segments | Where-Object {
                [string]::IsNullOrWhiteSpace($_) -or
                $_ -in @('.', '..') -or
                $_.EndsWith('.') -or
                $_.EndsWith(' ') -or
                $_.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
                $_ -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$'
            }).Count -gt 0 -or
            -not $verifiedPaths.Add($relativePath)) {
            throw "The generated PostgreSQL runtime contains an unsafe, duplicate, or non-normalized file path."
        }

        $entryStream = $entry.Open()
        $sha256 = [Security.Cryptography.SHA256]::Create()
        try {
            $hashBytes = $sha256.ComputeHash($entryStream)
            $runtimeFileHashes[$relativePath] = ([BitConverter]::ToString($hashBytes)).Replace('-', '')
        }
        finally {
            $sha256.Dispose()
            $entryStream.Dispose()
        }
    }

    foreach ($requiredRuntimeFile in @($manifest.requiredRuntimeFiles)) {
        $requiredPath = [string]$requiredRuntimeFile
        if (-not $runtimeFileHashes.Contains($requiredPath)) {
            throw "The generated PostgreSQL runtime is missing a required file."
        }
        $requiredFileHashes[$requiredPath] = [string]$runtimeFileHashes[$requiredPath]
    }
}
finally {
    $verificationArchive.Dispose()
}

[pscustomobject]@{
    Version = [string]$manifest.version
    SourceSha256 = $sourceHash
    RuntimeSha256 = $runtimeHash
    RuntimeBytes = [long]$runtimeBytes
    RuntimeArchivePath = $outputPath
    RuntimeFileCount = $runtimeFileHashes.Count
    RuntimeFileSha256 = $runtimeFileHashes
    RequiredFileSha256 = $requiredFileHashes
}
