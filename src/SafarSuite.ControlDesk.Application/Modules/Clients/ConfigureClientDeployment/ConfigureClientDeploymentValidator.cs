using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Common.Validation;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ConfigureClientDeployment;

public sealed class ConfigureClientDeploymentValidator : IValidator<ConfigureClientDeploymentCommand>
{
    public IReadOnlyCollection<ApplicationError> Validate(ConfigureClientDeploymentCommand value)
    {
        var errors = new List<ApplicationError>();

        if (value.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.ClientId),
                "Client id is required."));
        }

        AddRequiredText(errors, nameof(value.InstallationId), value.InstallationId, 160);
        AddRequiredText(errors, nameof(value.DisplayName), value.DisplayName, 128);
        AddRequiredText(errors, nameof(value.BootstrapMode), value.BootstrapMode, 64);
        AddRequiredText(errors, nameof(value.ClientDeploymentMode), value.ClientDeploymentMode, 64);
        AddRequiredText(errors, nameof(value.SiteId), value.SiteId, 96);
        AddRequiredText(errors, nameof(value.SiteRole), value.SiteRole, 64);
        AddRequiredText(errors, nameof(value.LocalServerVersion), value.LocalServerVersion, 64);
        AddOptionalText(errors, nameof(value.ParentSiteId), value.ParentSiteId, 96);
        AddOptionalText(errors, nameof(value.BranchCode), value.BranchCode, 64);
        AddOptionalText(errors, nameof(value.SyncTopologyId), value.SyncTopologyId, 96);
        AddOptionalText(errors, nameof(value.SafarSuiteAppVersion), value.SafarSuiteAppVersion, 64);

        if (!ControlCloudBootstrapModes.IsSupported(value.BootstrapMode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.BootstrapMode),
                "Bootstrap mode is not supported."));
        }

        if (!SafarSuiteClientDeploymentModes.IsSupported(value.ClientDeploymentMode))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.ClientDeploymentMode),
                "Client deployment mode is not supported."));
        }

        if (!SafarSuiteDeploymentSiteRoles.IsSupported(value.SiteRole))
        {
            errors.Add(ApplicationError.Validation(
                nameof(value.SiteRole),
                "Site role is not supported."));
        }

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
