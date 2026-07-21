namespace SafarSuite.ControlDesk.Application.Modules.Auth.RecoverLocalOperatorPassword;

public sealed record OperatorRecoveryRequest(
    bool IsElevated,
    bool MachineSecretReadable,
    bool ReissueMachineSecret,
    string Actor,
    string Reason,
    string NewPassword);

public sealed record OperatorRecoveryDecision(
    bool IsAllowed,
    bool ShouldReissueMachineSecret,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static OperatorRecoveryDecision Allow(bool reissue) =>
        new(true, reissue, null, null);

    public static OperatorRecoveryDecision Deny(string code, string message) =>
        new(false, false, code, message);
}

public static class OperatorRecoveryDecisionPolicy
{
    public static OperatorRecoveryDecision Evaluate(OperatorRecoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsElevated)
        {
            return OperatorRecoveryDecision.Deny(
                "elevation-required",
                "Operator recovery requires administrator elevation.");
        }

        if (!request.MachineSecretReadable)
        {
            return OperatorRecoveryDecision.Deny(
                "machine-secret-unavailable",
                "Recovery cannot continue while the machine secret is unavailable or invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Actor) || request.Actor.Trim().Length > 200)
        {
            return OperatorRecoveryDecision.Deny(
                "actor-required",
                "A recovery actor is required and cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1_000)
        {
            return OperatorRecoveryDecision.Deny(
                "reason-required",
                "A recovery reason is required and cannot exceed 1000 characters.");
        }

        if (request.NewPassword.Length < 14)
        {
            return OperatorRecoveryDecision.Deny(
                "password-too-short",
                "The recovered operator password must be at least 14 characters.");
        }

        return OperatorRecoveryDecision.Allow(request.ReissueMachineSecret);
    }
}
