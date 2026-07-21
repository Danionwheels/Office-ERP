namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

internal interface IMachineSecretProtector
{
    byte[] Protect(byte[] plaintext);

    byte[] Unprotect(byte[] ciphertext);
}
