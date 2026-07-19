Set-StrictMode -Version Latest

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if ($null -eq ('SafarSuiteControlDeskCommandLine' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class SafarSuiteControlDeskCommandLine
{
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW(
        [MarshalAs(UnmanagedType.LPWStr)] string commandLine,
        out int argumentCount);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);

    public static string[] Split(string commandLine)
    {
        int count;
        IntPtr arguments = CommandLineToArgvW(commandLine, out count);
        if (arguments == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            var result = new string[count];
            for (int index = 0; index < count; index++)
            {
                IntPtr value = Marshal.ReadIntPtr(arguments, index * IntPtr.Size);
                result[index] = Marshal.PtrToStringUni(value);
            }
            return result;
        }
        finally
        {
            LocalFree(arguments);
        }
    }
}
'@
}

$script:LifecycleProduct = "SafarSuite Control Desk"
$script:LifecycleSchemaVersion = 1
$script:StateFileName = "database-state.json"
$script:ActivationFileName = "database-activation.json"
$script:NativeProcessEnvironmentLock = [object]::new()

function ConvertTo-OfficeForwardSlashPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return ([System.IO.Path]::GetFullPath($Path)).Replace('\', '/')
}

function Test-OfficeRuntimeRelativePath {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or
        $Path.Contains('\') -or
        $Path.Contains(':') -or
        $Path.StartsWith('/') -or
        $Path.EndsWith('/')) {
        return $false
    }

    foreach ($segment in @($Path.Split('/'))) {
        if ([string]::IsNullOrWhiteSpace($segment) -or
            $segment -in @('.', '..') -or
            $segment.EndsWith('.') -or
            $segment.EndsWith(' ') -or
            $segment.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
            $segment -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?$') {
            return $false
        }
    }

    return $true
}

function Test-OfficeRuntimeArchiveEntryIsLink {
    param([Parameter(Mandatory = $true)][IO.Compression.ZipArchiveEntry]$Entry)

    $attributes = [BitConverter]::ToUInt32([BitConverter]::GetBytes([int]$Entry.ExternalAttributes), 0)
    $unixFileType = ($attributes -shr 16) -band 0xF000
    $dosAttributes = $attributes -band 0xFFFF
    return $unixFileType -eq 0xA000 -or
        [bool]($dosAttributes -band [uint32][IO.FileAttributes]::ReparsePoint)
}

function Test-OfficeIsAdministrator {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        return $false
    }

    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Set-OfficeUtf8NoBomContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value
    )

    [IO.File]::WriteAllText(
        [IO.Path]::GetFullPath($Path),
        $Value,
        [Text.UTF8Encoding]::new($false))
}

function Set-OfficeAtomicUtf8NoBomContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    $directory = Split-Path -Parent $fullPath
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $temporaryPath = Join-Path $directory ('.safarsuite-write-{0}.tmp' -f [Guid]::NewGuid().ToString('N'))
    try {
        Set-OfficeUtf8NoBomContent -Path $temporaryPath -Value $Value
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            [IO.File]::Replace($temporaryPath, $fullPath, $null, $true)
        }
        else {
            [IO.File]::Move($temporaryPath, $fullPath)
        }
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

function Set-OfficeAtomicJsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    Set-OfficeAtomicUtf8NoBomContent -Path $Path -Value ($Value | ConvertTo-Json -Depth 12)
}

function ConvertTo-OfficeWindowsCommandLineArgument {
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

function Assert-OfficeAdministrator {
    if (-not (Test-OfficeIsAdministrator)) {
        throw "The SafarSuite Control Desk database lifecycle must run from an elevated Windows PowerShell session."
    }
}

function Get-OfficeDatabasePaths {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageDirectory,

        [string]$ProgramFilesRoot,

        [string]$ProgramDataRoot
    )

    $packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
    if ([string]::IsNullOrWhiteSpace($ProgramFilesRoot)) {
        $ProgramFilesRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    }

    if ([string]::IsNullOrWhiteSpace($ProgramDataRoot)) {
        $ProgramDataRoot = [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData)
    }

    if ([string]::IsNullOrWhiteSpace($ProgramFilesRoot) -or [string]::IsNullOrWhiteSpace($ProgramDataRoot)) {
        throw "Program Files and ProgramData roots must resolve to absolute paths."
    }

    $packageManifest = Test-OfficeDatabasePackage -PackageDirectory $packagePath
    $distribution = $packageManifest.postgresql
    $version = [string]$distribution.version
    $productProgramFiles = Join-Path ([System.IO.Path]::GetFullPath($ProgramFilesRoot)) "SafarSuite\ControlDesk"
    $productProgramData = Join-Path ([System.IO.Path]::GetFullPath($ProgramDataRoot)) "SafarSuite\ControlDesk"
    $databaseDataRoot = Join-Path $productProgramData "Database\PostgreSQL17"

    return [pscustomobject]@{
        PackageDirectory = $packagePath
        DatabasePackageDirectory = Join-Path $packagePath "database"
        ProgramFilesRoot = $productProgramFiles
        ProgramDataRoot = $productProgramData
        RuntimeRoot = Join-Path $productProgramFiles "Database\PostgreSQL\$version"
        RuntimeArchivePath = Join-Path $packagePath "database\$($distribution.runtimeArchiveFileName)"
        DataRoot = $databaseDataRoot
        DataDirectory = Join-Path $databaseDataRoot "Data"
        DatabaseLogDirectory = Join-Path $productProgramData "Logs\PostgreSQL"
        LifecycleLogDirectory = Join-Path $productProgramData "Logs\DatabaseLifecycle"
        SecretDirectory = Join-Path $productProgramData "Secrets\Database"
        StateDirectory = Join-Path $productProgramData "State\Database"
        StateFilePath = Join-Path $productProgramData "State\Database\$script:StateFileName"
        InitializationReceiptPath = Join-Path $productProgramData "State\Database\database-initialization.json"
        ActivationFilePath = Join-Path $productProgramData "State\Database\$script:ActivationFileName"
        AdminPassfilePath = Join-Path $productProgramData "Secrets\Database\admin.pgpass"
        MigratorPassfilePath = Join-Path $productProgramData "Secrets\Database\migrator.pgpass"
        ApplicationPassfilePath = Join-Path $productProgramData "Secrets\Database\application.pgpass"
        MigrationBundlePath = Join-Path $packagePath "database\SafarSuite.ControlDesk.Migrations.exe"
        PackageManifestPath = Join-Path $packagePath "database\database-package-manifest.json"
        DistributionManifestPath = Join-Path $packagePath "database\postgresql-distribution.json"
        PostgresConfigurationTemplatePath = Join-Path $packagePath "database\postgresql.conf.template"
        HbaConfigurationTemplatePath = Join-Path $packagePath "database\pg_hba.conf.template"
        PostgresConfigurationPath = Join-Path $databaseDataRoot "Data\postgresql.conf"
        HbaConfigurationPath = Join-Path $databaseDataRoot "Data\pg_hba.conf"
        AuditPath = Join-Path $productProgramData "Logs\DatabaseLifecycle\database-lifecycle.jsonl"
    }
}

function Test-OfficeDatabasePackage {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$PackageDirectory)

    $packagePath = (Resolve-Path -LiteralPath $PackageDirectory).Path
    $databaseDirectory = Join-Path $packagePath "database"
    $manifestPath = Join-Path $databaseDirectory "database-package-manifest.json"
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "The office package has no database package manifest."
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    if ($manifest.schemaVersion -ne $script:LifecycleSchemaVersion -or
        $manifest.product -ne $script:LifecycleProduct -or
        $manifest.lifecycle -ne "native-postgresql-17") {
        throw "The database package manifest is not a supported SafarSuite Control Desk lifecycle manifest."
    }

    if ($manifest.postgresql.majorVersion -ne 17 -or
        [string]$manifest.postgresql.version -notmatch '^17\.[0-9]+$' -or
        $manifest.postgresql.port -lt 1024 -or
        $manifest.postgresql.port -gt 65535) {
        throw "The database package must pin PostgreSQL 17 and a non-privileged TCP port."
    }

    foreach ($identifier in @(
        [string]$manifest.postgresql.databaseName,
        [string]$manifest.postgresql.adminRole,
        [string]$manifest.postgresql.migratorRole,
        [string]$manifest.postgresql.applicationRole
    )) {
        if ($identifier -notmatch '^[a-z][a-z0-9_]{2,62}$') {
            throw "The database package contains an unsafe PostgreSQL identifier."
        }
    }

    if (-not $manifest.postgresql.runtimeIncluded) {
        throw "The office package does not include the installer-managed PostgreSQL runtime."
    }

    $runtimePath = Join-Path $databaseDirectory ([string]$manifest.postgresql.runtimeArchiveFileName)
    $bundlePath = Join-Path $databaseDirectory "SafarSuite.ControlDesk.Migrations.exe"
    $visualCppRuntimePath = Join-Path $databaseDirectory ([string]$manifest.postgresql.visualCppRuntime.archiveFileName)
    foreach ($filePath in @($runtimePath, $bundlePath, $visualCppRuntimePath)) {
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            throw "A required database package payload is missing: $([System.IO.Path]::GetFileName($filePath))"
        }
    }

    $runtimeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimePath).Hash.ToUpperInvariant()
    $bundleHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $bundlePath).Hash.ToUpperInvariant()
    if ($runtimeHash -ne ([string]$manifest.postgresql.runtimeSha256).ToUpperInvariant()) {
        throw "The packaged PostgreSQL runtime failed SHA-256 verification."
    }

    $runtimeInventoryProperty = $manifest.postgresql.PSObject.Properties['runtimeFileSha256']
    if ($null -eq $runtimeInventoryProperty -or $null -eq $runtimeInventoryProperty.Value) {
        throw "The database manifest has no complete PostgreSQL runtime file inventory."
    }
    $runtimeInventory = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    $runtimeInventoryPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($property in @($runtimeInventoryProperty.Value.PSObject.Properties)) {
        $relativePath = [string]$property.Name
        $fileHash = ([string]$property.Value).ToUpperInvariant()
        if (-not (Test-OfficeRuntimeRelativePath -Path $relativePath) -or
            $fileHash -notmatch '^[0-9A-F]{64}$' -or
            -not $runtimeInventoryPaths.Add($relativePath)) {
            throw "The database manifest contains an unsafe, duplicate, or invalid PostgreSQL runtime inventory entry."
        }
        $runtimeInventory.Add($relativePath, $fileHash)
    }
    if ($runtimeInventory.Count -lt 1 -or
        [int]$manifest.postgresql.runtimeFileCount -ne $runtimeInventory.Count) {
        throw "The database manifest PostgreSQL runtime file count is inconsistent."
    }

    $runtimeArchive = [IO.Compression.ZipFile]::OpenRead($runtimePath)
    try {
        $archivePaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($entry in @($runtimeArchive.Entries)) {
            $relativePath = [string]$entry.FullName
            if (-not (Test-OfficeRuntimeRelativePath -Path $relativePath) -or
                (Test-OfficeRuntimeArchiveEntryIsLink -Entry $entry) -or
                -not $archivePaths.Add($relativePath) -or
                -not $runtimeInventory.ContainsKey($relativePath)) {
                throw "The packaged PostgreSQL runtime contains an unsafe, duplicate, or unlisted archive entry."
            }
            $entryStream = $entry.Open()
            $sha256 = [Security.Cryptography.SHA256]::Create()
            try {
                $entryHash = ([BitConverter]::ToString($sha256.ComputeHash($entryStream))).Replace('-', '')
            }
            finally {
                $sha256.Dispose()
                $entryStream.Dispose()
            }
            if ($entryHash -ne $runtimeInventory[$relativePath]) {
                throw "A PostgreSQL runtime file failed package integrity verification."
            }
        }
        if ($archivePaths.Count -ne $runtimeInventory.Count) {
            throw "The packaged PostgreSQL runtime does not match its exact file inventory."
        }
    }
    finally {
        $runtimeArchive.Dispose()
    }

    $requiredInventoryProperty = $manifest.postgresql.PSObject.Properties['requiredRuntimeFileSha256']
    if ($null -eq $requiredInventoryProperty -or $null -eq $requiredInventoryProperty.Value) {
        throw "The database manifest has no required PostgreSQL runtime integrity records."
    }
    $requiredPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($requiredRuntimeFile in @($manifest.postgresql.requiredRuntimeFiles)) {
        $requiredPath = [string]$requiredRuntimeFile
        $requiredHashProperty = $requiredInventoryProperty.Value.PSObject.Properties[$requiredPath]
        if (-not (Test-OfficeRuntimeRelativePath -Path $requiredPath) -or
            -not $requiredPaths.Add($requiredPath) -or
            -not $runtimeInventory.ContainsKey($requiredPath) -or
            $null -eq $requiredHashProperty -or
            ([string]$requiredHashProperty.Value).ToUpperInvariant() -ne $runtimeInventory[$requiredPath]) {
            throw "The database manifest has no consistent integrity record for a required PostgreSQL runtime file."
        }
    }
    if (@($requiredInventoryProperty.Value.PSObject.Properties).Count -ne $requiredPaths.Count) {
        throw "The database manifest required PostgreSQL runtime integrity records are inconsistent."
    }

    if ($bundleHash -ne ([string]$manifest.migrations.bundleSha256).ToUpperInvariant()) {
        throw "The packaged migration bundle failed SHA-256 verification."
    }

    $visualCppRuntimeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $visualCppRuntimePath).Hash.ToUpperInvariant()
    if ($visualCppRuntimeHash -ne ([string]$manifest.postgresql.visualCppRuntime.sha256).ToUpperInvariant()) {
        throw "The packaged Microsoft Visual C++ runtime failed SHA-256 verification."
    }

    $migrationIds = @($manifest.migrations.orderedIds | ForEach-Object { [string]$_ })
    if ($migrationIds.Count -lt 1 -or
        $migrationIds.Count -ne [int]$manifest.migrations.count -or
        $migrationIds[-1] -ne [string]$manifest.migrations.target -or
        @($migrationIds | Select-Object -Unique).Count -ne $migrationIds.Count) {
        throw "The packaged migration ledger is incomplete or inconsistent."
    }

    if (@($manifest.migrations.requiredExtensions) -notcontains "pg_trgm") {
        throw "The database package manifest must require pg_trgm."
    }

    foreach ($requiredFile in @(
        "postgresql-distribution.json",
        "postgresql.conf.template",
        "pg_hba.conf.template"
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $databaseDirectory $requiredFile) -PathType Leaf)) {
            throw "A required database lifecycle file is missing: $requiredFile"
        }
    }

    return $manifest
}

function Get-OfficeInstalledVisualCppRuntimeVersion {
    $registryPaths = @(
        'HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64'
    )
    foreach ($registryPath in $registryPaths) {
        $runtime = Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue
        if ($null -ne $runtime -and [int]$runtime.Installed -eq 1 -and -not [string]::IsNullOrWhiteSpace([string]$runtime.Version)) {
            $versionText = ([string]$runtime.Version).TrimStart('v')
            $version = $null
            if ([Version]::TryParse($versionText, [ref]$version)) {
                return $version
            }
        }
    }
    return $null
}

function Test-OfficeVisualCppRuntime {
    param([Parameter(Mandatory = $true)]$Context)

    $installed = Get-OfficeInstalledVisualCppRuntimeVersion
    $minimum = [Version]::Parse([string]$Context.Distribution.visualCppRuntime.minimumVersion)
    return $null -ne $installed -and $installed -ge $minimum
}

function Install-OfficeVisualCppRuntime {
    param([Parameter(Mandatory = $true)]$Context)

    if (Test-OfficeVisualCppRuntime -Context $Context) {
        return
    }
    $installerPath = Join-Path $Context.Paths.DatabasePackageDirectory ([string]$Context.Distribution.visualCppRuntime.archiveFileName)
    $signature = Get-AuthenticodeSignature -LiteralPath $installerPath
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') {
        throw "The packaged Microsoft Visual C++ runtime has no valid Microsoft signature."
    }
    $result = Invoke-OfficeNativeCommand `
        -FilePath $installerPath `
        -Arguments @('/install', '/quiet', '/norestart') `
        -TimeoutSeconds 300 `
        -AllowFailure
    if ($result.ExitCode -notin @(0, 1638, 3010)) {
        throw "The Microsoft Visual C++ runtime installation failed with exit code $($result.ExitCode)."
    }
    if (-not (Test-OfficeVisualCppRuntime -Context $Context)) {
        throw "The required Microsoft Visual C++ runtime is still unavailable after installation."
    }
}

function Write-OfficeDatabaseAudit {
    param(
        [Parameter(Mandatory = $true)]$Paths,
        [Parameter(Mandatory = $true)][string]$EventCode,
        [Parameter(Mandatory = $true)][string]$Outcome,
        [hashtable]$Details = @{}
    )

    New-Item -ItemType Directory -Force -Path $Paths.LifecycleLogDirectory | Out-Null
    $entry = [ordered]@{
        timestampUtc = [DateTimeOffset]::UtcNow.ToString("O")
        product = $script:LifecycleProduct
        eventCode = $EventCode
        outcome = $Outcome
        details = $Details
    }
    Add-Content -LiteralPath $Paths.AuditPath -Encoding utf8 -Value ($entry | ConvertTo-Json -Compress -Depth 6)
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT -and (Test-OfficeIsAdministrator)) {
        Set-OfficeRestrictedAcl -Path $Paths.LifecycleLogDirectory -Profile Secrets
        Set-OfficeRestrictedAcl -Path $Paths.AuditPath -Profile Secrets
    }
}

function New-OfficeDatabasePassword {
    $bytes = New-Object byte[] 36
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $random.GetBytes($bytes)
    }
    finally {
        $random.Dispose()
    }

    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function ConvertTo-OfficeSqlLiteral {
    param([Parameter(Mandatory = $true)][string]$Value)
    return "'$($Value.Replace("'", "''"))'"
}

function Invoke-OfficeNativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string[]]$Arguments = @(),
        [hashtable]$Environment = @{},
        [string]$StandardInput,
        [int]$TimeoutSeconds = 120,
        [switch]$AllowFailure
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardInput = $PSBoundParameters.ContainsKey('StandardInput')
    $startInfo.Arguments = (($Arguments | ForEach-Object {
        ConvertTo-OfficeWindowsCommandLineArgument -Value ([string]$_)
    }) -join ' ')
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $previousEnvironment = @{}
    $environmentLockHeld = $false
    try {
        [Threading.Monitor]::Enter($script:NativeProcessEnvironmentLock)
        $environmentLockHeld = $true
        try {
            foreach ($entry in $Environment.GetEnumerator()) {
                $environmentName = [string]$entry.Key
                $previousEnvironment[$environmentName] = [Environment]::GetEnvironmentVariable($environmentName, 'Process')
                [Environment]::SetEnvironmentVariable($environmentName, [string]$entry.Value, 'Process')
            }
            if (-not $process.Start()) {
                throw "A required database lifecycle process could not start."
            }
        }
        finally {
            foreach ($entry in $previousEnvironment.GetEnumerator()) {
                [Environment]::SetEnvironmentVariable([string]$entry.Key, $entry.Value, 'Process')
            }
            [Threading.Monitor]::Exit($script:NativeProcessEnvironmentLock)
            $environmentLockHeld = $false
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        if ($startInfo.RedirectStandardInput) {
            $process.StandardInput.Write($StandardInput)
            $process.StandardInput.Close()
        }
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill()
            $process.WaitForExit()
            throw "A required database lifecycle process timed out."
        }
        $stdout = $stdoutTask.Result
        $stderr = $stderrTask.Result
        if ($process.ExitCode -ne 0 -and -not $AllowFailure) {
            throw "A required database lifecycle process failed with exit code $($process.ExitCode)."
        }
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StandardOutput = $stdout
            StandardError = $stderr
        }
    }
    finally {
        if ($environmentLockHeld) {
            foreach ($entry in $previousEnvironment.GetEnumerator()) {
                [Environment]::SetEnvironmentVariable([string]$entry.Key, $entry.Value, 'Process')
            }
            [Threading.Monitor]::Exit($script:NativeProcessEnvironmentLock)
        }
        $process.Dispose()
    }
}

function Set-OfficeRestrictedAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ServiceSid,
        [ValidateSet('Secrets', 'Data', 'Runtime')][string]$Profile
    )

    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw "Windows ACL enforcement is required for the native database lifecycle."
    }

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    $systemSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-18')
    $administratorsSid = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
    $security = if ($item.PSIsContainer) {
        [Security.AccessControl.DirectorySecurity]::new()
    }
    else {
        [Security.AccessControl.FileSecurity]::new()
    }
    $security.SetOwner($administratorsSid)
    $security.SetAccessRuleProtection($true, $false)
    $inheritanceFlags = if ($item.PSIsContainer) {
        [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
    }
    else {
        [Security.AccessControl.InheritanceFlags]::None
    }
    $propagationFlags = [Security.AccessControl.PropagationFlags]::None
    $expectedRules = [Collections.Generic.List[object]]::new()
    foreach ($identity in @($systemSid, $administratorsSid)) {
        $rule = [Security.AccessControl.FileSystemAccessRule]::new(
            $identity,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritanceFlags,
            $propagationFlags,
            [Security.AccessControl.AccessControlType]::Allow)
        [void]$security.AddAccessRule($rule)
        [void]$expectedRules.Add([pscustomobject]@{
            Sid = $identity.Value
            Rights = [Security.AccessControl.FileSystemRights]::FullControl
            InheritanceFlags = $inheritanceFlags
            PropagationFlags = $propagationFlags
        })
    }
    if (-not [string]::IsNullOrWhiteSpace($ServiceSid)) {
        $serviceIdentity = [Security.Principal.SecurityIdentifier]::new($ServiceSid)
        $serviceRights = if ($Profile -eq 'Runtime') {
            [Security.AccessControl.FileSystemRights]::ReadAndExecute
        }
        else {
            [Security.AccessControl.FileSystemRights]::Modify
        }
        $serviceRule = [Security.AccessControl.FileSystemAccessRule]::new(
            $serviceIdentity,
            $serviceRights,
            $inheritanceFlags,
            $propagationFlags,
            [Security.AccessControl.AccessControlType]::Allow)
        [void]$security.AddAccessRule($serviceRule)
        [void]$expectedRules.Add([pscustomobject]@{
            Sid = $serviceIdentity.Value
            Rights = $serviceRights
            InheritanceFlags = $inheritanceFlags
            PropagationFlags = $propagationFlags
        })
    }
    Set-Acl -LiteralPath $Path -AclObject $security

    $verified = Get-Acl -LiteralPath $Path
    if (-not $verified.AreAccessRulesProtected) {
        throw "Windows did not disable ACL inheritance on a managed database path."
    }
    $ownerSid = $verified.GetOwner([Security.Principal.SecurityIdentifier]).Value
    if ($ownerSid -ne $administratorsSid.Value) {
        throw "A managed database path does not have the expected Administrators owner."
    }
    $actualRules = @($verified.Access)
    if ($actualRules.Count -ne $expectedRules.Count -or
        @($actualRules | Where-Object { $_.AccessControlType -ne [Security.AccessControl.AccessControlType]::Allow -or $_.IsInherited }).Count -gt 0) {
        throw "A managed database path retained an unexpected Windows ACL entry."
    }
    foreach ($expectedRule in $expectedRules) {
        $matches = @($actualRules | Where-Object {
            $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -eq $expectedRule.Sid -and
            [int]$_.FileSystemRights -eq [int]$expectedRule.Rights -and
            $_.InheritanceFlags -eq $expectedRule.InheritanceFlags -and
            $_.PropagationFlags -eq $expectedRule.PropagationFlags
        })
        if ($matches.Count -ne 1) {
            throw "A managed database path retained incorrect Windows ACL rights."
        }
    }
}

function Test-OfficeRestrictedAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ServiceSid,
        [ValidateSet('Secrets', 'Data', 'Runtime')][string]$Profile
    )

    try {
        $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
        $acl = Get-Acl -LiteralPath $Path -ErrorAction Stop
        $administratorsSid = 'S-1-5-32-544'
        if (-not $acl.AreAccessRulesProtected -or
            $acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -ne $administratorsSid) {
            return $false
        }
        $inheritanceFlags = if ($item.PSIsContainer) {
            [Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit
        }
        else {
            [Security.AccessControl.InheritanceFlags]::None
        }
        $expected = @(
            [pscustomobject]@{ Sid = 'S-1-5-18'; Rights = [Security.AccessControl.FileSystemRights]::FullControl },
            [pscustomobject]@{ Sid = $administratorsSid; Rights = [Security.AccessControl.FileSystemRights]::FullControl }
        )
        if (-not [string]::IsNullOrWhiteSpace($ServiceSid)) {
            $rights = if ($Profile -eq 'Runtime') {
                [Security.AccessControl.FileSystemRights]::ReadAndExecute
            }
            else {
                [Security.AccessControl.FileSystemRights]::Modify
            }
            $expected += [pscustomobject]@{ Sid = $ServiceSid; Rights = $rights }
        }
        $actual = @($acl.Access)
        if ($actual.Count -ne $expected.Count -or
            @($actual | Where-Object { $_.IsInherited -or $_.AccessControlType -ne [Security.AccessControl.AccessControlType]::Allow }).Count -gt 0) {
            return $false
        }
        foreach ($expectedRule in $expected) {
            $matches = @($actual | Where-Object {
                $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -eq $expectedRule.Sid -and
                [int]$_.FileSystemRights -eq [int]$expectedRule.Rights -and
                $_.InheritanceFlags -eq $inheritanceFlags -and
                $_.PropagationFlags -eq [Security.AccessControl.PropagationFlags]::None
            })
            if ($matches.Count -ne 1) {
                return $false
            }
        }
        return $true
    }
    catch {
        return $false
    }
}

function Get-OfficeManagedPostgresServiceSid {
    param([Parameter(Mandatory = $true)]$Context)

    $service = Get-OfficeService -ServiceName ([string]$Context.Distribution.serviceName)
    if ($null -eq $service) {
        return $null
    }
    if (-not (Test-OfficePostgresServiceOwnership -Context $Context)) {
        throw "The PostgreSQL service ownership check failed before ACL convergence."
    }
    return Get-OfficeServiceSid -ServiceName ([string]$Context.Distribution.serviceName)
}

function Set-OfficeDatabasePathPermissions {
    param([Parameter(Mandatory = $true)]$Context)

    $serviceSid = Get-OfficeManagedPostgresServiceSid -Context $Context
    $managedRoots = @(
        @{ Path = $Context.Paths.RuntimeRoot; Profile = 'Runtime'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DataRoot; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DataDirectory; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DatabaseLogDirectory; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.SecretDirectory; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = $Context.Paths.StateDirectory; Profile = 'Secrets'; ServiceSid = $null }
    )
    foreach ($entry in $managedRoots) {
        if (Test-Path -LiteralPath $entry.Path) {
            Set-OfficeRestrictedAcl -Path $entry.Path -ServiceSid $entry.ServiceSid -Profile $entry.Profile
        }
    }

    $protectedFiles = @(
        $Context.Paths.AdminPassfilePath,
        $Context.Paths.MigratorPassfilePath,
        $Context.Paths.ApplicationPassfilePath,
        $Context.Paths.StateFilePath,
        $Context.Paths.InitializationReceiptPath,
        $Context.Paths.ActivationFilePath,
        (Join-Path $Context.Paths.RuntimeRoot '.safarsuite-runtime-receipt.json')
    )
    $protectedFileSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($filePath in $protectedFiles) {
        [void]$protectedFileSet.Add([IO.Path]::GetFullPath($filePath))
    }

    foreach ($entry in $managedRoots) {
        if (-not (Test-Path -LiteralPath $entry.Path -PathType Container)) {
            continue
        }
        $excludedSubtrees = if ([IO.Path]::GetFullPath($entry.Path) -eq [IO.Path]::GetFullPath($Context.Paths.DataRoot) -and
            (Test-Path -LiteralPath $Context.Paths.DataDirectory -PathType Container)) {
            @($Context.Paths.DataDirectory)
        }
        else {
            @()
        }
        foreach ($descendantPath in @(Get-OfficeManagedTreeEntriesNoReparse -Root $entry.Path -ExcludedSubtrees $excludedSubtrees)) {
            if ($protectedFileSet.Contains([IO.Path]::GetFullPath($descendantPath))) {
                continue
            }
            Set-OfficeInheritedAcl -Path $descendantPath
        }
    }

    foreach ($filePath in $protectedFiles) {
        if (Test-Path -LiteralPath $filePath -PathType Leaf) {
            Set-OfficeRestrictedAcl -Path $filePath -Profile Secrets
        }
    }
}

function Test-OfficeDatabasePathPermissions {
    param([Parameter(Mandatory = $true)]$Context)

    try {
        $serviceSid = Get-OfficeManagedPostgresServiceSid -Context $Context
    }
    catch {
        return $false
    }
    $managedRoots = @(
        @{ Path = $Context.Paths.RuntimeRoot; Profile = 'Runtime'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DataRoot; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DataDirectory; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.DatabaseLogDirectory; Profile = 'Data'; ServiceSid = $serviceSid },
        @{ Path = $Context.Paths.SecretDirectory; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = $Context.Paths.StateDirectory; Profile = 'Secrets'; ServiceSid = $null }
    )
    foreach ($entry in $managedRoots) {
        if (-not (Test-Path -LiteralPath $entry.Path -PathType Container) -or
            -not (Test-OfficeRestrictedAcl -Path $entry.Path -ServiceSid $entry.ServiceSid -Profile $entry.Profile)) {
            return $false
        }
    }

    $requiredProtectedFiles = @(
        @{ Path = $Context.Paths.AdminPassfilePath; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = $Context.Paths.MigratorPassfilePath; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = $Context.Paths.ApplicationPassfilePath; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = $Context.Paths.StateFilePath; Profile = 'Secrets'; ServiceSid = $null },
        @{ Path = (Join-Path $Context.Paths.RuntimeRoot '.safarsuite-runtime-receipt.json'); Profile = 'Secrets'; ServiceSid = $null }
    )
    foreach ($entry in $requiredProtectedFiles) {
        if (-not (Test-Path -LiteralPath $entry.Path) -or
            -not (Test-OfficeRestrictedAcl -Path $entry.Path -ServiceSid $entry.ServiceSid -Profile $entry.Profile)) {
            return $false
        }
    }

    $optionalProtectedFiles = @(
        $Context.Paths.InitializationReceiptPath,
        $Context.Paths.ActivationFilePath
    )
    foreach ($filePath in $optionalProtectedFiles) {
        if ((Test-Path -LiteralPath $filePath -PathType Leaf) -and
            -not (Test-OfficeRestrictedAcl -Path $filePath -Profile Secrets)) {
            return $false
        }
    }

    $protectedFileSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $requiredProtectedFiles) {
        [void]$protectedFileSet.Add([IO.Path]::GetFullPath($entry.Path))
    }
    foreach ($filePath in $optionalProtectedFiles) {
        [void]$protectedFileSet.Add([IO.Path]::GetFullPath($filePath))
    }
    try {
        foreach ($entry in $managedRoots) {
            $excludedSubtrees = if ([IO.Path]::GetFullPath($entry.Path) -eq [IO.Path]::GetFullPath($Context.Paths.DataRoot)) {
                @($Context.Paths.DataDirectory)
            }
            else {
                @()
            }
            foreach ($descendantPath in @(Get-OfficeManagedTreeEntriesNoReparse -Root $entry.Path -ExcludedSubtrees $excludedSubtrees)) {
                if ($protectedFileSet.Contains([IO.Path]::GetFullPath($descendantPath))) {
                    continue
                }
                if (-not (Test-OfficeInheritedAcl -Path $descendantPath -ServiceSid $entry.ServiceSid -Profile $entry.Profile)) {
                    return $false
                }
            }
        }
    }
    catch {
        return $false
    }
    return $true
}

function Get-OfficeServiceSid {
    param([Parameter(Mandatory = $true)][string]$ServiceName)

    $account = [Security.Principal.NTAccount]::new("NT SERVICE", $ServiceName)
    return $account.Translate([Security.Principal.SecurityIdentifier]).Value
}

function Write-OfficePgPassFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Database,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$Password
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    $escapedPassword = $Password.Replace('\', '\\').Replace(':', '\:')
    Set-OfficeAtomicUtf8NoBomContent -Path $Path -Value "127.0.0.1:$Port`:$Database`:$Role`:$escapedPassword"
    Set-OfficeRestrictedAcl -Path $Path -Profile Secrets
}

function Read-OfficePgPassPassword {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }
    $parts = (Get-Content -Raw -LiteralPath $Path) -split ':', 5
    if ($parts.Count -ne 5 -or [string]::IsNullOrWhiteSpace($parts[4])) {
        return $null
    }
    return $parts[4].Replace('\:', ':').Replace('\\', '\')
}

function Invoke-OfficePsql {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Role,
        [Parameter(Mandatory = $true)][string]$Database,
        [Parameter(Mandatory = $true)][string]$Passfile,
        [Parameter(Mandatory = $true)][string]$Sql,
        [switch]$AllowFailure
    )

    $psqlPath = Join-Path $Context.Paths.RuntimeRoot "bin\psql.exe"
    return Invoke-OfficeNativeCommand `
        -FilePath $psqlPath `
        -Arguments @(
            '-X', '-q', '-v', 'ON_ERROR_STOP=1', '-tA',
            '-h', '127.0.0.1', '-p', [string]$Context.Distribution.port,
            '-U', $Role, '-d', $Database
        ) `
        -Environment @{ PGPASSFILE = $Passfile } `
        -StandardInput $Sql `
        -TimeoutSeconds 120 `
        -AllowFailure:$AllowFailure
}

function Get-OfficeService {
    param([Parameter(Mandatory = $true)][string]$ServiceName)

    return Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
}

function Get-OfficeServiceConfiguration {
    param([Parameter(Mandatory = $true)][string]$ServiceName)

    $escapedName = $ServiceName.Replace("'", "''")
    return Get-CimInstance -ClassName Win32_Service -Filter "Name='$escapedName'" -ErrorAction SilentlyContinue
}

function Test-OfficeExactPostgresServiceCommandLine {
    param(
        [Parameter(Mandatory = $true)][string]$PathName,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutablePath,
        [Parameter(Mandatory = $true)][string]$ExpectedServiceName,
        [Parameter(Mandatory = $true)][string]$ExpectedDataDirectory
    )

    try {
        $tokens = @([SafarSuiteControlDeskCommandLine]::Split($PathName))
        $serviceExecutablePath = [IO.Path]::GetFullPath($tokens[0])
        $expectedExecutable = [IO.Path]::GetFullPath($ExpectedExecutablePath)
    }
    catch {
        return $false
    }
    if ($tokens.Count -lt 7 -or
        $serviceExecutablePath -ne $expectedExecutable -or
        $tokens[1] -ne 'runservice') {
        return $false
    }

    $serviceName = $null
    $dataDirectory = $null
    $waitForStartup = $false
    for ($index = 2; $index -lt $tokens.Count; $index++) {
        switch -CaseSensitive ($tokens[$index]) {
            '-N' {
                if ($null -ne $serviceName -or $index + 1 -ge $tokens.Count) { return $false }
                $serviceName = $tokens[++$index]
            }
            '-D' {
                if ($null -ne $dataDirectory -or $index + 1 -ge $tokens.Count) { return $false }
                $dataDirectory = $tokens[++$index]
            }
            '-w' {
                if ($waitForStartup) { return $false }
                $waitForStartup = $true
            }
            default { return $false }
        }
    }
    try {
        return $serviceName -ceq $ExpectedServiceName -and
            $null -ne $dataDirectory -and
            [IO.Path]::GetFullPath($dataDirectory) -eq [IO.Path]::GetFullPath($ExpectedDataDirectory) -and
            $waitForStartup
    }
    catch {
        return $false
    }
}

function Test-OfficePostgresServiceOwnership {
    param([Parameter(Mandatory = $true)]$Context)

    $configuration = Get-OfficeServiceConfiguration -ServiceName ([string]$Context.Distribution.serviceName)
    if ($null -eq $configuration) {
        return $false
    }
    return Test-OfficeExactPostgresServiceCommandLine `
        -PathName ([string]$configuration.PathName) `
        -ExpectedExecutablePath (Join-Path $Context.Paths.RuntimeRoot 'bin\pg_ctl.exe') `
        -ExpectedServiceName ([string]$Context.Distribution.serviceName) `
        -ExpectedDataDirectory $Context.Paths.DataDirectory
}

function Test-OfficeServiceFailureActions {
    param([Parameter(Mandatory = $true)][byte[]]$Value)

    if ($Value.Length -lt 44) {
        return $false
    }
    $resetPeriod = [BitConverter]::ToUInt32($Value, 0)
    $actionCount = [BitConverter]::ToUInt32($Value, 12)
    $actionOffset = [BitConverter]::ToUInt32($Value, 16)
    if ($resetPeriod -ne 86400 -or $actionCount -ne 3 -or
        $actionOffset -lt 20 -or $Value.Length -lt ($actionOffset + ($actionCount * 8))) {
        return $false
    }
    $expectedDelays = @(5000, 15000, 60000)
    for ($index = 0; $index -lt $actionCount; $index++) {
        $offset = [int]$actionOffset + ([int]$index * 8)
        $actionType = [BitConverter]::ToUInt32($Value, $offset)
        $delay = [BitConverter]::ToUInt32($Value, $offset + 4)
        if ($actionType -ne 1 -or $delay -ne $expectedDelays[$index]) {
            return $false
        }
    }
    return $true
}

function Test-OfficePostgresServiceConfiguration {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [ValidateSet('Auto', 'Manual')][string]$ExpectedStartMode = 'Auto'
    )

    $configuration = Get-OfficeServiceConfiguration -ServiceName ([string]$Context.Distribution.serviceName)
    if ($null -eq $configuration -or
        $configuration.StartName -ne [string]$Context.Distribution.serviceAccount -or
        $configuration.DisplayName -cne [string]$Context.Distribution.serviceDisplayName -or
        $configuration.StartMode -ne $ExpectedStartMode) {
        return $false
    }
    $serviceRegistryPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$($Context.Distribution.serviceName)"
    $registry = Get-ItemProperty -LiteralPath $serviceRegistryPath -ErrorAction SilentlyContinue
    $dependencies = if ($null -ne $registry -and $null -ne $registry.PSObject.Properties['DependOnService']) {
        @($registry.DependOnService | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    }
    else {
        @()
    }
    return $null -ne $registry -and
        $dependencies.Count -eq 0 -and
        [int]$registry.ServiceSidType -eq 1 -and
        $null -ne $registry.FailureActions -and
        (Test-OfficeServiceFailureActions -Value ([byte[]]$registry.FailureActions)) -and
        [int]$registry.FailureActionsOnNonCrashFailures -eq 1
}

function Set-OfficePostgresServiceConfiguration {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [ValidateSet('Pending', 'Activated')][string]$Mode
    )

    $serviceName = [string]$Context.Distribution.serviceName
    $startMode = if ($Mode -eq 'Activated') { 'auto' } else { 'demand' }
    Invoke-OfficeNativeCommand `
        -FilePath "$env:SystemRoot\System32\sc.exe" `
        -Arguments @('sidtype', $serviceName, 'unrestricted') | Out-Null
    Invoke-OfficeNativeCommand `
        -FilePath "$env:SystemRoot\System32\sc.exe" `
        -Arguments @('failure', $serviceName, 'reset=', '86400', 'actions=', 'restart/5000/restart/15000/restart/60000') | Out-Null
    Invoke-OfficeNativeCommand `
        -FilePath "$env:SystemRoot\System32\sc.exe" `
        -Arguments @('failureflag', $serviceName, '1') | Out-Null
    Invoke-OfficeNativeCommand `
        -FilePath "$env:SystemRoot\System32\sc.exe" `
        -Arguments @(
            'config', $serviceName,
            'start=', $startMode,
            'obj=', [string]$Context.Distribution.serviceAccount,
            'password=', '',
            'depend=', '/',
            'DisplayName=', [string]$Context.Distribution.serviceDisplayName
        ) | Out-Null

    $expectedStartMode = if ($Mode -eq 'Activated') { 'Auto' } else { 'Manual' }
    if (-not (Test-OfficePostgresServiceConfiguration -Context $Context -ExpectedStartMode $expectedStartMode)) {
        throw "Windows did not retain the exact managed PostgreSQL service configuration."
    }
}

function Test-OfficePathIsReparsePoint {
    param([Parameter(Mandatory = $true)][string]$Path)

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    return $null -ne $item -and [bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)
}

function Test-OfficePathChainIsSafe {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($fullPath)
    $current = $root.TrimEnd('\')
    $relative = $fullPath.Substring($root.Length)
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if ([bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                return $false
            }
        }
    }
    return $true
}

function Get-OfficeManagedTreeEntriesNoReparse {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [string[]]$ExcludedSubtrees = @()
    )

    $rootPath = [IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $rootPath -PathType Container)) {
        return @()
    }
    if (-not (Test-OfficePathChainIsSafe -Path $rootPath) -or (Test-OfficePathIsReparsePoint -Path $rootPath)) {
        throw "Managed tree traversal refused a reparse point."
    }

    $excluded = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $ExcludedSubtrees) {
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            [void]$excluded.Add([IO.Path]::GetFullPath($path))
        }
    }

    $queue = [Collections.Generic.Queue[string]]::new()
    $queue.Enqueue($rootPath)
    $entries = [Collections.Generic.List[string]]::new()
    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        foreach ($item in @(Get-ChildItem -LiteralPath $current -Force -ErrorAction Stop)) {
            $itemPath = [IO.Path]::GetFullPath($item.FullName)
            if ([bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Managed tree traversal found a nested reparse point."
            }
            if ($excluded.Contains($itemPath)) {
                continue
            }
            [void]$entries.Add($itemPath)
            if ($item.PSIsContainer) {
                $queue.Enqueue($itemPath)
            }
        }
    }
    return $entries.ToArray()
}

function Set-OfficeInheritedAcl {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (Test-OfficePathIsReparsePoint -Path $Path) {
        throw "ACL convergence refused a reparse point."
    }
    $security = Get-Acl -LiteralPath $Path -ErrorAction Stop
    foreach ($rule in @($security.Access | Where-Object { -not $_.IsInherited })) {
        [void]$security.RemoveAccessRuleSpecific($rule)
    }
    $security.SetAccessRuleProtection($false, $false)
    Set-Acl -LiteralPath $Path -AclObject $security
}

function Test-OfficeInheritedAcl {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ServiceSid,
        [ValidateSet('Secrets', 'Data', 'Runtime')][string]$Profile
    )

    try {
        if (Test-OfficePathIsReparsePoint -Path $Path) {
            return $false
        }
        $security = Get-Acl -LiteralPath $Path -ErrorAction Stop
        if ($security.AreAccessRulesProtected) {
            return $false
        }
        $expected = @(
            [pscustomobject]@{ Sid = 'S-1-5-18'; Rights = [Security.AccessControl.FileSystemRights]::FullControl },
            [pscustomobject]@{ Sid = 'S-1-5-32-544'; Rights = [Security.AccessControl.FileSystemRights]::FullControl }
        )
        if (-not [string]::IsNullOrWhiteSpace($ServiceSid)) {
            $serviceRights = if ($Profile -eq 'Runtime') {
                [Security.AccessControl.FileSystemRights]::ReadAndExecute
            }
            else {
                [Security.AccessControl.FileSystemRights]::Modify
            }
            $expected += [pscustomobject]@{ Sid = $ServiceSid; Rights = $serviceRights }
        }
        $actual = @($security.Access)
        if ($actual.Count -ne $expected.Count -or
            @($actual | Where-Object { -not $_.IsInherited -or $_.AccessControlType -ne [Security.AccessControl.AccessControlType]::Allow }).Count -gt 0) {
            return $false
        }
        foreach ($expectedRule in $expected) {
            $matches = @($actual | Where-Object {
                $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value -eq $expectedRule.Sid -and
                [int]$_.FileSystemRights -eq [int]$expectedRule.Rights
            })
            if ($matches.Count -ne 1) {
                return $false
            }
        }
        return $true
    }
    catch {
        return $false
    }
}

function Remove-OfficeDirectoryTreeNoReparse {
    param([Parameter(Mandatory = $true)][string]$Root)

    $rootPath = [IO.Path]::GetFullPath($Root)
    if (-not (Test-Path -LiteralPath $rootPath)) {
        return
    }
    $entries = @(Get-OfficeManagedTreeEntriesNoReparse -Root $rootPath)
    foreach ($entryPath in @($entries | Sort-Object { $_.Length } -Descending)) {
        $item = Get-Item -LiteralPath $entryPath -Force -ErrorAction Stop
        if ([bool]($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Managed tree deletion found a nested reparse point."
        }
        Remove-Item -LiteralPath $entryPath -Force
    }
    $rootItem = Get-Item -LiteralPath $rootPath -Force -ErrorAction Stop
    if ([bool]($rootItem.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Managed tree deletion refused a reparse point."
    }
    Remove-Item -LiteralPath $rootPath -Force
}

function Test-OfficeManagedPathSafety {
    param([Parameter(Mandatory = $true)]$Context)

    foreach ($path in @(
        $Context.Paths.ProgramFilesRoot,
        $Context.Paths.ProgramDataRoot,
        $Context.Paths.RuntimeRoot,
        $Context.Paths.DataRoot,
        $Context.Paths.DataDirectory,
        $Context.Paths.DatabaseLogDirectory,
        $Context.Paths.SecretDirectory,
        $Context.Paths.StateDirectory
    )) {
        if (-not (Test-OfficePathChainIsSafe -Path $path)) {
            return $false
        }
    }
    return $true
}

function Get-OfficeClusterSystemIdentifier {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [string]$DataDirectory = $Context.Paths.DataDirectory
    )

    $controlDataPath = Join-Path $Context.Paths.RuntimeRoot 'bin\pg_controldata.exe'
    if (-not (Test-Path -LiteralPath $controlDataPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $DataDirectory 'PG_VERSION') -PathType Leaf)) {
        return $null
    }
    try {
        $controlData = Invoke-OfficeNativeCommand -FilePath $controlDataPath -Arguments @($DataDirectory)
        $line = @($controlData.StandardOutput -split "`r?`n" | Where-Object { $_ -match '^Database system identifier\s*:' } | Select-Object -First 1)
        if ($line.Count -ne 1) {
            return $null
        }
        $identifier = ($line[0] -split ':', 2)[1].Trim()
        if ($identifier -notmatch '^[0-9]{10,32}$') {
            return $null
        }
        return $identifier
    }
    catch {
        return $null
    }
}

function Read-OfficeInitializationReceipt {
    param([Parameter(Mandatory = $true)]$Context)

    if (-not (Test-Path -LiteralPath $Context.Paths.InitializationReceiptPath -PathType Leaf)) {
        return $null
    }
    try {
        $receipt = Get-Content -Raw -LiteralPath $Context.Paths.InitializationReceiptPath | ConvertFrom-Json
        if ($receipt.product -ne $script:LifecycleProduct -or
            $receipt.schemaVersion -ne $script:LifecycleSchemaVersion -or
            $receipt.phase -notin @('Initializing', 'Promoting') -or
            [string]$receipt.nonce -notmatch '^[0-9a-f]{32}$' -or
            [IO.Path]::GetFullPath([string]$receipt.dataDirectory) -ne [IO.Path]::GetFullPath($Context.Paths.DataDirectory) -or
            [IO.Path]::GetFullPath([string]$receipt.stagingDataDirectory) -ne [IO.Path]::GetFullPath("$($Context.Paths.DataDirectory).initializing-$($receipt.nonce)") -or
            [IO.Path]::GetFullPath([string]$receipt.bootstrapPasswordPath) -ne [IO.Path]::GetFullPath((Join-Path $Context.Paths.SecretDirectory "bootstrap-$($receipt.nonce).txt"))) {
            return $null
        }
        if ($receipt.phase -eq 'Promoting' -and [string]$receipt.clusterSystemIdentifier -notmatch '^[0-9]{10,32}$') {
            return $null
        }
        return $receipt
    }
    catch {
        return $null
    }
}

function Test-OfficeManagedClusterMarker {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [switch]$RequireLiveIdentity
    )

    if (-not (Test-Path -LiteralPath $Context.Paths.StateFilePath -PathType Leaf)) {
        return $false
    }
    try {
        $state = Get-Content -Raw -LiteralPath $Context.Paths.StateFilePath | ConvertFrom-Json
        $valid = $state.product -eq $script:LifecycleProduct -and
            $state.schemaVersion -eq $script:LifecycleSchemaVersion -and
            $state.serviceName -eq [string]$Context.Distribution.serviceName -and
            [IO.Path]::GetFullPath([string]$state.dataDirectory) -eq [IO.Path]::GetFullPath($Context.Paths.DataDirectory) -and
            [string]$state.postgresVersion -eq [string]$Context.Distribution.version -and
            [string]$state.runtimeSha256 -eq [string]$Context.PackageManifest.postgresql.runtimeSha256 -and
            [string]$state.clusterSystemIdentifier -match '^[0-9]{10,32}$'
        if (-not $valid) {
            return $false
        }
        if ($RequireLiveIdentity) {
            $liveIdentifier = Get-OfficeClusterSystemIdentifier -Context $Context
            return $null -ne $liveIdentifier -and $liveIdentifier -ceq [string]$state.clusterSystemIdentifier
        }
        return $true
    }
    catch {
        return $false
    }
}

function Test-OfficeDatabaseActivationReceipt {
    param([Parameter(Mandatory = $true)]$Context)

    if (-not (Test-Path -LiteralPath $Context.Paths.ActivationFilePath -PathType Leaf)) {
        return $false
    }
    try {
        $activation = Get-Content -Raw -LiteralPath $Context.Paths.ActivationFilePath | ConvertFrom-Json
        return $activation.schemaVersion -eq $script:LifecycleSchemaVersion -and
            $activation.product -eq $script:LifecycleProduct -and
            $activation.packageSourceRevision -eq [string]$Context.PackageManifest.sourceRevision -and
            $activation.postgresVersion -eq [string]$Context.Distribution.version -and
            $activation.serviceName -eq [string]$Context.Distribution.serviceName -and
            [int]$activation.migrationCount -eq [int]$Context.PackageManifest.migrations.count -and
            $activation.migrationTarget -eq [string]$Context.PackageManifest.migrations.target -and
            $activation.requiredExtension -eq 'pg_trgm' -and
            (@($activation.listenerAddresses) -join '|') -eq '127.0.0.1'
    }
    catch {
        return $false
    }
}

function Test-OfficeConfiguration {
    param([Parameter(Mandatory = $true)]$Context)

    foreach ($path in @($Context.Paths.PostgresConfigurationPath, $Context.Paths.HbaConfigurationPath)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            return $false
        }
    }

    $postgresConfig = Get-Content -Raw -LiteralPath $Context.Paths.PostgresConfigurationPath
    $hbaConfig = Get-Content -Raw -LiteralPath $Context.Paths.HbaConfigurationPath
    return $postgresConfig -ceq (Get-OfficeExpectedPostgresConfiguration -Context $Context) -and
        $hbaConfig -ceq (Get-OfficeExpectedHbaConfiguration -Context $Context)
}

function Get-OfficeExpectedPostgresConfiguration {
    param([Parameter(Mandatory = $true)]$Context)

    $logPath = (ConvertTo-OfficeForwardSlashPath -Path $Context.Paths.DatabaseLogDirectory).Replace("'", "''")
    return (Get-Content -Raw -LiteralPath $Context.Paths.PostgresConfigurationTemplatePath).
        Replace('{{PORT}}', [string]$Context.Distribution.port).
        Replace('{{LOG_DIRECTORY}}', $logPath)
}

function Get-OfficeExpectedHbaConfiguration {
    param([Parameter(Mandatory = $true)]$Context)

    return (Get-Content -Raw -LiteralPath $Context.Paths.HbaConfigurationTemplatePath).
        Replace('{{ADMIN_ROLE}}', [string]$Context.Distribution.adminRole).
        Replace('{{MIGRATOR_ROLE}}', [string]$Context.Distribution.migratorRole).
        Replace('{{APPLICATION_ROLE}}', [string]$Context.Distribution.applicationRole).
        Replace('{{DATABASE_NAME}}', [string]$Context.Distribution.databaseName)
}

function Get-OfficeAppliedMigrations {
    param([Parameter(Mandatory = $true)]$Context)

    $historyTable = Invoke-OfficePsql `
        -Context $Context `
        -Role ([string]$Context.Distribution.migratorRole) `
        -Database ([string]$Context.Distribution.databaseName) `
        -Passfile $Context.Paths.MigratorPassfilePath `
        -Sql "SELECT to_regclass('control.__ef_migrations_history');"
    if ([string]::IsNullOrWhiteSpace($historyTable.StandardOutput)) {
        return @()
    }
    $result = Invoke-OfficePsql `
        -Context $Context `
        -Role ([string]$Context.Distribution.migratorRole) `
        -Database ([string]$Context.Distribution.databaseName) `
        -Passfile $Context.Paths.MigratorPassfilePath `
        -Sql 'SELECT "MigrationId" FROM control.__ef_migrations_history ORDER BY "MigrationId";'
    return @($result.StandardOutput -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Compare-OfficeMigrationLedger {
    param(
        [Parameter(Mandatory = $true)][string[]]$Expected,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Applied
    )

    if ($Applied.Count -gt $Expected.Count) {
        return 'Diverged'
    }
    for ($index = 0; $index -lt $Applied.Count; $index++) {
        if ($Applied[$index] -ne $Expected[$index]) {
            return 'Diverged'
        }
    }
    if ($Applied.Count -eq $Expected.Count) {
        return 'Exact'
    }
    return 'Prefix'
}

function Test-OfficeInstalledRuntimeIntegrity {
    param([Parameter(Mandatory = $true)]$Context)

    if (-not (Test-Path -LiteralPath $Context.Paths.RuntimeRoot -PathType Container)) {
        return $false
    }
    $receiptPath = Join-Path $Context.Paths.RuntimeRoot '.safarsuite-runtime-receipt.json'
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        return $false
    }
    try {
        $receipt = Get-Content -Raw -LiteralPath $receiptPath | ConvertFrom-Json
        $runtimeRoot = [IO.Path]::GetFullPath($Context.Paths.RuntimeRoot)
        $locationValid = ($receipt.phase -eq 'Installed' -and
                [IO.Path]::GetFullPath([string]$receipt.installedDirectory) -eq $runtimeRoot) -or
            ($receipt.phase -eq 'Promoting' -and
                ([IO.Path]::GetFullPath([string]$receipt.installedDirectory) -eq $runtimeRoot -or
                 [IO.Path]::GetFullPath([string]$receipt.stagingDirectory) -eq $runtimeRoot))
        if ($receipt.product -ne $script:LifecycleProduct -or
            $receipt.schemaVersion -ne $script:LifecycleSchemaVersion -or
            [string]$receipt.postgresVersion -ne [string]$Context.Distribution.version -or
            [string]$receipt.runtimeSha256 -ne [string]$Context.PackageManifest.postgresql.runtimeSha256 -or
            -not $locationValid) {
            return $false
        }

        $inventoryProperty = $Context.Distribution.PSObject.Properties['runtimeFileSha256']
        if ($null -eq $inventoryProperty -or $null -eq $inventoryProperty.Value) {
            return $false
        }
        $runtimeInventory = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        $inventoryPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $expectedDirectories = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($property in @($inventoryProperty.Value.PSObject.Properties)) {
            $relativeFile = [string]$property.Name
            $expectedHash = ([string]$property.Value).ToUpperInvariant()
            if (-not (Test-OfficeRuntimeRelativePath -Path $relativeFile) -or
                $expectedHash -notmatch '^[0-9A-F]{64}$' -or
                -not $inventoryPaths.Add($relativeFile)) {
                return $false
            }
            $runtimeInventory.Add($relativeFile, $expectedHash)

            $parent = $relativeFile
            while ($parent.LastIndexOf('/') -gt 0) {
                $parent = $parent.Substring(0, $parent.LastIndexOf('/'))
                [void]$expectedDirectories.Add($parent)
            }
        }
        if ($runtimeInventory.Count -lt 1 -or
            [int]$Context.Distribution.runtimeFileCount -ne $runtimeInventory.Count) {
            return $false
        }

        $seenFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $receiptRelativePath = '.safarsuite-runtime-receipt.json'
        foreach ($entryPath in @(Get-OfficeManagedTreeEntriesNoReparse -Root $runtimeRoot)) {
            $fullEntryPath = [IO.Path]::GetFullPath($entryPath)
            if (-not $fullEntryPath.StartsWith($runtimeRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
                return $false
            }
            $relativeEntryPath = $fullEntryPath.Substring($runtimeRoot.TrimEnd('\').Length + 1).Replace('\', '/')
            $item = Get-Item -LiteralPath $fullEntryPath -Force -ErrorAction Stop
            if ($item.PSIsContainer) {
                if (-not $expectedDirectories.Contains($relativeEntryPath)) {
                    return $false
                }
                continue
            }
            if ($relativeEntryPath -ceq $receiptRelativePath) {
                continue
            }
            if (-not $runtimeInventory.ContainsKey($relativeEntryPath) -or
                -not $seenFiles.Add($relativeEntryPath) -or
                (Get-FileHash -Algorithm SHA256 -LiteralPath $fullEntryPath).Hash.ToUpperInvariant() -ne $runtimeInventory[$relativeEntryPath]) {
                return $false
            }
        }
        if ($seenFiles.Count -ne $runtimeInventory.Count) {
            return $false
        }
        return $true
    }
    catch {
        return $false
    }
}

function Set-OfficeInstalledRuntimeReceipt {
    param([Parameter(Mandatory = $true)]$Context)

    $receiptPath = Join-Path $Context.Paths.RuntimeRoot '.safarsuite-runtime-receipt.json'
    Set-OfficeAtomicJsonFile -Path $receiptPath -Value ([ordered]@{
        schemaVersion = $script:LifecycleSchemaVersion
        product = $script:LifecycleProduct
        phase = 'Installed'
        installedDirectory = $Context.Paths.RuntimeRoot
        postgresVersion = [string]$Context.Distribution.version
        runtimeSha256 = [string]$Context.PackageManifest.postgresql.runtimeSha256
    })
    Set-OfficeRestrictedAcl -Path $receiptPath -Profile Secrets
}

function New-OfficeClusterStateReceipt {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$ClusterSystemIdentifier,
        [string]$CreatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    )

    return [ordered]@{
        schemaVersion = $script:LifecycleSchemaVersion
        product = $script:LifecycleProduct
        createdAtUtc = $CreatedAtUtc
        serviceName = [string]$Context.Distribution.serviceName
        dataDirectory = $Context.Paths.DataDirectory
        postgresVersion = [string]$Context.Distribution.version
        runtimeSha256 = [string]$Context.PackageManifest.postgresql.runtimeSha256
        clusterSystemIdentifier = $ClusterSystemIdentifier
    }
}

function Remove-OfficeOwnedDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedParent
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    $parent = [IO.Path]::GetFullPath($ExpectedParent).TrimEnd('\')
    if (-not $fullPath.StartsWith($parent + '\', [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-OfficePathChainIsSafe -Path $fullPath)) {
        throw "Managed database cleanup refused a path outside its exact non-reparse ownership boundary."
    }
    if (Test-Path -LiteralPath $fullPath) {
        Remove-OfficeDirectoryTreeNoReparse -Root $fullPath
    }
}

function Invoke-OfficeDatabaseTestFault {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Point
    )

    if ([Environment]::GetEnvironmentVariable('SAFARSUITE_OFFICE_DATABASE_FAULT_POINT', 'Process') -ne $Point) {
        return
    }
    $runnerTemp = [Environment]::GetEnvironmentVariable('RUNNER_TEMP', 'Process')
    if ([Environment]::GetEnvironmentVariable('GITHUB_ACTIONS', 'Process') -ne 'true' -or
        [string]::IsNullOrWhiteSpace($runnerTemp)) {
        throw "Database lifecycle fault injection is restricted to disposable GitHub Actions runners."
    }
    $runnerRoot = [IO.Path]::GetFullPath($runnerTemp).TrimEnd('\')
    foreach ($path in @($Context.Paths.ProgramFilesRoot, $Context.Paths.ProgramDataRoot)) {
        if (-not [IO.Path]::GetFullPath($path).StartsWith($runnerRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw "Database lifecycle fault injection refused a path outside RUNNER_TEMP."
        }
    }
    throw "Injected disposable database lifecycle failure at '$Point'."
}

function Test-OfficeEffectiveDatabaseConfiguration {
    param([Parameter(Mandatory = $true)]$Context)

    $autoConfigurationPath = Join-Path $Context.Paths.DataDirectory 'postgresql.auto.conf'
    if (Test-Path -LiteralPath $autoConfigurationPath -PathType Leaf) {
        $activeOverrides = @(Get-Content -LiteralPath $autoConfigurationPath | Where-Object {
            $_ -notmatch '^\s*(#|$)'
        })
        if ($activeOverrides.Count -gt 0) {
            return $false
        }
    }
    try {
        $settings = Invoke-OfficePsql `
            -Context $Context `
            -Role ([string]$Context.Distribution.adminRole) `
            -Database ([string]$Context.Distribution.databaseName) `
            -Passfile $Context.Paths.AdminPassfilePath `
            -Sql @"
SELECT current_setting('listen_addresses') || chr(9) ||
       current_setting('port') || chr(9) ||
       current_setting('password_encryption') || chr(9) ||
       current_setting('ssl') || chr(9) ||
       current_setting('hba_file') || chr(9) ||
       current_setting('config_file') || chr(9) ||
       current_setting('data_directory');
"@
        $parts = $settings.StandardOutput.Trim() -split "`t", 7
        if ($parts.Count -ne 7) {
            return $false
        }
        $expectedHba = ConvertTo-OfficeForwardSlashPath -Path $Context.Paths.HbaConfigurationPath
        $expectedConfig = ConvertTo-OfficeForwardSlashPath -Path $Context.Paths.PostgresConfigurationPath
        $expectedData = ConvertTo-OfficeForwardSlashPath -Path $Context.Paths.DataDirectory
        if ($parts[0] -ne '127.0.0.1' -or
            $parts[1] -ne [string]$Context.Distribution.port -or
            $parts[2] -ne 'scram-sha-256' -or
            $parts[3] -ne 'off' -or
            $parts[4].Replace('\', '/') -ne $expectedHba -or
            $parts[5].Replace('\', '/') -ne $expectedConfig -or
            $parts[6].Replace('\', '/') -ne $expectedData) {
            return $false
        }
        $hbaErrors = Invoke-OfficePsql `
            -Context $Context `
            -Role ([string]$Context.Distribution.adminRole) `
            -Database ([string]$Context.Distribution.databaseName) `
            -Passfile $Context.Paths.AdminPassfilePath `
            -Sql 'SELECT count(*) FROM pg_hba_file_rules WHERE error IS NOT NULL;'
        return $hbaErrors.StandardOutput.Trim() -eq '0'
    }
    catch {
        return $false
    }
}

function Test-OfficeDatabaseSecurityState {
    param([Parameter(Mandatory = $true)]$Context)

    $adminRole = [string]$Context.Distribution.adminRole
    $migratorRole = [string]$Context.Distribution.migratorRole
    $applicationRole = [string]$Context.Distribution.applicationRole
    $databaseName = [string]$Context.Distribution.databaseName
    try {
        $applicationIdentity = Invoke-OfficePsql `
            -Context $Context `
            -Role $applicationRole `
            -Database $databaseName `
            -Passfile $Context.Paths.ApplicationPassfilePath `
            -Sql 'SELECT current_user;'
        if ($applicationIdentity.StandardOutput.Trim() -cne $applicationRole) {
            return $false
        }

        $security = Invoke-OfficePsql `
            -Context $Context `
            -Role $adminRole `
            -Database $databaseName `
            -Passfile $Context.Paths.AdminPassfilePath `
            -Sql @"
WITH role_ids AS (
    SELECT
        (SELECT oid FROM pg_roles WHERE rolname = '$migratorRole') AS migrator_oid,
        (SELECT oid FROM pg_roles WHERE rolname = '$applicationRole') AS application_oid
),
managed_schemas AS (
    SELECT oid, nspname, nspowner, nspacl
    FROM pg_namespace
    WHERE nspname <> 'information_schema'
      AND nspname NOT LIKE 'pg\_%' ESCAPE '\'
),
required_defaults(object_type, privilege_type) AS (
    VALUES
        ('r', 'SELECT'), ('r', 'INSERT'), ('r', 'UPDATE'), ('r', 'DELETE'),
        ('S', 'USAGE'), ('S', 'SELECT'), ('S', 'UPDATE')
)
SELECT CASE WHEN
    (SELECT count(*) FROM pg_authid
     WHERE rolname IN ('$migratorRole', '$applicationRole')
       AND rolcanlogin
       AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole
       AND NOT rolreplication AND NOT rolbypassrls
       AND rolinherit AND rolconnlimit = -1
       AND COALESCE(rolvaliduntil, 'infinity'::timestamptz) = 'infinity'::timestamptz
       AND rolpassword LIKE 'SCRAM-SHA-256`$%') = 2
    AND NOT EXISTS (
        SELECT 1 FROM pg_auth_members, role_ids
        WHERE member IN (migrator_oid, application_oid)
           OR roleid IN (migrator_oid, application_oid))
    AND (SELECT datdba = migrator_oid FROM pg_database, role_ids WHERE datname = '$databaseName')
    AND NOT EXISTS (
        SELECT 1
        FROM pg_database d
        CROSS JOIN LATERAL aclexplode(COALESCE(d.datacl, acldefault('d', d.datdba))) privilege
        WHERE d.datname = '$databaseName' AND privilege.grantee = 0)
    AND (SELECT has_database_privilege(application_oid, d.oid, 'CONNECT')
                AND NOT has_database_privilege(application_oid, d.oid, 'CONNECT WITH GRANT OPTION')
                AND NOT has_database_privilege(application_oid, d.oid, 'CREATE')
                AND NOT has_database_privilege(application_oid, d.oid, 'TEMPORARY')
         FROM pg_database d, role_ids WHERE d.datname = '$databaseName')
    AND NOT EXISTS (
        SELECT 1 FROM managed_schemas, role_ids
        WHERE NOT has_schema_privilege(application_oid, oid, 'USAGE')
           OR has_schema_privilege(application_oid, oid, 'USAGE WITH GRANT OPTION')
           OR has_schema_privilege(application_oid, oid, 'CREATE')
           OR (SELECT nspowner = application_oid FROM pg_namespace WHERE pg_namespace.oid = managed_schemas.oid))
    AND NOT EXISTS (
        SELECT 1
        FROM managed_schemas schema
        CROSS JOIN LATERAL aclexplode(COALESCE(schema.nspacl, acldefault('n', schema.nspowner))) privilege
        WHERE privilege.grantee = 0)
    AND NOT EXISTS (
        SELECT 1
        FROM pg_class object
        JOIN managed_schemas schema ON schema.oid = object.relnamespace
        CROSS JOIN role_ids
        WHERE object.relkind IN ('r', 'p')
          AND object.relowner = application_oid)
    AND NOT EXISTS (
        SELECT 1
        FROM pg_class object
        JOIN managed_schemas schema ON schema.oid = object.relnamespace
        CROSS JOIN role_ids
        WHERE object.relkind IN ('r', 'p')
          AND NOT (schema.nspname = 'control' AND object.relname = '__ef_migrations_history')
          AND NOT (
              has_table_privilege(application_oid, object.oid, 'SELECT')
              AND has_table_privilege(application_oid, object.oid, 'INSERT')
              AND has_table_privilege(application_oid, object.oid, 'UPDATE')
              AND has_table_privilege(application_oid, object.oid, 'DELETE')
              AND NOT has_table_privilege(application_oid, object.oid, 'TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
              AND NOT has_table_privilege(application_oid, object.oid, 'SELECT WITH GRANT OPTION,INSERT WITH GRANT OPTION,UPDATE WITH GRANT OPTION,DELETE WITH GRANT OPTION')))
    AND (SELECT has_table_privilege(application_oid, 'control.__ef_migrations_history', 'SELECT')
                AND NOT has_table_privilege(application_oid, 'control.__ef_migrations_history', 'SELECT WITH GRANT OPTION')
                AND NOT has_table_privilege(application_oid, 'control.__ef_migrations_history', 'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER,MAINTAIN')
         FROM role_ids)
    AND NOT EXISTS (
        SELECT 1
        FROM pg_class object
        JOIN managed_schemas schema ON schema.oid = object.relnamespace
        CROSS JOIN role_ids
        WHERE object.relkind = 'S'
          AND NOT (
              has_sequence_privilege(application_oid, object.oid, 'USAGE')
              AND has_sequence_privilege(application_oid, object.oid, 'SELECT')
              AND has_sequence_privilege(application_oid, object.oid, 'UPDATE')
              AND NOT has_sequence_privilege(application_oid, object.oid, 'USAGE WITH GRANT OPTION,SELECT WITH GRANT OPTION,UPDATE WITH GRANT OPTION')))
    AND NOT EXISTS (
        SELECT 1
        FROM pg_class object
        JOIN managed_schemas schema ON schema.oid = object.relnamespace
        CROSS JOIN LATERAL aclexplode(object.relacl) privilege
        WHERE object.relkind IN ('r', 'p', 'S')
          AND privilege.grantee = 0)
    AND NOT EXISTS (
        SELECT 1
        FROM managed_schemas schema
        CROSS JOIN required_defaults required
        CROSS JOIN role_ids
        WHERE NOT EXISTS (
            SELECT 1
            FROM pg_default_acl defaults
            CROSS JOIN LATERAL aclexplode(defaults.defaclacl) privilege
            WHERE defaults.defaclrole = migrator_oid
              AND defaults.defaclnamespace = schema.oid
              AND defaults.defaclobjtype = required.object_type::"char"
              AND privilege.grantee = application_oid
              AND privilege.privilege_type = required.privilege_type))
    AND NOT EXISTS (
        SELECT 1
        FROM pg_default_acl defaults
        JOIN managed_schemas schema ON schema.oid = defaults.defaclnamespace
        CROSS JOIN LATERAL aclexplode(defaults.defaclacl) privilege
        CROSS JOIN role_ids
        WHERE defaults.defaclrole = migrator_oid
          AND (
              privilege.grantee = 0
              OR (privilege.grantee = application_oid AND (
                  privilege.is_grantable
                  OR defaults.defaclobjtype NOT IN ('r'::"char", 'S'::"char")
                  OR (defaults.defaclobjtype = 'r'::"char" AND privilege.privilege_type NOT IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE'))
                  OR (defaults.defaclobjtype = 'S'::"char" AND privilege.privilege_type NOT IN ('USAGE', 'SELECT', 'UPDATE'))))))
THEN 1 ELSE 0 END;
"@
        return $security.StandardOutput.Trim() -eq '1'
    }
    catch {
        return $false
    }
}

function New-OfficeNativeDatabaseAdapter {
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)]$Context)

    $inspect = {
        param($ctx)
        $clusterVersionPath = Join-Path $ctx.Paths.DataDirectory 'PG_VERSION'
        $clusterExists = Test-Path -LiteralPath $clusterVersionPath -PathType Leaf
        if (-not (Test-OfficeManagedPathSafety -Context $ctx)) {
            return 'UnsafePath'
        }
        $initializationReceiptExists = Test-Path -LiteralPath $ctx.Paths.InitializationReceiptPath -PathType Leaf
        $initializationReceipt = Read-OfficeInitializationReceipt -Context $ctx
        $markerValid = Test-OfficeManagedClusterMarker -Context $ctx
        if ($clusterExists -and -not $markerValid) {
            if ($null -ne $initializationReceipt) {
                return 'InterruptedInitialization'
            }
            return 'ForeignCluster'
        }
        if (-not $clusterExists) {
            if ($initializationReceiptExists) {
                if ($null -eq $initializationReceipt) {
                    return 'UnsafeInitializationState'
                }
                return 'InterruptedInitialization'
            }
            if (Test-Path -LiteralPath $ctx.Paths.DataDirectory) {
                return 'ForeignCluster'
            }
            $collision = @(Get-NetTCPConnection -State Listen -LocalPort ([int]$ctx.Distribution.port) -ErrorAction SilentlyContinue)
            if ($collision.Count -gt 0) {
                return 'PortCollision'
            }
            return 'Absent'
        }
        if ((Get-Content -Raw -LiteralPath $clusterVersionPath).Trim() -ne '17') {
            return 'UnsupportedCluster'
        }
        if ($initializationReceiptExists) {
            if ($null -eq $initializationReceipt) {
                return 'UnsafeInitializationState'
            }
            return 'InterruptedInitialization'
        }
        foreach ($passfile in @($ctx.Paths.AdminPassfilePath, $ctx.Paths.MigratorPassfilePath, $ctx.Paths.ApplicationPassfilePath)) {
            if (-not (Test-Path -LiteralPath $passfile -PathType Leaf)) {
                return 'MissingCredentials'
            }
        }

        $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
        if ($null -eq $service) {
            if ((Test-OfficeInstalledRuntimeIntegrity -Context $ctx) -and
                (Test-OfficeVisualCppRuntime -Context $ctx) -and
                -not (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity)) {
                return 'ForeignCluster'
            }
            return 'MissingService'
        }
        if (-not (Test-OfficeInstalledRuntimeIntegrity -Context $ctx) -or
            -not (Test-OfficeVisualCppRuntime -Context $ctx)) {
            return 'MissingPrerequisite'
        }
        if (-not (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity)) {
            return 'ForeignCluster'
        }
        if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
            return 'ForeignService'
        }
        $activationValid = Test-OfficeDatabaseActivationReceipt -Context $ctx
        $serviceConfigurationValid = if ($activationValid) {
            Test-OfficePostgresServiceConfiguration -Context $ctx -ExpectedStartMode Auto
        }
        else {
            (Test-OfficePostgresServiceConfiguration -Context $ctx -ExpectedStartMode Manual) -or
                (Test-OfficePostgresServiceConfiguration -Context $ctx -ExpectedStartMode Auto)
        }
        if (-not $serviceConfigurationValid) {
            return 'CorruptServiceConfiguration'
        }
        if (-not (Test-OfficeConfiguration -Context $ctx)) {
            return 'CorruptConfiguration'
        }
        if (-not (Test-OfficeDatabasePathPermissions -Context $ctx)) {
            return 'CorruptPermissions'
        }
        if (-not $activationValid) {
            return 'InitializationIncomplete'
        }
        if ($service.Status -ne 'Running') {
            return 'StoppedService'
        }

        $readyPath = Join-Path $ctx.Paths.RuntimeRoot 'bin\pg_isready.exe'
        $ready = Invoke-OfficeNativeCommand `
            -FilePath $readyPath `
            -Arguments @('-h', '127.0.0.1', '-p', [string]$ctx.Distribution.port, '-t', '5') `
            -TimeoutSeconds 10 `
            -AllowFailure
        if ($ready.ExitCode -ne 0) {
            return 'UnavailableDatabase'
        }

        try {
            $applied = @(Get-OfficeAppliedMigrations -Context $ctx)
        }
        catch {
            $adminProbe = Invoke-OfficePsql `
                -Context $ctx `
                -Role ([string]$ctx.Distribution.adminRole) `
                -Database 'postgres' `
                -Passfile $ctx.Paths.AdminPassfilePath `
                -Sql 'SELECT 1;' `
                -AllowFailure
            if ($adminProbe.ExitCode -eq 0) {
                return 'SecurityDrift'
            }
            return 'UnavailableDatabase'
        }
        $comparison = Compare-OfficeMigrationLedger -Expected @($ctx.PackageManifest.migrations.orderedIds) -Applied $applied
        if ($comparison -eq 'Diverged') {
            return 'MigrationDiverged'
        }
        if ($comparison -eq 'Prefix') {
            return 'MigrationMismatch'
        }
        $extension = Invoke-OfficePsql `
            -Context $ctx `
            -Role ([string]$ctx.Distribution.migratorRole) `
            -Database ([string]$ctx.Distribution.databaseName) `
            -Passfile $ctx.Paths.MigratorPassfilePath `
            -Sql "SELECT extname FROM pg_extension WHERE extname = 'pg_trgm';" `
            -AllowFailure
        if ($extension.ExitCode -ne 0 -or $extension.StandardOutput.Trim() -ne 'pg_trgm') {
            return 'MissingExtension'
        }
        if (-not (Test-OfficeEffectiveDatabaseConfiguration -Context $ctx)) {
            return 'CorruptConfiguration'
        }
        if (-not (Test-OfficeDatabaseSecurityState -Context $ctx)) {
            return 'SecurityDrift'
        }
        return 'Ready'
    }

    $stageRuntime = {
        param($ctx)
        Install-OfficeVisualCppRuntime -Context $ctx
        if ((Test-Path -LiteralPath $ctx.Paths.RuntimeRoot) -and
            (Test-OfficeInstalledRuntimeIntegrity -Context $ctx)) {
            Set-OfficeInstalledRuntimeReceipt -Context $ctx
            Set-OfficeDatabasePathPermissions -Context $ctx
            return
        }

        $runtimeParent = Split-Path -Parent $ctx.Paths.RuntimeRoot
        $runtimeLeaf = Split-Path -Leaf $ctx.Paths.RuntimeRoot
        New-Item -ItemType Directory -Force -Path $runtimeParent | Out-Null
        foreach ($abandoned in @(Get-ChildItem -LiteralPath $runtimeParent -Directory -Force -ErrorAction SilentlyContinue | Where-Object {
            $_.Name -match ('^{0}\.initializing-[0-9a-f]{{32}}$' -f [regex]::Escape($runtimeLeaf))
        })) {
            $markerPath = Join-Path $abandoned.FullName '.safarsuite-runtime-receipt.json'
            $owned = $false
            try {
                $marker = Get-Content -Raw -LiteralPath $markerPath | ConvertFrom-Json
                $owned = $marker.product -eq $script:LifecycleProduct -and
                    $marker.phase -eq 'Promoting' -and
                    [IO.Path]::GetFullPath([string]$marker.stagingDirectory) -eq [IO.Path]::GetFullPath($abandoned.FullName) -and
                    [IO.Path]::GetFullPath([string]$marker.installedDirectory) -eq [IO.Path]::GetFullPath($ctx.Paths.RuntimeRoot) -and
                    [string]$marker.runtimeSha256 -eq [string]$ctx.PackageManifest.postgresql.runtimeSha256
            }
            catch { $owned = $false }
            if (-not $owned) {
                throw "An unowned PostgreSQL runtime staging directory occupies the SafarSuite path."
            }
            Remove-OfficeOwnedDirectory -Path $abandoned.FullName -ExpectedParent $runtimeParent
        }

        if (Test-Path -LiteralPath $ctx.Paths.RuntimeRoot) {
            if (-not (Test-OfficeManagedClusterMarker -Context $ctx)) {
                throw "An unmanaged PostgreSQL runtime occupies the SafarSuite path."
            }
            $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
            if ($null -ne $service) {
                if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
                    throw "Runtime repair refused an unmanaged PostgreSQL service."
                }
                if ($service.Status -eq 'Running') {
                    Stop-Service -Name ([string]$ctx.Distribution.serviceName) -Force
                    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
                }
            }
        }

        $nonce = [Guid]::NewGuid().ToString('N')
        $stagingRoot = Join-Path $runtimeParent "$runtimeLeaf.initializing-$nonce"
        New-Item -ItemType Directory -Path $stagingRoot | Out-Null
        Set-OfficeAtomicJsonFile -Path (Join-Path $stagingRoot '.safarsuite-runtime-receipt.json') -Value ([ordered]@{
            schemaVersion = $script:LifecycleSchemaVersion
            product = $script:LifecycleProduct
            phase = 'Promoting'
            stagingDirectory = $stagingRoot
            installedDirectory = $ctx.Paths.RuntimeRoot
            postgresVersion = [string]$ctx.Distribution.version
            runtimeSha256 = [string]$ctx.PackageManifest.postgresql.runtimeSha256
        })
        try {
            [IO.Compression.ZipFile]::ExtractToDirectory($ctx.Paths.RuntimeArchivePath, $stagingRoot)
            $stagingContext = [pscustomobject]@{
                Distribution = $ctx.Distribution
                PackageManifest = $ctx.PackageManifest
                Paths = [pscustomobject]@{ RuntimeRoot = $stagingRoot }
            }
            if (-not (Test-OfficeInstalledRuntimeIntegrity -Context $stagingContext)) {
                throw "The staged PostgreSQL runtime failed installed-file integrity verification."
            }
            Invoke-OfficeDatabaseTestFault -Context $ctx -Point 'AfterRuntimeExtract'
            Set-OfficeRestrictedAcl -Path $stagingRoot -Profile Runtime

            if (Test-Path -LiteralPath $ctx.Paths.RuntimeRoot) {
                $replacedRoot = Join-Path $runtimeParent "$runtimeLeaf.replaced-$nonce"
                Move-Item -LiteralPath $ctx.Paths.RuntimeRoot -Destination $replacedRoot
                try {
                    Move-Item -LiteralPath $stagingRoot -Destination $ctx.Paths.RuntimeRoot
                }
                catch {
                    if (-not (Test-Path -LiteralPath $ctx.Paths.RuntimeRoot) -and (Test-Path -LiteralPath $replacedRoot)) {
                        Move-Item -LiteralPath $replacedRoot -Destination $ctx.Paths.RuntimeRoot
                    }
                    throw
                }
                Remove-OfficeOwnedDirectory -Path $replacedRoot -ExpectedParent $runtimeParent
            }
            else {
                Move-Item -LiteralPath $stagingRoot -Destination $ctx.Paths.RuntimeRoot
            }
            Set-OfficeInstalledRuntimeReceipt -Context $ctx
        }
        finally {
            if (Test-Path -LiteralPath $stagingRoot) {
                Remove-OfficeOwnedDirectory -Path $stagingRoot -ExpectedParent $runtimeParent
            }
        }
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $recoverInitialization = {
        param($ctx)

        $receipt = Read-OfficeInitializationReceipt -Context $ctx
        if ($null -eq $receipt) {
            throw "The database initialization receipt is missing or invalid; automatic recovery was refused."
        }
        $stagingDataDirectory = [IO.Path]::GetFullPath([string]$receipt.stagingDataDirectory)
        $bootstrapPasswordPath = [IO.Path]::GetFullPath([string]$receipt.bootstrapPasswordPath)
        $finalPathExists = Test-Path -LiteralPath $ctx.Paths.DataDirectory
        $stagingPathExists = Test-Path -LiteralPath $stagingDataDirectory
        $finalClusterExists = Test-Path -LiteralPath (Join-Path $ctx.Paths.DataDirectory 'PG_VERSION') -PathType Leaf
        $stagingClusterExists = Test-Path -LiteralPath (Join-Path $stagingDataDirectory 'PG_VERSION') -PathType Leaf

        if (($finalPathExists -and -not $finalClusterExists) -or
            ($finalClusterExists -and $stagingPathExists)) {
            throw "Database initialization recovery found more than one promotion target or an unowned final destination."
        }
        if (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity) {
            if (Test-Path -LiteralPath $bootstrapPasswordPath -PathType Leaf) {
                Remove-Item -LiteralPath $bootstrapPasswordPath -Force
            }
            Remove-Item -LiteralPath $ctx.Paths.InitializationReceiptPath -Force
            Set-OfficeDatabasePathPermissions -Context $ctx
            return
        }

        if ($receipt.phase -eq 'Promoting') {
            $candidateDirectory = if ($finalClusterExists) { $ctx.Paths.DataDirectory } elseif ($stagingClusterExists) { $stagingDataDirectory } else { $null }
            $credentialsComplete = @(
                $ctx.Paths.AdminPassfilePath,
                $ctx.Paths.MigratorPassfilePath,
                $ctx.Paths.ApplicationPassfilePath
            ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
            if ($null -ne $candidateDirectory -and $credentialsComplete.Count -eq 3) {
                $liveIdentifier = Get-OfficeClusterSystemIdentifier -Context $ctx -DataDirectory $candidateDirectory
                if ($liveIdentifier -ceq [string]$receipt.clusterSystemIdentifier) {
                    if (-not $finalClusterExists) {
                        if (Test-Path -LiteralPath $ctx.Paths.DataDirectory) {
                            throw "Database initialization recovery refused to promote into an existing destination."
                        }
                        Move-Item -LiteralPath $stagingDataDirectory -Destination $ctx.Paths.DataDirectory
                    }
                    $state = New-OfficeClusterStateReceipt `
                        -Context $ctx `
                        -ClusterSystemIdentifier $liveIdentifier `
                        -CreatedAtUtc ([string]$receipt.createdAtUtc)
                    Set-OfficeAtomicJsonFile -Path $ctx.Paths.StateFilePath -Value $state
                    Set-OfficeRestrictedAcl -Path $ctx.Paths.StateFilePath -Profile Secrets
                    if (Test-Path -LiteralPath $bootstrapPasswordPath -PathType Leaf) {
                        Remove-Item -LiteralPath $bootstrapPasswordPath -Force
                    }
                    Remove-Item -LiteralPath $ctx.Paths.InitializationReceiptPath -Force
                    Set-OfficeDatabasePathPermissions -Context $ctx
                    return
                }
            }
            if ($finalClusterExists) {
                throw "The promoted PostgreSQL cluster cannot be bound safely to its initialization receipt."
            }
        }

        if (Test-Path -LiteralPath $stagingDataDirectory) {
            Remove-OfficeOwnedDirectory -Path $stagingDataDirectory -ExpectedParent $ctx.Paths.DataRoot
        }
        if (Test-Path -LiteralPath $bootstrapPasswordPath -PathType Leaf) {
            Remove-Item -LiteralPath $bootstrapPasswordPath -Force
        }
        foreach ($passfile in @(
            $ctx.Paths.AdminPassfilePath,
            $ctx.Paths.MigratorPassfilePath,
            $ctx.Paths.ApplicationPassfilePath
        )) {
            if (Test-Path -LiteralPath $passfile -PathType Leaf) {
                Remove-Item -LiteralPath $passfile -Force
            }
        }
        Remove-Item -LiteralPath $ctx.Paths.InitializationReceiptPath -Force
        $ctx.Secrets = $null
    }

    $initializeCluster = {
        param($ctx)
        $clusterVersionPath = Join-Path $ctx.Paths.DataDirectory 'PG_VERSION'
        if (Test-Path -LiteralPath $clusterVersionPath -PathType Leaf) {
            if (-not (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity)) {
                throw "The existing PostgreSQL cluster is not owned by SafarSuite Control Desk."
            }
            return
        }

        New-Item -ItemType Directory -Force -Path $ctx.Paths.DatabaseLogDirectory | Out-Null
        New-Item -ItemType Directory -Force -Path $ctx.Paths.StateDirectory | Out-Null
        New-Item -ItemType Directory -Force -Path $ctx.Paths.SecretDirectory | Out-Null
        New-Item -ItemType Directory -Force -Path $ctx.Paths.DataRoot | Out-Null
        Set-OfficeDatabasePathPermissions -Context $ctx

        $adminPassword = New-OfficeDatabasePassword
        $migratorPassword = New-OfficeDatabasePassword
        $applicationPassword = New-OfficeDatabasePassword
        $nonce = [Guid]::NewGuid().ToString('N')
        $stagingDataDirectory = "$($ctx.Paths.DataDirectory).initializing-$nonce"
        $bootstrapPasswordPath = Join-Path $ctx.Paths.SecretDirectory "bootstrap-$nonce.txt"
        $createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        $initializationReceipt = [ordered]@{
            schemaVersion = $script:LifecycleSchemaVersion
            product = $script:LifecycleProduct
            phase = 'Initializing'
            nonce = $nonce
            createdAtUtc = $createdAtUtc
            stagingDataDirectory = $stagingDataDirectory
            dataDirectory = $ctx.Paths.DataDirectory
            bootstrapPasswordPath = $bootstrapPasswordPath
        }
        Set-OfficeAtomicJsonFile -Path $ctx.Paths.InitializationReceiptPath -Value $initializationReceipt
        Set-OfficeRestrictedAcl -Path $ctx.Paths.InitializationReceiptPath -Profile Secrets
        New-Item -ItemType Directory -Path $stagingDataDirectory | Out-Null
        Set-OfficeRestrictedAcl -Path $stagingDataDirectory -Profile Data

        try {
            Set-OfficeUtf8NoBomContent -Path $bootstrapPasswordPath -Value $adminPassword
            Set-OfficeRestrictedAcl -Path $bootstrapPasswordPath -Profile Secrets
            $initdb = Join-Path $ctx.Paths.RuntimeRoot 'bin\initdb.exe'
            Invoke-OfficeNativeCommand `
                -FilePath $initdb `
                -Arguments @(
                    '-D', $stagingDataDirectory,
                    '-U', [string]$ctx.Distribution.adminRole,
                    '--auth-host=scram-sha-256', '--auth-local=scram-sha-256',
                    '--encoding=UTF8', '--locale=C', "--pwfile=$bootstrapPasswordPath"
                ) `
                -TimeoutSeconds 180 | Out-Null
            Invoke-OfficeDatabaseTestFault -Context $ctx -Point 'AfterClusterInitialize'
        }
        finally {
            if (Test-Path -LiteralPath $bootstrapPasswordPath) {
                Remove-Item -LiteralPath $bootstrapPasswordPath -Force
            }
        }

        Write-OfficePgPassFile -Path $ctx.Paths.AdminPassfilePath -Port $ctx.Distribution.port -Database '*' -Role $ctx.Distribution.adminRole -Password $adminPassword
        Write-OfficePgPassFile -Path $ctx.Paths.MigratorPassfilePath -Port $ctx.Distribution.port -Database '*' -Role $ctx.Distribution.migratorRole -Password $migratorPassword
        Write-OfficePgPassFile -Path $ctx.Paths.ApplicationPassfilePath -Port $ctx.Distribution.port -Database '*' -Role $ctx.Distribution.applicationRole -Password $applicationPassword

        $ctx.Secrets = [pscustomobject]@{
            Admin = $adminPassword
            Migrator = $migratorPassword
            Application = $applicationPassword
        }

        $systemIdentifier = Get-OfficeClusterSystemIdentifier -Context $ctx -DataDirectory $stagingDataDirectory
        if ($null -eq $systemIdentifier) {
            throw "The initialized PostgreSQL cluster has no readable system identifier."
        }
        $initializationReceipt.phase = 'Promoting'
        $initializationReceipt.clusterSystemIdentifier = $systemIdentifier
        Set-OfficeAtomicJsonFile -Path $ctx.Paths.InitializationReceiptPath -Value $initializationReceipt
        Set-OfficeRestrictedAcl -Path $ctx.Paths.InitializationReceiptPath -Profile Secrets

        Move-Item -LiteralPath $stagingDataDirectory -Destination $ctx.Paths.DataDirectory
        Invoke-OfficeDatabaseTestFault -Context $ctx -Point 'AfterClusterPromote'
        $state = New-OfficeClusterStateReceipt `
            -Context $ctx `
            -ClusterSystemIdentifier $systemIdentifier `
            -CreatedAtUtc $createdAtUtc
        Set-OfficeAtomicJsonFile -Path $ctx.Paths.StateFilePath -Value $state
        Set-OfficeRestrictedAcl -Path $ctx.Paths.StateFilePath -Profile Secrets
        Remove-Item -LiteralPath $ctx.Paths.InitializationReceiptPath -Force
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $configure = {
        param($ctx)
        $postgresConfig = Get-OfficeExpectedPostgresConfiguration -Context $ctx
        $hbaConfig = Get-OfficeExpectedHbaConfiguration -Context $ctx
        $autoConfigPath = Join-Path $ctx.Paths.DataDirectory 'postgresql.auto.conf'
        $autoConfig = "# SafarSuite Control Desk does not permit ALTER SYSTEM overrides.`r`n"

        foreach ($configuration in @(
            @{ Path = $ctx.Paths.PostgresConfigurationPath; Content = $postgresConfig },
            @{ Path = $ctx.Paths.HbaConfigurationPath; Content = $hbaConfig },
            @{ Path = $autoConfigPath; Content = $autoConfig }
        )) {
            if (Test-Path -LiteralPath $configuration.Path) {
                $current = Get-Content -Raw -LiteralPath $configuration.Path
                if ($current -ne $configuration.Content) {
                    $backupPath = "$($configuration.Path).safarsuite-backup-$([DateTimeOffset]::UtcNow.ToString('yyyyMMddHHmmssfffffff'))-$([Guid]::NewGuid().ToString('N'))"
                    Copy-Item -LiteralPath $configuration.Path -Destination $backupPath
                }
            }
            Set-OfficeAtomicUtf8NoBomContent -Path $configuration.Path -Value $configuration.Content
        }
        New-Item -ItemType Directory -Force -Path $ctx.Paths.DatabaseLogDirectory | Out-Null
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $configurePermissions = {
        param($ctx)
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $configureService = {
        param($ctx)
        if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
            throw "Service configuration repair refused an unmanaged PostgreSQL service."
        }
        Set-OfficePostgresServiceConfiguration -Context $ctx -Mode Pending
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $registerService = {
        param($ctx)
        $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
        if ($null -ne $service) {
            if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
                throw "The PostgreSQL service name is occupied by a service not owned by SafarSuite Control Desk."
            }
            Set-OfficePostgresServiceConfiguration -Context $ctx -Mode Pending
            Set-OfficeDatabasePathPermissions -Context $ctx
            return
        }
        Invoke-OfficeNativeCommand `
            -FilePath (Join-Path $ctx.Paths.RuntimeRoot 'bin\pg_ctl.exe') `
            -Arguments @(
                'register', '-N', [string]$ctx.Distribution.serviceName,
                '-D', $ctx.Paths.DataDirectory, '-S', 'demand',
                '-U', [string]$ctx.Distribution.serviceAccount
            ) | Out-Null
        Set-OfficePostgresServiceConfiguration -Context $ctx -Mode Pending
        Set-OfficeDatabasePathPermissions -Context $ctx
    }

    $ensureStarted = {
        param($ctx)
        $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
        if ($null -eq $service) {
            throw "The managed PostgreSQL service is missing."
        }
        if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
            throw "The PostgreSQL service ownership check failed."
        }
        if ($service.Status -ne 'Running') {
            Start-Service -Name ([string]$ctx.Distribution.serviceName)
        }
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(60)
        do {
            $ready = Invoke-OfficeNativeCommand `
                -FilePath (Join-Path $ctx.Paths.RuntimeRoot 'bin\pg_isready.exe') `
                -Arguments @('-h', '127.0.0.1', '-p', [string]$ctx.Distribution.port, '-t', '3') `
                -TimeoutSeconds 5 `
                -AllowFailure
            if ($ready.ExitCode -eq 0) {
                Invoke-OfficeDatabaseTestFault -Context $ctx -Point 'AfterServiceStart'
                return
            }
            Start-Sleep -Milliseconds 500
        } while ([DateTimeOffset]::UtcNow -lt $deadline)
        throw "The managed PostgreSQL service started but did not become ready on loopback."
    }

    $provision = {
        param($ctx)
        if ($null -eq $ctx.Secrets) {
            throw "Database provisioning credentials are available only during first cluster initialization. Repair will never rotate or guess them."
        }
        $adminRole = [string]$ctx.Distribution.adminRole
        $migratorRole = [string]$ctx.Distribution.migratorRole
        $applicationRole = [string]$ctx.Distribution.applicationRole
        $databaseName = [string]$ctx.Distribution.databaseName
        $migratorPassword = ConvertTo-OfficeSqlLiteral -Value $ctx.Secrets.Migrator
        $applicationPassword = ConvertTo-OfficeSqlLiteral -Value $ctx.Secrets.Application
        $roleSql = @"
DO `$safarsuite`$
DECLARE
    membership record;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$migratorRole') THEN
        CREATE ROLE $migratorRole LOGIN PASSWORD $migratorPassword NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '$applicationRole') THEN
        CREATE ROLE $applicationRole LOGIN PASSWORD $applicationPassword NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;
    END IF;
    FOR membership IN
        SELECT parent.rolname AS parent_name, member.rolname AS member_name
        FROM pg_auth_members assigned
        JOIN pg_roles parent ON parent.oid = assigned.roleid
        JOIN pg_roles member ON member.oid = assigned.member
        WHERE parent.rolname IN ('$migratorRole', '$applicationRole')
           OR member.rolname IN ('$migratorRole', '$applicationRole')
    LOOP
        EXECUTE format('REVOKE %I FROM %I', membership.parent_name, membership.member_name);
    END LOOP;
END
`$safarsuite`$;
ALTER ROLE $migratorRole WITH LOGIN PASSWORD $migratorPassword NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT NOREPLICATION NOBYPASSRLS CONNECTION LIMIT -1 VALID UNTIL 'infinity';
ALTER ROLE $applicationRole WITH LOGIN PASSWORD $applicationPassword NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT NOREPLICATION NOBYPASSRLS CONNECTION LIMIT -1 VALID UNTIL 'infinity';
SELECT format('CREATE DATABASE %I OWNER %I', '$databaseName', '$migratorRole')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = '$databaseName') \gexec
ALTER DATABASE $databaseName OWNER TO $migratorRole;
REVOKE ALL ON DATABASE $databaseName FROM PUBLIC;
REVOKE ALL ON DATABASE $databaseName FROM $applicationRole;
GRANT CONNECT ON DATABASE $databaseName TO $adminRole, $migratorRole, $applicationRole;
"@
        Invoke-OfficePsql -Context $ctx -Role $adminRole -Database 'postgres' -Passfile $ctx.Paths.AdminPassfilePath -Sql $roleSql | Out-Null
        $databaseSql = @"
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO $applicationRole;
ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $applicationRole;
ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $applicationRole;
"@
        Invoke-OfficePsql -Context $ctx -Role $migratorRole -Database $databaseName -Passfile $ctx.Paths.MigratorPassfilePath -Sql $databaseSql | Out-Null
        Invoke-OfficePsql `
            -Context $ctx `
            -Role $adminRole `
            -Database $databaseName `
            -Passfile $ctx.Paths.AdminPassfilePath `
            -Sql "REASSIGN OWNED BY $applicationRole TO $migratorRole;" | Out-Null
    }

    $migrate = {
        param($ctx)
        $expected = @($ctx.PackageManifest.migrations.orderedIds | ForEach-Object { [string]$_ })
        $applied = @(Get-OfficeAppliedMigrations -Context $ctx)
        $comparison = Compare-OfficeMigrationLedger -Expected $expected -Applied $applied
        if ($comparison -eq 'Diverged') {
            throw "The database migration history diverges from the signed package ledger. No migration was attempted."
        }
        if ($comparison -ne 'Exact') {
            $mutex = [Threading.Mutex]::new($false, 'Global\SafarSuiteControlDeskDatabaseMigration')
            $lockAcquired = $false
            try {
                $lockAcquired = $mutex.WaitOne([TimeSpan]::FromMinutes(5))
                if (-not $lockAcquired) {
                    throw "Another SafarSuite Control Desk migration is already in progress."
                }
                $connectionString = "Host=127.0.0.1;Port=$($ctx.Distribution.port);Database=$($ctx.Distribution.databaseName);Username=$($ctx.Distribution.migratorRole);Passfile=$($ctx.Paths.MigratorPassfilePath);SSL Mode=Disable;Application Name=SafarSuite Control Desk Migrator"
                Invoke-OfficeNativeCommand `
                    -FilePath $ctx.Paths.MigrationBundlePath `
                    -Arguments @([string]$ctx.PackageManifest.migrations.target) `
                    -Environment @{ SAFARSUITE_CONTROL_DESK_CONNECTION_STRING = $connectionString } `
                    -TimeoutSeconds 900 | Out-Null
            }
            finally {
                if ($lockAcquired) {
                    $mutex.ReleaseMutex()
                }
                $mutex.Dispose()
            }
        }

        $migratorRole = [string]$ctx.Distribution.migratorRole
        $applicationRole = [string]$ctx.Distribution.applicationRole
        $privilegeSql = @"
CREATE EXTENSION IF NOT EXISTS pg_trgm;
DO `$safarsuite`$
DECLARE
    managed_schema text;
BEGIN
    FOR managed_schema IN
        SELECT nspname
        FROM pg_namespace
        WHERE nspname <> 'information_schema'
          AND nspname NOT LIKE 'pg\_%' ESCAPE '\'
    LOOP
        EXECUTE format('REVOKE ALL ON SCHEMA %I FROM PUBLIC', managed_schema);
        EXECUTE format('REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I FROM PUBLIC', managed_schema);
        EXECUTE format('REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I FROM PUBLIC', managed_schema);
        EXECUTE format('REVOKE ALL ON SCHEMA %I FROM $applicationRole', managed_schema);
        EXECUTE format('GRANT USAGE ON SCHEMA %I TO $applicationRole', managed_schema);
        EXECUTE format('REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I FROM $applicationRole', managed_schema);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO $applicationRole', managed_schema);
        EXECUTE format('REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I FROM $applicationRole', managed_schema);
        EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA %I TO $applicationRole', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I REVOKE ALL ON TABLES FROM $applicationRole', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $applicationRole', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I REVOKE ALL ON SEQUENCES FROM $applicationRole', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO $applicationRole', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I REVOKE ALL ON TABLES FROM PUBLIC', managed_schema);
        EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE $migratorRole IN SCHEMA %I REVOKE ALL ON SEQUENCES FROM PUBLIC', managed_schema);
    END LOOP;
END
`$safarsuite`$;
REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER
    ON TABLE control.__ef_migrations_history
    FROM $applicationRole;
GRANT SELECT ON TABLE control.__ef_migrations_history TO $applicationRole;
"@
        Invoke-OfficePsql `
            -Context $ctx `
            -Role $migratorRole `
            -Database ([string]$ctx.Distribution.databaseName) `
            -Passfile $ctx.Paths.MigratorPassfilePath `
            -Sql $privilegeSql | Out-Null
    }

    $verify = {
        param($ctx)
        $expected = @($ctx.PackageManifest.migrations.orderedIds | ForEach-Object { [string]$_ })
        $applied = @(Get-OfficeAppliedMigrations -Context $ctx)
        if ((Compare-OfficeMigrationLedger -Expected $expected -Applied $applied) -ne 'Exact') {
            throw "The database did not reach the package's exact migration target."
        }
        $extension = Invoke-OfficePsql `
            -Context $ctx `
            -Role ([string]$ctx.Distribution.migratorRole) `
            -Database ([string]$ctx.Distribution.databaseName) `
            -Passfile $ctx.Paths.MigratorPassfilePath `
            -Sql "SELECT extname FROM pg_extension WHERE extname = 'pg_trgm';"
        if ($extension.StandardOutput.Trim() -ne 'pg_trgm') {
            throw "The required pg_trgm extension is not installed."
        }
        if (-not (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity)) {
            throw "The PostgreSQL cluster identity no longer matches the protected ownership receipt."
        }
        if (-not (Test-OfficeEffectiveDatabaseConfiguration -Context $ctx)) {
            throw "The effective PostgreSQL configuration does not match the managed loopback/SCRAM policy."
        }
        if (-not (Test-OfficeDatabaseSecurityState -Context $ctx)) {
            throw "The PostgreSQL roles, ownership, credentials, or grants do not match the managed security policy."
        }
        if (-not (Test-OfficeDatabasePathPermissions -Context $ctx)) {
            throw "The managed PostgreSQL filesystem permissions failed exact verification."
        }
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort ([int]$ctx.Distribution.port) -ErrorAction Stop)
        $nonLoopback = @($listeners | Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') })
        if ($listeners.Count -lt 1 -or $nonLoopback.Count -gt 0) {
            throw "PostgreSQL is not restricted to loopback listeners."
        }
        return [pscustomobject]@{
            MigrationCount = $applied.Count
            MigrationTarget = $applied[-1]
            RequiredExtension = 'pg_trgm'
            ListenerAddresses = @($listeners.LocalAddress | Sort-Object -Unique)
        }
    }

    $activate = {
        param($ctx, $verification)
        Set-OfficePostgresServiceConfiguration -Context $ctx -Mode Activated
        Invoke-OfficeDatabaseTestFault -Context $ctx -Point 'AfterServiceActivation'
        $activation = [ordered]@{
            schemaVersion = $script:LifecycleSchemaVersion
            product = $script:LifecycleProduct
            activatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
            packageSourceRevision = [string]$ctx.PackageManifest.sourceRevision
            postgresVersion = [string]$ctx.Distribution.version
            serviceName = [string]$ctx.Distribution.serviceName
            migrationCount = [int]$verification.MigrationCount
            migrationTarget = [string]$verification.MigrationTarget
            requiredExtension = [string]$verification.RequiredExtension
            listenerAddresses = @($verification.ListenerAddresses)
        }
        Set-OfficeAtomicJsonFile -Path $ctx.Paths.ActivationFilePath -Value $activation
        Set-OfficeRestrictedAcl -Path $ctx.Paths.ActivationFilePath -Profile Secrets
    }

    $restart = {
        param($ctx)
        $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
        if ($null -eq $service) {
            throw "The managed PostgreSQL service is missing."
        }
        if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
            throw "The PostgreSQL service ownership check failed."
        }
        if ($service.Status -eq 'Running') {
            Stop-Service -Name ([string]$ctx.Distribution.serviceName) -Force
            (Get-Service -Name ([string]$ctx.Distribution.serviceName)).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
        }
        Start-Service -Name ([string]$ctx.Distribution.serviceName)
    }

    $unregister = {
        param($ctx)
        $service = Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)
        if ($null -eq $service) {
            return
        }
        if (-not (Test-OfficePostgresServiceOwnership -Context $ctx)) {
            throw "The PostgreSQL service ownership check failed; no service was stopped or removed."
        }
        if ($service.Status -eq 'Running') {
            Stop-Service -Name ([string]$ctx.Distribution.serviceName) -Force
            (Get-Service -Name ([string]$ctx.Distribution.serviceName)).WaitForStatus('Stopped', [TimeSpan]::FromSeconds(30))
        }
        Invoke-OfficeNativeCommand `
            -FilePath (Join-Path $ctx.Paths.RuntimeRoot 'bin\pg_ctl.exe') `
            -Arguments @('unregister', '-N', [string]$ctx.Distribution.serviceName) | Out-Null
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
        do {
            if ($null -eq (Get-OfficeService -ServiceName ([string]$ctx.Distribution.serviceName)) -and
                $null -eq (Get-OfficeServiceConfiguration -ServiceName ([string]$ctx.Distribution.serviceName))) {
                return
            }
            Start-Sleep -Milliseconds 250
        } while ([DateTimeOffset]::UtcNow -lt $deadline)
        throw "Windows did not remove the managed PostgreSQL service within the safety timeout."
    }

    $removeRuntime = {
        param($ctx)
        if (-not (Test-Path -LiteralPath $ctx.Paths.RuntimeRoot)) {
            $dataExists = Test-Path -LiteralPath $ctx.Paths.DataDirectory
            $stateExists = Test-Path -LiteralPath $ctx.Paths.StateFilePath -PathType Leaf
            if ((-not $dataExists -and -not $stateExists) -or
                (Test-OfficeManagedClusterMarker -Context $ctx)) {
                return
            }
            throw "Runtime removal found preserved data without a valid SafarSuite ownership marker."
        }
        if (-not (Test-OfficeManagedClusterMarker -Context $ctx -RequireLiveIdentity)) {
            throw "Runtime removal was refused because the SafarSuite database ownership marker is absent or invalid."
        }
        if (-not (Test-OfficeInstalledRuntimeIntegrity -Context $ctx)) {
            throw "Runtime removal was refused because the installed runtime receipt or integrity check failed."
        }
        $runtimePath = [IO.Path]::GetFullPath($ctx.Paths.RuntimeRoot)
        $expectedRoot = [IO.Path]::GetFullPath((Join-Path $ctx.Paths.ProgramFilesRoot "Database\PostgreSQL"))
        if (-not $runtimePath.StartsWith($expectedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Runtime removal was refused because the resolved path is outside the SafarSuite product root."
        }
        if (Test-Path -LiteralPath $runtimePath) {
            Remove-OfficeOwnedDirectory -Path $runtimePath -ExpectedParent $expectedRoot
        }
    }

    return @{
        Inspect = $inspect
        StageRuntime = $stageRuntime
        RecoverInitialization = $recoverInitialization
        InitializeCluster = $initializeCluster
        Configure = $configure
        ConfigurePermissions = $configurePermissions
        ConfigureService = $configureService
        RegisterService = $registerService
        EnsureStarted = $ensureStarted
        Provision = $provision
        Migrate = $migrate
        Verify = $verify
        Activate = $activate
        Restart = $restart
        Unregister = $unregister
        RemoveRuntime = $removeRuntime
    }
}

function Invoke-OfficeDatabaseAdapterOperation {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Adapter,
        [Parameter(Mandatory = $true)][string]$Operation,
        [Parameter(Mandatory = $true)]$Context,
        [object[]]$AdditionalArguments = @()
    )
    if (-not $Adapter.ContainsKey($Operation) -or $Adapter[$Operation] -isnot [scriptblock]) {
        throw "The database lifecycle adapter does not implement '$Operation'."
    }
    return & $Adapter[$Operation] $Context @AdditionalArguments
}

function Invoke-OfficeDatabaseLifecycleLock {
    param([Parameter(Mandatory = $true)][scriptblock]$Action)

    $mutex = [Threading.Mutex]::new($false, 'Global\SafarSuiteControlDeskDatabaseLifecycle')
    $lockAcquired = $false
    try {
        try {
            $lockAcquired = $mutex.WaitOne([TimeSpan]::FromMinutes(5))
        }
        catch [Threading.AbandonedMutexException] {
            $lockAcquired = $true
        }
        if (-not $lockAcquired) {
            throw "Another SafarSuite Control Desk database lifecycle operation is in progress."
        }
        return & $Action
    }
    finally {
        if ($lockAcquired) {
            $mutex.ReleaseMutex()
        }
        $mutex.Dispose()
    }
}

function Invoke-OfficeDatabaseLifecycleCore {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][ValidateSet('Install', 'Repair', 'Uninstall')][string]$Action,
        [Parameter(Mandatory = $true)][hashtable]$Adapter,
        [Parameter(Mandatory = $true)]$Context
    )

    $initialState = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Inspect -Context $Context
    $reportedInitialState = $initialState
    if ($initialState -in @('ForeignCluster', 'ForeignService', 'MigrationDiverged', 'MissingCredentials', 'PortCollision', 'UnsafeInitializationState', 'UnsafePath', 'UnsupportedCluster')) {
        throw "The database lifecycle stopped safely because state '$initialState' requires manual recovery."
    }
    if ($initialState -eq 'InterruptedInitialization') {
        if ($Action -eq 'Uninstall') {
            throw "Database uninstall refused an interrupted initialization; resume install first."
        }
        Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation RecoverInitialization -Context $Context
        $initialState = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Inspect -Context $Context
        if ($initialState -in @('ForeignCluster', 'ForeignService', 'MigrationDiverged', 'MissingCredentials', 'PortCollision', 'UnsafeInitializationState', 'UnsafePath', 'UnsupportedCluster')) {
            throw "Initialization recovery stopped safely because state '$initialState' requires manual recovery."
        }
    }

    if ($Action -eq 'Uninstall') {
        Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Unregister -Context $Context
        Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation RemoveRuntime -Context $Context
        return [pscustomobject]@{ Action = $Action; InitialState = $reportedInitialState; FinalState = 'PreservedData' }
    }

    $requiresProvision = $reportedInitialState -eq 'InterruptedInitialization'
    if ($Action -eq 'Install') {
        if ($initialState -eq 'Absent') {
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation StageRuntime -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation InitializeCluster -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Configure -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation RegisterService -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            $requiresProvision = $true
        }
        elseif ($initialState -eq 'MissingService') {
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation StageRuntime -Context $Context
            $postStageState = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Inspect -Context $Context
            if ($postStageState -ne 'MissingService') {
                throw "Database service recovery stopped safely because state '$postStageState' did not retain managed cluster ownership."
            }
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Configure -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation RegisterService -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            $requiresProvision = $true
        }
        elseif ($initialState -in @('CorruptConfiguration', 'CorruptPermissions', 'CorruptServiceConfiguration', 'UnavailableDatabase')) {
            throw "Install will not repair state '$initialState'; run the explicit repair command."
        }
        elseif ($initialState -eq 'MissingPrerequisite') {
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation StageRuntime -Context $Context
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
        }
        elseif ($initialState -eq 'StoppedService') {
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
        }
        elseif ($initialState -eq 'InitializationIncomplete') {
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            $requiresProvision = $true
        }
        elseif ($initialState -eq 'SecurityDrift') {
            $requiresProvision = $true
        }
        if ($requiresProvision) {
            if ($null -eq $Context.Secrets) {
                throw "Database provisioning cannot resume because its retained credentials are unavailable."
            }
            Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Provision -Context $Context
        }
    }
    else {
        switch ($initialState) {
            'Absent' { throw "Repair cannot initialize an absent database; run install." }
            'MissingPrerequisite' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation StageRuntime -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'MissingService' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation StageRuntime -Context $Context
                $postStageState = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Inspect -Context $Context
                if ($postStageState -ne 'MissingService') {
                    throw "Database service recovery stopped safely because state '$postStageState' did not retain managed cluster ownership."
                }
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Configure -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation RegisterService -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
                if ($null -eq $Context.Secrets) {
                    throw "Database service recovery cannot restore provisioning because its retained credentials are unavailable."
                }
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Provision -Context $Context
            }
            'StoppedService' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'CorruptConfiguration' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Configure -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Restart -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'CorruptPermissions' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation ConfigurePermissions -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'CorruptServiceConfiguration' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation ConfigureService -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Restart -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'UnavailableDatabase' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Restart -Context $Context
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
            }
            'InitializationIncomplete' {
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation EnsureStarted -Context $Context
                if ($null -eq $Context.Secrets) {
                    throw "Database initialization cannot resume because its retained credentials are unavailable."
                }
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Provision -Context $Context
            }
            'SecurityDrift' {
                if ($null -eq $Context.Secrets) {
                    throw "Database security repair cannot proceed because its retained credentials are unavailable."
                }
                Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Provision -Context $Context
            }
        }
    }

    Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Migrate -Context $Context
    $verification = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Verify -Context $Context
    Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Activate -Context $Context -AdditionalArguments @($verification)
    $finalState = Invoke-OfficeDatabaseAdapterOperation -Adapter $Adapter -Operation Inspect -Context $Context
    if ($finalState -ne 'Ready') {
        throw "The database lifecycle did not converge to Ready; final state was '$finalState'."
    }
    return [pscustomobject]@{ Action = $Action; InitialState = $reportedInitialState; FinalState = $finalState; Verification = $verification }
}

function New-OfficeDatabaseContext {
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [string]$ProgramFilesRoot,
        [string]$ProgramDataRoot
    )

    $manifest = Test-OfficeDatabasePackage -PackageDirectory $PackageDirectory
    $paths = Get-OfficeDatabasePaths -PackageDirectory $PackageDirectory -ProgramFilesRoot $ProgramFilesRoot -ProgramDataRoot $ProgramDataRoot
    $adminPassword = Read-OfficePgPassPassword -Path $paths.AdminPassfilePath
    $migratorPassword = Read-OfficePgPassPassword -Path $paths.MigratorPassfilePath
    $applicationPassword = Read-OfficePgPassPassword -Path $paths.ApplicationPassfilePath
    $secrets = if ($null -ne $adminPassword -and $null -ne $migratorPassword -and $null -ne $applicationPassword) {
        [pscustomobject]@{
            Admin = $adminPassword
            Migrator = $migratorPassword
            Application = $applicationPassword
        }
    }
    else {
        $null
    }
    return [pscustomobject]@{
        PackageManifest = $manifest
        Distribution = $manifest.postgresql
        Paths = $paths
        Secrets = $secrets
    }
}

function Install-OfficeDatabaseLifecycle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [string]$ProgramFilesRoot,
        [string]$ProgramDataRoot
    )

    Assert-OfficeAdministrator
    $contextParameters = @{} + $PSBoundParameters
    return Invoke-OfficeDatabaseLifecycleLock -Action {
        $context = New-OfficeDatabaseContext @contextParameters
        Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseInstallStarted' -Outcome 'Started'
        try {
            $result = Invoke-OfficeDatabaseLifecycleCore -Action Install -Adapter (New-OfficeNativeDatabaseAdapter -Context $context) -Context $context
            Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseInstallCompleted' -Outcome 'Succeeded' -Details @{ initialState = $result.InitialState; finalState = $result.FinalState; migrationTarget = $result.Verification.MigrationTarget }
            return $result
        }
        catch {
            Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseInstallFailed' -Outcome 'Failed' -Details @{ exceptionType = $_.Exception.GetType().FullName }
            throw
        }
    }
}

function Repair-OfficeDatabaseLifecycle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [string]$ProgramFilesRoot,
        [string]$ProgramDataRoot
    )

    Assert-OfficeAdministrator
    $contextParameters = @{} + $PSBoundParameters
    return Invoke-OfficeDatabaseLifecycleLock -Action {
        $context = New-OfficeDatabaseContext @contextParameters
        Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseRepairStarted' -Outcome 'Started'
        try {
            $result = Invoke-OfficeDatabaseLifecycleCore -Action Repair -Adapter (New-OfficeNativeDatabaseAdapter -Context $context) -Context $context
            Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseRepairCompleted' -Outcome 'Succeeded' -Details @{ classification = $result.InitialState; finalState = $result.FinalState; migrationTarget = $result.Verification.MigrationTarget }
            return $result
        }
        catch {
            Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseRepairFailed' -Outcome 'Failed' -Details @{ exceptionType = $_.Exception.GetType().FullName }
            throw
        }
    }
}

function Get-OfficeDatabaseLifecycleState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [string]$ProgramFilesRoot,
        [string]$ProgramDataRoot
    )

    $contextParameters = @{} + $PSBoundParameters
    return Invoke-OfficeDatabaseLifecycleLock -Action {
        $context = New-OfficeDatabaseContext @contextParameters
        Invoke-OfficeDatabaseAdapterOperation -Adapter (New-OfficeNativeDatabaseAdapter -Context $context) -Operation Inspect -Context $context
    }
}

function Uninstall-OfficeDatabaseLifecycle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$PackageDirectory,
        [string]$ProgramFilesRoot,
        [string]$ProgramDataRoot
    )

    Assert-OfficeAdministrator
    $contextParameters = @{} + $PSBoundParameters
    return Invoke-OfficeDatabaseLifecycleLock -Action {
        $context = New-OfficeDatabaseContext @contextParameters
        if ($null -ne (Get-Service -Name 'SafarSuiteControlDeskApi' -ErrorAction SilentlyContinue)) {
            throw "Database uninstall was refused while the SafarSuite Control Desk API service exists. Remove the API service first."
        }
        Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseUninstallStarted' -Outcome 'Started'
        $result = Invoke-OfficeDatabaseLifecycleCore -Action Uninstall -Adapter (New-OfficeNativeDatabaseAdapter -Context $context) -Context $context
        Write-OfficeDatabaseAudit -Paths $context.Paths -EventCode 'OfficeDatabaseUninstallCompleted' -Outcome 'Succeeded' -Details @{ dataPreserved = $true; secretsPreserved = $true; statePreserved = $true }
        $result
    }
}

function Set-OfficeApiDatabaseDependency {
    [CmdletBinding()]
    param(
        [string]$ApiServiceName = 'SafarSuiteControlDeskApi',
        [string]$DatabaseServiceName = 'SafarSuiteControlDeskPostgreSQL',
        [string]$ExpectedApiExecutablePath,
        [string]$ExpectedDatabaseExecutablePath,
        [string]$ExpectedDatabaseDataDirectory
    )

    Assert-OfficeAdministrator
    return Invoke-OfficeDatabaseLifecycleLock -Action {
        $databaseConfiguration = Get-OfficeServiceConfiguration -ServiceName $DatabaseServiceName
        if ($null -eq $databaseConfiguration) {
            throw "The managed PostgreSQL service must exist before configuring the API dependency."
        }
        $apiConfiguration = Get-OfficeServiceConfiguration -ServiceName $ApiServiceName
        if ($null -eq $apiConfiguration) {
            return [pscustomobject]@{ Status = 'Deferred'; Reason = 'ApiServiceNotInstalled'; ApiServiceName = $ApiServiceName; DatabaseServiceName = $DatabaseServiceName }
        }
        if ([string]::IsNullOrWhiteSpace($ExpectedApiExecutablePath)) {
            throw "The exact API executable path is required before mutating an existing API service."
        }
        if ([string]::IsNullOrWhiteSpace($ExpectedDatabaseDataDirectory)) {
            $commonApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData)
            if ([string]::IsNullOrWhiteSpace($commonApplicationData)) {
                throw "The canonical PostgreSQL data directory could not be resolved."
            }
            $ExpectedDatabaseDataDirectory = Join-Path $commonApplicationData 'SafarSuite\ControlDesk\Database\PostgreSQL17\Data'
        }
        if ([string]::IsNullOrWhiteSpace($ExpectedDatabaseExecutablePath)) {
            $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
            if ([string]::IsNullOrWhiteSpace($programFiles)) {
                throw "The canonical PostgreSQL runtime directory could not be resolved."
            }
            try {
                $databaseTokens = @([SafarSuiteControlDeskCommandLine]::Split([string]$databaseConfiguration.PathName))
                if ($databaseTokens.Count -lt 1) { throw 'Missing executable token.' }
                $candidateExecutable = [IO.Path]::GetFullPath($databaseTokens[0])
                $runtimeParent = [IO.Path]::GetFullPath((Join-Path $programFiles 'SafarSuite\ControlDesk\Database\PostgreSQL')).TrimEnd('\')
                $relativeExecutable = $candidateExecutable.Substring($runtimeParent.Length + 1)
            }
            catch {
                throw "The PostgreSQL service command line is invalid."
            }
            if (-not $candidateExecutable.StartsWith($runtimeParent + '\', [StringComparison]::OrdinalIgnoreCase) -or
                $relativeExecutable -notmatch '^17\.[0-9]+\\bin\\pg_ctl\.exe$' -or
                -not (Test-OfficePathChainIsSafe -Path $candidateExecutable)) {
                throw "The PostgreSQL service executable is outside the canonical SafarSuite runtime path."
            }
            $ExpectedDatabaseExecutablePath = $candidateExecutable
        }
        if (-not (Test-OfficeExactPostgresServiceCommandLine `
            -PathName ([string]$databaseConfiguration.PathName) `
            -ExpectedExecutablePath $ExpectedDatabaseExecutablePath `
            -ExpectedServiceName $DatabaseServiceName `
            -ExpectedDataDirectory $ExpectedDatabaseDataDirectory)) {
            throw "The PostgreSQL service name is occupied by an unmanaged service."
        }
        try {
            $apiTokens = @([SafarSuiteControlDeskCommandLine]::Split([string]$apiConfiguration.PathName))
            $apiExecutablePath = [IO.Path]::GetFullPath($apiTokens[0])
            $expectedApiPath = [IO.Path]::GetFullPath($ExpectedApiExecutablePath)
        }
        catch {
            throw "The API service command line is invalid."
        }
        if ($apiTokens.Count -lt 1 -or $apiExecutablePath -ne $expectedApiPath) {
            throw "The API service name is occupied by a service outside the expected SafarSuite package."
        }
        Invoke-OfficeNativeCommand `
            -FilePath "$env:SystemRoot\System32\sc.exe" `
            -Arguments @('config', $ApiServiceName, 'depend=', $DatabaseServiceName) | Out-Null
        Invoke-OfficeNativeCommand `
            -FilePath "$env:SystemRoot\System32\sc.exe" `
            -Arguments @('failure', $ApiServiceName, 'reset=', '86400', 'actions=', 'restart/5000/restart/15000/restart/60000') | Out-Null
        $dependencyPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ApiServiceName"
        $dependencies = @((Get-ItemProperty -LiteralPath $dependencyPath -Name DependOnService -ErrorAction Stop).DependOnService)
        if ($dependencies.Count -ne 1 -or $dependencies[0] -ne $DatabaseServiceName) {
            throw "Windows did not retain the exact API-to-PostgreSQL service dependency."
        }
        return [pscustomobject]@{ Status = 'Configured'; ApiServiceName = $ApiServiceName; DatabaseServiceName = $DatabaseServiceName }
    }
}

Export-ModuleMember -Function @(
    'Compare-OfficeMigrationLedger',
    'Get-OfficeDatabaseLifecycleState',
    'Get-OfficeDatabasePaths',
    'Install-OfficeDatabaseLifecycle',
    'Invoke-OfficeDatabaseLifecycleCore',
    'New-OfficeDatabaseContext',
    'Repair-OfficeDatabaseLifecycle',
    'Set-OfficeApiDatabaseDependency',
    'Test-OfficeDatabasePackage',
    'Uninstall-OfficeDatabaseLifecycle'
)
