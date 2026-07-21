using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud;

internal static class CloudInstallationProvisioningValidator
{
    public static IReadOnlyCollection<ApplicationError> Validate(
        Guid clientId,
        string installationId,
        int expiresInHours,
        string deploymentMode,
        string? clientDeploymentMode,
        string? siteId,
        string? siteRole,
        string? parentSiteId,
        string? branchCode,
        string? syncTopologyId,
        string? localServerVersion = null,
        string? safarSuiteAppVersion = null)
    {
        var errors = new List<ApplicationError>();

        if (clientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(clientId),
                "Client id is required."));
        }

        AddRequiredText(errors, nameof(installationId), installationId, 160);
        AddRequiredText(errors, nameof(deploymentMode), deploymentMode, 64);
        AddOptionalText(errors, nameof(clientDeploymentMode), clientDeploymentMode, 64);
        AddOptionalText(errors, nameof(siteId), siteId, 160);
        AddOptionalText(errors, nameof(siteRole), siteRole, 64);
        AddOptionalText(errors, nameof(parentSiteId), parentSiteId, 160);
        AddOptionalText(errors, nameof(branchCode), branchCode, 80);
        AddOptionalText(errors, nameof(syncTopologyId), syncTopologyId, 160);
        AddOptionalText(errors, nameof(safarSuiteAppVersion), safarSuiteAppVersion, 64);

        if (localServerVersion is not null)
        {
            AddRequiredText(errors, nameof(localServerVersion), localServerVersion, 64);
        }

        if (expiresInHours is < 1 or > 168)
        {
            errors.Add(ApplicationError.Validation(
                nameof(expiresInHours),
                "Setup token expiry must be between 1 and 168 hours."));
        }

        if (!ControlCloudBootstrapModes.IsSupported(deploymentMode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(deploymentMode),
                "Bootstrap mode is not supported."));
        }

        if (!SafarSuiteClientDeploymentModes.IsSupported(clientDeploymentMode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(clientDeploymentMode),
                "Client deployment mode is not supported."));
        }

        if (!SafarSuiteDeploymentSiteRoles.IsSupported(siteRole))
        {
            errors.Add(ApplicationError.Validation(
                nameof(siteRole),
                "Site role is not supported."));
        }

        return errors;
    }

    public static string NormalizeActor(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "SafarSuite Control Desk"
            : value.Trim();
    }

    public static string? OptionalText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static ApplicationError ToApplicationError(
        string? failureCode,
        string? detail)
    {
        var message = string.IsNullOrWhiteSpace(detail)
            ? "Control Cloud provisioning request failed."
            : detail;

        return failureCode switch
        {
            "ClientNotFound" => ApplicationError.NotFound("clientId", message),
            "InstallationNotFound" => ApplicationError.NotFound("installationId", message),
            "DiagnosticsNotFound" => ApplicationError.NotFound("installationId", message),
            "InstallationClientMismatch" => ApplicationError.Conflict("installationId", message),
            "InstallationNotRegistered" => ApplicationError.Conflict("installationId", message),
            "EntitlementNotFound" => ApplicationError.Conflict("installationId", message),
            "EntitlementInstallationMismatch" => ApplicationError.Conflict("installationId", message),
            "EntitlementPayloadInvalid" => ApplicationError.Conflict("installationId", message),
            "SetupTokenScopeMismatch" => ApplicationError.Conflict("setupToken", message),
            "SetupTokenNotUsable" => ApplicationError.Conflict("setupToken", message),
            "ProviderAccessDenied" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessNotConfigured" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessExpired" => ApplicationError.ServiceUnavailable(message),
            "ProviderAccessScopeDenied" => ApplicationError.ServiceUnavailable(message),
            "ControlCloudProviderAccessDenied" => ApplicationError.ServiceUnavailable(message),
            "ControlCloudProviderAccessNotConfigured" => ApplicationError.ServiceUnavailable(message),
            "BootstrapModeUnsupported" => ApplicationError.Validation("deploymentMode", message),
            "ClientDeploymentModeUnsupported" => ApplicationError.Validation("clientDeploymentMode", message),
            "SiteRoleUnsupported" => ApplicationError.Validation("siteRole", message),
            "BootstrapPackageNotFound" => ApplicationError.NotFound("bootstrapPackageId", message),
            "BootstrapPackageIdRequired" => ApplicationError.Validation("bootstrapPackageId", message),
            "BootstrapPackageTakeInvalid" => ApplicationError.Validation("take", message),
            "HandoffChannelRequired" => ApplicationError.Validation("channel", message),
            "HandoffMarkedByRequired" => ApplicationError.Validation("markedBy", message),
            "InstallationIdRequired" => ApplicationError.Validation("installationId", message),
            "ClientIdRequired" => ApplicationError.Validation("clientId", message),
            "AppServerInstallationIdInvalid" => ApplicationError.Validation("appServerInstallationId", message),
            "AppServerInstallationIdRequired" => ApplicationError.Validation("serverInstallationId", message),
            "AppFingerprintRequired" => ApplicationError.Validation("fingerprintHash", message),
            "AppServerPublicKeyRequired" => ApplicationError.Validation("serverPublicKey", message),
            "PendingDeviceRequestIdRequired" => ApplicationError.Validation("pendingDeviceRequestId", message),
            "ManagerDisplayNameRequired" => ApplicationError.Validation("managerDisplayName", message),
            "FirstManagerSetupTokenInvalid" => ApplicationError.Validation("firstManagerSetupToken", message),
            "CloudBaseUrlInvalid" => ApplicationError.Validation("cloudBaseUrl", message),
            "InstallScriptUrlInvalid" => ApplicationError.Validation("installScriptUrl", message),
            _ => ApplicationError.Unexpected(message)
        };
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
