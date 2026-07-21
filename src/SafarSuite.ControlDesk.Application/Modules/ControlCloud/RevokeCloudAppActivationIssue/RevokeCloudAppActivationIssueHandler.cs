using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.RevokeCloudAppActivationIssue;

public sealed class RevokeCloudAppActivationIssueHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public RevokeCloudAppActivationIssueHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<SafarSuiteAppActivationIssueResponse>> HandleAsync(
        RevokeCloudAppActivationIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<SafarSuiteAppActivationIssueResponse>.Failure(validationErrors);
        }

        var request = new RevokeSafarSuiteAppActivationIssueRequest(
            command.RevokedBy.Trim(),
            command.Reason.Trim());
        var result = await _provisioningClient.RevokeAppActivationIssueAsync(
            command.ClientId,
            command.ActivationIssueId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<SafarSuiteAppActivationIssueResponse>.Success(result.Issue!)
            : Result<SafarSuiteAppActivationIssueResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        RevokeCloudAppActivationIssueCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id is required."));
        }

        if (command.ActivationIssueId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ActivationIssueId),
                "Activation issue id is required."));
        }

        AddRequiredText(errors, nameof(command.RevokedBy), command.RevokedBy, 120);
        AddRequiredText(errors, nameof(command.Reason), command.Reason, 500);

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

        if (value.Trim().Length > maxLength)
        {
            errors.Add(ApplicationError.Validation(
                target,
                $"{target} cannot exceed {maxLength} characters."));
        }
    }
}
