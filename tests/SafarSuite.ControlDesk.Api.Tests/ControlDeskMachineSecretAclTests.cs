using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Runtime.Versioning;
using SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

namespace SafarSuite.ControlDesk.Api.Tests;

[SupportedOSPlatform("windows")]
public sealed class ControlDeskMachineSecretAclTests
{
    [Fact]
    public void Installed_acl_repairs_drift_without_changing_the_machine_secret()
    {
        if (!CanRunElevatedWindowsProof())
        {
            return;
        }

        var root = Path.Combine(
            Path.GetTempPath(),
            $"SafarSuite-ControlDesk-MachineSecret-{Guid.NewGuid():N}");
        var envelopePath = Path.Combine(root, ControlDeskMachineSecretPaths.EnvelopeFileName);

        try
        {
            var store = new ControlDeskMachineSecretEnvelopeStore(
                envelopePath,
                ControlDeskMachineSecretAccessProfile.InstalledApiService);
            using var created = store.CreateOrLoad();
            var originalKey = created.CopySessionSigningKey();
            var originalGeneration = created.GenerationId;
            var originalFingerprint = created.CiphertextFingerprint;

            try
            {
                var security = new FileInfo(envelopePath).GetAccessControl();
                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier("S-1-5-32-545"),
                    FileSystemRights.Read,
                    AccessControlType.Allow));
                new FileInfo(envelopePath).SetAccessControl(security);

                var failure = Assert.Throws<MachineSecretEnvelopeException>(store.Read);
                Assert.Equal(MachineSecretEnvelopeFailure.AccessControlInvalid, failure.Failure);

                using var repaired = store.RepairAccessControl();
                var repairedKey = repaired.CopySessionSigningKey();

                try
                {
                    Assert.Equal(originalGeneration, repaired.GenerationId);
                    Assert.Equal(originalFingerprint, repaired.CiphertextFingerprint);
                    Assert.Equal(originalKey, repairedKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(repairedKey);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(originalKey);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static bool CanRunElevatedWindowsProof()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
