using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationBootstrapPackages;

public sealed class ListCloudInstallationBootstrapPackagesHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public ListCloudInstallationBootstrapPackagesHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<LocalServerBootstrapPackageRegisterResponse>> HandleAsync(
        ListCloudInstallationBootstrapPackagesQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(query);

        if (validationErrors.Count > 0)
        {
            return Result<LocalServerBootstrapPackageRegisterResponse>.Failure(validationErrors);
        }

        var result = await _provisioningClient.ListBootstrapPackagesAsync(
            query.ClientId,
            query.InstallationId.Trim(),
            query.Take,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerBootstrapPackageRegisterResponse>.Success(result.Response!)
            : Result<LocalServerBootstrapPackageRegisterResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        ListCloudInstallationBootstrapPackagesQuery query)
    {
        var errors = new List<ApplicationError>();

        if (query.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.ClientId),
                "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(query.InstallationId))
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.InstallationId),
                "Installation id is required."));
        }
        else if (query.InstallationId.Trim().Length > 160)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.InstallationId),
                "Installation id cannot exceed 160 characters."));
        }

        if (query.Take is < 1 or > 200)
        {
            errors.Add(ApplicationError.Validation(
                nameof(query.Take),
                "Take must be between 1 and 200."));
        }

        return errors;
    }
}
