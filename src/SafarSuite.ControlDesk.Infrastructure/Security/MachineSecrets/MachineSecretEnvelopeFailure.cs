namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public enum MachineSecretEnvelopeFailure
{
    InvalidEnvelope,
    ProtectionFailed,
    LifecycleBusy,
    WriteVerificationFailed,
    AccessControlInvalid,
    AccessControlFailed,
    UnsafePath
}
