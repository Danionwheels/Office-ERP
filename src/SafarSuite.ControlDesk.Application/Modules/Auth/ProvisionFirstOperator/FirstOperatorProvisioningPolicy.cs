namespace SafarSuite.ControlDesk.Application.Modules.Auth.ProvisionFirstOperator;

public sealed record FirstOperatorProvisioningRequest(
    bool IsElevated,
    bool OperatorAlreadyExists,
    string Email,
    string FullName,
    string Password);

public sealed record FirstOperatorProvisioningDecision(
    bool IsAllowed,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static FirstOperatorProvisioningDecision Allow() => new(true, null, null);

    public static FirstOperatorProvisioningDecision Deny(string code, string message) =>
        new(false, code, message);
}

public static class FirstOperatorProvisioningPolicy
{
    public static FirstOperatorProvisioningDecision Evaluate(
        FirstOperatorProvisioningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsElevated)
        {
            return FirstOperatorProvisioningDecision.Deny(
                "elevation-required",
                "First-operator provisioning requires administrator elevation.");
        }

        if (request.OperatorAlreadyExists)
        {
            return FirstOperatorProvisioningDecision.Deny(
                "already-provisioned",
                "First-operator provisioning is single-use and an operator already exists.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return FirstOperatorProvisioningDecision.Deny(
                "email-required",
                "An operator email address is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return FirstOperatorProvisioningDecision.Deny(
                "full-name-required",
                "An operator full name is required.");
        }

        if (request.Password.Length < 14)
        {
            return FirstOperatorProvisioningDecision.Deny(
                "password-too-short",
                "The first-operator password must be at least 14 characters.");
        }

        return FirstOperatorProvisioningDecision.Allow();
    }
}
