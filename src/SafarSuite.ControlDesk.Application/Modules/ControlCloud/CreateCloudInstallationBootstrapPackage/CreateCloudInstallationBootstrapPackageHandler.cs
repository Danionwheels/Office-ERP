using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationBootstrapPackage;

public sealed class CreateCloudInstallationBootstrapPackageHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public CreateCloudInstallationBootstrapPackageHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<LocalServerBootstrapPackageResponse>> HandleAsync(
        CreateCloudInstallationBootstrapPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = CloudInstallationProvisioningValidator.Validate(
            command.ClientId,
            command.InstallationId,
            command.ExpiresInHours,
            command.DeploymentMode,
            command.ClientDeploymentMode,
            command.SiteId,
            command.SiteRole,
            command.ParentSiteId,
            command.BranchCode,
            command.SyncTopologyId,
            command.LocalServerVersion,
            command.SafarSuiteAppVersion);

        if (validationErrors.Count > 0)
        {
            return Result<LocalServerBootstrapPackageResponse>.Failure(validationErrors);
        }

        var request = new CreateLocalServerBootstrapPackageRequest(
            command.ExpiresInHours,
            CloudInstallationProvisioningValidator.NormalizeActor(command.CreatedBy),
            ControlCloudBootstrapModes.NormalizeOrDefault(command.DeploymentMode),
            command.LocalServerVersion.Trim(),
            CloudInstallationProvisioningValidator.OptionalText(command.SafarSuiteAppVersion),
            SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode),
            CloudInstallationProvisioningValidator.OptionalText(command.SiteId),
            SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
                command.SiteRole,
                SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode)),
            CloudInstallationProvisioningValidator.OptionalText(command.ParentSiteId),
            CloudInstallationProvisioningValidator.OptionalText(command.BranchCode),
            CloudInstallationProvisioningValidator.OptionalText(command.SyncTopologyId));

        var result = await _provisioningClient.CreateBootstrapPackageAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerBootstrapPackageResponse>.Success(result.BootstrapPackage!)
            : Result<LocalServerBootstrapPackageResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
