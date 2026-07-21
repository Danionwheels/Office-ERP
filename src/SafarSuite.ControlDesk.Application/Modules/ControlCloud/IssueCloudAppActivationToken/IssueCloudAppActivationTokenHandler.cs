using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudAppActivationToken;

public sealed class IssueCloudAppActivationTokenHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public IssueCloudAppActivationTokenHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<IssueSafarSuiteAppActivationTokenResponse>> HandleAsync(
        IssueCloudAppActivationTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<IssueSafarSuiteAppActivationTokenResponse>.Failure(validationErrors);
        }

        var request = new IssueSafarSuiteAppActivationTokenRequest(
            command.ActivationRequestId,
            command.ServerInstallationId,
            command.FingerprintHash.Trim(),
            command.ServerPublicKey.Trim(),
            string.IsNullOrWhiteSpace(command.RequestedBy)
                ? "SafarSuite Control Desk"
                : command.RequestedBy.Trim(),
            command.ReplacesActivationIssueId);
        var result = await _provisioningClient.IssueAppActivationTokenAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<IssueSafarSuiteAppActivationTokenResponse>.Success(result.Response!)
            : Result<IssueSafarSuiteAppActivationTokenResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        IssueCloudAppActivationTokenCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id is required."));
        }

        if (command.ServerInstallationId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ServerInstallationId),
                "App server installation id is required."));
        }

        if (command.ReplacesActivationIssueId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ReplacesActivationIssueId),
                "Replacement activation issue id cannot be empty."));
        }

        AddRequiredText(errors, nameof(command.InstallationId), command.InstallationId, 160);
        AddRequiredText(errors, nameof(command.FingerprintHash), command.FingerprintHash, 512);
        AddRequiredText(errors, nameof(command.ServerPublicKey), command.ServerPublicKey, 4096);
        AddOptionalText(errors, nameof(command.RequestedBy), command.RequestedBy, 120);

        return errors;
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
