using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationSetupToken;

public sealed class CreateCloudInstallationSetupTokenHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public CreateCloudInstallationSetupTokenHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<LocalServerSetupTokenResponse>> HandleAsync(
        CreateCloudInstallationSetupTokenCommand command,
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
            command.SyncTopologyId);

        if (validationErrors.Count > 0)
        {
            return Result<LocalServerSetupTokenResponse>.Failure(validationErrors);
        }

        var request = new CreateLocalServerSetupTokenRequest(
            command.ExpiresInHours,
            CloudInstallationProvisioningValidator.NormalizeActor(command.CreatedBy),
            ControlCloudBootstrapModes.NormalizeOrDefault(command.DeploymentMode),
            SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode),
            CloudInstallationProvisioningValidator.OptionalText(command.SiteId),
            SafarSuiteDeploymentSiteRoles.NormalizeOrDefault(
                command.SiteRole,
                SafarSuiteClientDeploymentModes.NormalizeOrDefault(command.ClientDeploymentMode)),
            CloudInstallationProvisioningValidator.OptionalText(command.ParentSiteId),
            CloudInstallationProvisioningValidator.OptionalText(command.BranchCode),
            CloudInstallationProvisioningValidator.OptionalText(command.SyncTopologyId));

        var result = await _provisioningClient.CreateSetupTokenAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerSetupTokenResponse>.Success(result.SetupToken!)
            : Result<LocalServerSetupTokenResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }
}
