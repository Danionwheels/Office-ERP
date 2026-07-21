using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

[SupportedOSPlatform("windows")]
internal sealed class WindowsMachineSecretAccessPolicy : IMachineSecretAccessPolicy
{
    internal const string SystemSidValue = "S-1-5-18";
    internal const string AdministratorsSidValue = "S-1-5-32-544";
    internal const string ApiServiceSidValue =
        "S-1-5-80-2177609957-237951300-3651597395-3114367455-1078186923";

    private static readonly SecurityIdentifier SystemSid = new(SystemSidValue);
    private static readonly SecurityIdentifier AdministratorsSid = new(AdministratorsSidValue);
    private static readonly SecurityIdentifier ApiServiceSid = new(ApiServiceSidValue);

    public void PrepareForWrite(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
        ExecuteAccessControlOperation(() =>
        {
            var directory = GetEnvelopeDirectory(envelopePath);
            EnsureExistingPathChainHasNoReparsePoint(directory);
            Directory.CreateDirectory(directory);
            EnsureExistingPathChainHasNoReparsePoint(directory);
            ApplyDirectorySecurity(directory, profile);

            if (File.Exists(envelopePath))
            {
                EnsureNotReparsePoint(envelopePath);
                ApplyFileSecurity(envelopePath, profile);
            }

            ValidateDirectory(directory, profile);

            if (File.Exists(envelopePath))
            {
                ValidateEnvelope(envelopePath, profile);
            }
        });
    }

    public void ProtectTransientFile(string path)
    {
        ExecuteAccessControlOperation(() =>
        {
            EnsureExistingPathChainHasNoReparsePoint(path);
            ApplyFileSecurity(path, ControlDeskMachineSecretAccessProfile.PreService);
            ValidateEnvelope(path, ControlDeskMachineSecretAccessProfile.PreService);
        });
    }

    public void ProtectEnvelope(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
        ExecuteAccessControlOperation(() =>
        {
            EnsureExistingPathChainHasNoReparsePoint(envelopePath);
            ApplyFileSecurity(envelopePath, profile);
            ValidateEnvelope(envelopePath, profile);
        });
    }

    public void ValidateForRead(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
        try
        {
            var directory = GetEnvelopeDirectory(envelopePath);
            EnsureExistingPathChainHasNoReparsePoint(envelopePath);
            ValidateDirectory(directory, profile);
            ValidateEnvelope(envelopePath, profile);
        }
        catch (MachineSecretEnvelopeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or SystemException)
        {
            throw new MachineSecretEnvelopeException(
                MachineSecretEnvelopeFailure.AccessControlInvalid,
                "The Control Desk machine-secret access boundary is unavailable or invalid.",
                exception);
        }
    }

    public void Repair(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
        ExecuteAccessControlOperation(() =>
        {
            var directory = GetEnvelopeDirectory(envelopePath);
            EnsureExistingPathChainHasNoReparsePoint(envelopePath);

            if (!File.Exists(envelopePath))
            {
                throw InvalidAccessControl();
            }

            ApplyDirectorySecurity(directory, profile);
            ApplyFileSecurity(envelopePath, profile);
            ValidateDirectory(directory, profile);
            ValidateEnvelope(envelopePath, profile);
        });
    }

    private static void ExecuteAccessControlOperation(Action operation)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The Control Desk machine-secret ACL requires Windows.");
        }

        try
        {
            operation();
        }
        catch (MachineSecretEnvelopeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or SystemException)
        {
            throw new MachineSecretEnvelopeException(
                MachineSecretEnvelopeFailure.AccessControlFailed,
                "The Control Desk machine-secret access boundary could not be converged.",
                exception);
        }
    }

    private static void ApplyDirectorySecurity(
        string directory,
        ControlDeskMachineSecretAccessProfile profile)
    {
        var security = new DirectorySecurity();
        security.SetOwner(SystemSid);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        AddAllowRule(
            security,
            SystemSid,
            FileSystemRights.FullControl,
            inheritance);
        AddAllowRule(
            security,
            AdministratorsSid,
            FileSystemRights.FullControl,
            inheritance);

        if (profile == ControlDeskMachineSecretAccessProfile.InstalledApiService)
        {
            AddAllowRule(
                security,
                ApiServiceSid,
                FileSystemRights.ReadAndExecute,
                InheritanceFlags.None);
        }

        using var privilege = WindowsRestorePrivilegeScope.Enable();
        new DirectoryInfo(directory).SetAccessControl(security);
    }

    private static void ApplyFileSecurity(
        string path,
        ControlDeskMachineSecretAccessProfile profile)
    {
        var security = new FileSecurity();
        security.SetOwner(SystemSid);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        AddAllowRule(
            security,
            SystemSid,
            FileSystemRights.FullControl,
            InheritanceFlags.None);
        AddAllowRule(
            security,
            AdministratorsSid,
            FileSystemRights.FullControl,
            InheritanceFlags.None);

        if (profile == ControlDeskMachineSecretAccessProfile.InstalledApiService)
        {
            AddAllowRule(
                security,
                ApiServiceSid,
                FileSystemRights.Read,
                InheritanceFlags.None);
        }

        using var privilege = WindowsRestorePrivilegeScope.Enable();
        new FileInfo(path).SetAccessControl(security);
    }

    private static void AddAllowRule(
        FileSystemSecurity security,
        SecurityIdentifier identity,
        FileSystemRights rights,
        InheritanceFlags inheritanceFlags)
    {
        security.AddAccessRule(new FileSystemAccessRule(
            identity,
            rights,
            inheritanceFlags,
            PropagationFlags.None,
            AccessControlType.Allow));
    }

    private static void ValidateDirectory(
        string directory,
        ControlDeskMachineSecretAccessProfile profile)
    {
        var security = new DirectoryInfo(directory)
            .GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
        var inheritance = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
        var expected = new List<ExpectedRule>
        {
            new(SystemSidValue, FileSystemRights.FullControl, inheritance),
            new(AdministratorsSidValue, FileSystemRights.FullControl, inheritance)
        };

        if (profile == ControlDeskMachineSecretAccessProfile.InstalledApiService)
        {
            expected.Add(new(
                ApiServiceSidValue,
                FileSystemRights.ReadAndExecute,
                InheritanceFlags.None));
        }

        ValidateSecurityDescriptor(security, expected);
    }

    private static void ValidateEnvelope(
        string path,
        ControlDeskMachineSecretAccessProfile profile)
    {
        if (!File.Exists(path))
        {
            throw InvalidAccessControl();
        }

        var security = new FileInfo(path)
            .GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
        var expected = new List<ExpectedRule>
        {
            new(SystemSidValue, FileSystemRights.FullControl, InheritanceFlags.None),
            new(AdministratorsSidValue, FileSystemRights.FullControl, InheritanceFlags.None)
        };

        if (profile == ControlDeskMachineSecretAccessProfile.InstalledApiService)
        {
            expected.Add(new(
                ApiServiceSidValue,
                FileSystemRights.Read,
                InheritanceFlags.None));
        }

        ValidateSecurityDescriptor(security, expected);
    }

    private static void ValidateSecurityDescriptor(
        FileSystemSecurity security,
        IReadOnlyCollection<ExpectedRule> expected)
    {
        if (!security.AreAccessRulesProtected
            || security.GetOwner(typeof(SecurityIdentifier)) is not SecurityIdentifier owner
            || owner.Value != SystemSidValue)
        {
            throw InvalidAccessControl();
        }

        var actual = security
            .GetAccessRules(
                includeExplicit: true,
                includeInherited: true,
                typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .ToArray();

        if (actual.Length != expected.Count
            || actual.Any(rule => rule.IsInherited
                                  || rule.AccessControlType != AccessControlType.Allow
                                  || rule.PropagationFlags != PropagationFlags.None))
        {
            throw InvalidAccessControl();
        }

        foreach (var expectedRule in expected)
        {
            var matches = actual.Count(rule =>
                rule.IdentityReference is SecurityIdentifier sid
                && sid.Value == expectedRule.Sid
                && NormalizeRights(rule.FileSystemRights) == NormalizeRights(expectedRule.Rights)
                && rule.InheritanceFlags == expectedRule.InheritanceFlags);

            if (matches != 1)
            {
                throw InvalidAccessControl();
            }
        }
    }

    private static FileSystemRights NormalizeRights(FileSystemRights rights) =>
        rights & ~FileSystemRights.Synchronize;

    private static string GetEnvelopeDirectory(string envelopePath) =>
        Path.GetDirectoryName(Path.GetFullPath(envelopePath))
        ?? throw new InvalidOperationException(
            "The machine-secret envelope path must have a parent directory.");

    private static void EnsureExistingPathChainHasNoReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (File.Exists(fullPath))
        {
            EnsureNotReparsePoint(fullPath);
            fullPath = Path.GetDirectoryName(fullPath)
                ?? throw UnsafePath();
        }

        var current = new DirectoryInfo(fullPath);

        while (current is not null)
        {
            if (current.Exists)
            {
                EnsureNotReparsePoint(current.FullName);
            }

            current = current.Parent;
        }
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw UnsafePath();
        }
    }

    private static MachineSecretEnvelopeException InvalidAccessControl() =>
        new(
            MachineSecretEnvelopeFailure.AccessControlInvalid,
            "The Control Desk machine-secret access boundary is unavailable or invalid.");

    private static MachineSecretEnvelopeException UnsafePath() =>
        new(
            MachineSecretEnvelopeFailure.UnsafePath,
            "The Control Desk machine-secret path contains a reparse point.");

    private sealed record ExpectedRule(
        string Sid,
        FileSystemRights Rights,
        InheritanceFlags InheritanceFlags);
}
