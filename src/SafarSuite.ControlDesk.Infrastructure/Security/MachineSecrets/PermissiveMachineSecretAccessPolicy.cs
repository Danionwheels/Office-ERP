namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

internal sealed class PermissiveMachineSecretAccessPolicy : IMachineSecretAccessPolicy
{
    public static PermissiveMachineSecretAccessPolicy Instance { get; } = new();

    private PermissiveMachineSecretAccessPolicy()
    {
    }

    public void PrepareForWrite(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
        var directory = Path.GetDirectoryName(envelopePath)
            ?? throw new InvalidOperationException(
                "The machine-secret envelope path must have a parent directory.");
        Directory.CreateDirectory(directory);
    }

    public void ProtectTransientFile(string path)
    {
    }

    public void ProtectEnvelope(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
    }

    public void ValidateForRead(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
    }

    public void Repair(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile)
    {
    }
}
