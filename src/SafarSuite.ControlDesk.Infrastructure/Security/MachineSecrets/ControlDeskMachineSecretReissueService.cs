namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public sealed record ControlDeskMachineSecretReissueResult(
    Guid GenerationId,
    string SessionSigningKeyId,
    string CiphertextFingerprint,
    string Actor,
    string Reason);

public sealed class ControlDeskMachineSecretReissueService(
    ControlDeskMachineSecretEnvelopeStore store)
{
    public ControlDeskMachineSecretReissueResult Reissue(
        string actor,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(actor) || actor.Trim().Length > 200)
        {
            throw new ArgumentException(
                "A reissue actor is required and cannot exceed 200 characters.",
                nameof(actor));
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1_000)
        {
            throw new ArgumentException(
                "A reissue reason is required and cannot exceed 1000 characters.",
                nameof(reason));
        }

        using var snapshot = store.Replace();
        return new ControlDeskMachineSecretReissueResult(
            snapshot.GenerationId,
            snapshot.SessionSigningKeyId,
            snapshot.CiphertextFingerprint,
            actor.Trim(),
            reason.Trim());
    }
}
