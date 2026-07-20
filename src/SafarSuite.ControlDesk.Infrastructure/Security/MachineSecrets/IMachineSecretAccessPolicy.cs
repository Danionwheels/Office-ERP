namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

internal interface IMachineSecretAccessPolicy
{
    void PrepareForWrite(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile);

    void ProtectTransientFile(string path);

    void ProtectEnvelope(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile);

    void ValidateForRead(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile);

    void Repair(
        string envelopePath,
        ControlDeskMachineSecretAccessProfile profile);
}
