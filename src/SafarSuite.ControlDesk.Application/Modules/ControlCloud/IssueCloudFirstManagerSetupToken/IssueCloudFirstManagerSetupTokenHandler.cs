using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudFirstManagerSetupToken;

public sealed class IssueCloudFirstManagerSetupTokenHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public IssueCloudFirstManagerSetupTokenHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<IssueLocalServerFirstManagerSetupTokenResponse>> HandleAsync(
        IssueCloudFirstManagerSetupTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueLocalServerFirstManagerSetupTokenResponse>.Failure(validationErrors);
        }

        var request = new IssueLocalServerFirstManagerSetupTokenRequest(
            command.PendingDeviceRequestId,
            command.ManagerDisplayName.Trim(),
            CloudInstallationProvisioningValidator.OptionalText(command.ManagerEmail),
            CloudInstallationProvisioningValidator.NormalizeActor(command.CreatedBy ?? ""),
            command.ExpiresInHours,
            NormalizePurpose(command.Purpose),
            CloudInstallationProvisioningValidator.OptionalText(command.RecoveryReason));
        var result = await _provisioningClient.IssueFirstManagerSetupTokenAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<IssueLocalServerFirstManagerSetupTokenResponse>.Success(result.Response!)
            : Result<IssueLocalServerFirstManagerSetupTokenResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        IssueCloudFirstManagerSetupTokenCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id is required."));
        }

        if (command.PendingDeviceRequestId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.PendingDeviceRequestId),
                "Pending device request id is required."));
        }

        AddRequiredText(errors, nameof(command.InstallationId), command.InstallationId, 160);
        AddRequiredText(errors, nameof(command.ManagerDisplayName), command.ManagerDisplayName, 160);
        AddOptionalText(errors, nameof(command.ManagerEmail), command.ManagerEmail, 160);
        AddOptionalText(errors, nameof(command.CreatedBy), command.CreatedBy, 160);
        AddOptionalText(errors, nameof(command.RecoveryReason), command.RecoveryReason, 500);

        if (command.ExpiresInHours is < 1 or > 168)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ExpiresInHours),
                "First-manager setup token expiry must be between 1 and 168 hours."));
        }

        var purpose = NormalizePurpose(command.Purpose);
        if (!string.Equals(purpose, LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap, StringComparison.Ordinal)
            && !string.Equals(purpose, LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery, StringComparison.Ordinal))
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.Purpose),
                "First-manager setup token purpose is not supported."));
        }

        if (string.Equals(purpose, LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(command.RecoveryReason))
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.RecoveryReason),
                "A recovery reason is required for manager recovery setup tokens."));
        }

        return errors;
    }

    private static string NormalizePurpose(string? value)
    {
        var normalized = CloudInstallationProvisioningValidator.OptionalText(value);

        return string.IsNullOrWhiteSpace(normalized)
            ? LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap
            : normalized;
    }

    private static void AddRequiredText(
        ICollection<ApplicationError> errors,
        string target,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(ApplicationError.Validation(target, $"{target} is required."));

            return;
        }

        AddOptionalText(errors, target, value, maxLength);
    }

    private static void AddOptionalText(
        ICollection<ApplicationError> errors,
        string target,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(ApplicationError.Validation(
                target,
                $"{target} cannot exceed {maxLength} characters."));
        }
    }
}
