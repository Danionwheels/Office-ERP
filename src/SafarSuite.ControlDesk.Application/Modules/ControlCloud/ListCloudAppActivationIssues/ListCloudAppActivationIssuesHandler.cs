using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudAppActivationIssues;

public sealed class ListCloudAppActivationIssuesHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public ListCloudAppActivationIssuesHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<ListCloudAppActivationIssuesResult>> HandleAsync(
        ListCloudAppActivationIssuesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(query);

        if (validationErrors.Count > 0)
        {
            return Result<ListCloudAppActivationIssuesResult>.Failure(validationErrors);
        }

        var result = await _provisioningClient.ListAppActivationIssuesAsync(
            query.ClientId,
            query.InstallationId,
            query.AppServerInstallationId,
            query.Query,
            query.Take,
            cancellationToken);

        return result.IsSuccess
            ? Result<ListCloudAppActivationIssuesResult>.Success(
                new ListCloudAppActivationIssuesResult(result.Response!.Issues))
            : Result<ListCloudAppActivationIssuesResult>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        ListCloudAppActivationIssuesQuery query)
    {
        var errors = new List<ApplicationError>();

        if (query.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.ClientId),
                "Client id is required."));
        }

        AddOptionalText(errors, nameof(query.InstallationId), query.InstallationId, 160);
        AddOptionalText(errors, nameof(query.Query), query.Query, 200);

        if (query.Take < 1 || query.Take > 500)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.Take),
                "Take must be between 1 and 500."));
        }

        return errors;
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
