namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

public sealed class MachineSecretEnvelopeException : Exception
{
    internal MachineSecretEnvelopeException(
        MachineSecretEnvelopeFailure failure,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public MachineSecretEnvelopeFailure Failure { get; }
}
