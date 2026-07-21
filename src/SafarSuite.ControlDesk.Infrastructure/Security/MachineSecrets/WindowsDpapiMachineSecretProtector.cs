using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDpapiMachineSecretProtector : IMachineSecretProtector
{
    private static readonly byte[] OptionalEntropy =
        Encoding.UTF8.GetBytes("SafarSuite.ControlDesk/MachineSecrets/v1");

    public byte[] Protect(byte[] plaintext) =>
        ProtectedData.Protect(plaintext, OptionalEntropy, DataProtectionScope.LocalMachine);

    public byte[] Unprotect(byte[] ciphertext) =>
        ProtectedData.Unprotect(ciphertext, OptionalEntropy, DataProtectionScope.LocalMachine);
}
