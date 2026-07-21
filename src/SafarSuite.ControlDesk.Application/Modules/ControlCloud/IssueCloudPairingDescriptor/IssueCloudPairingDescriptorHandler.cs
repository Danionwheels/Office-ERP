using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudPairingDescriptor;

public sealed class IssueCloudPairingDescriptorHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public IssueCloudPairingDescriptorHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<LocalServerPairingDescriptorResponse>> HandleAsync(
        IssueCloudPairingDescriptorCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<LocalServerPairingDescriptorResponse>.Failure(validationErrors);
        }

        var request = new IssueLocalServerPairingDescriptorRequest(
            command.BootstrapPackageId,
            command.SetupTokenId,
            CloudInstallationProvisioningValidator.OptionalText(command.ClientCode),
            CloudInstallationProvisioningValidator.OptionalText(command.CustomerName),
            CloudInstallationProvisioningValidator.OptionalText(command.AppServerInstallationId),
            CloudInstallationProvisioningValidator.OptionalText(command.FingerprintHash),
            command.UrlCandidates?.Select(candidate => candidate.Trim()).Where(candidate => candidate.Length > 0).ToArray(),
            CloudInstallationProvisioningValidator.OptionalText(command.TlsCaSha256),
            CloudInstallationProvisioningValidator.OptionalText(command.TlsCertificateSha256),
            CloudInstallationProvisioningValidator.OptionalText(command.ServerPairingKeySha256),
            CloudInstallationProvisioningValidator.NormalizeActor(command.RequestedBy ?? ""));
        var result = await _provisioningClient.IssuePairingDescriptorAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerPairingDescriptorResponse>.Success(result.Descriptor!)
            : Result<LocalServerPairingDescriptorResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        IssueCloudPairingDescriptorCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id is required."));
        }

        AddRequiredText(errors, nameof(command.InstallationId), command.InstallationId, 160);
        AddOptionalText(errors, nameof(command.ClientCode), command.ClientCode, 80);
        AddOptionalText(errors, nameof(command.CustomerName), command.CustomerName, 160);
        AddOptionalText(errors, nameof(command.AppServerInstallationId), command.AppServerInstallationId, 80);
        AddOptionalText(errors, nameof(command.FingerprintHash), command.FingerprintHash, 512);
        AddOptionalText(errors, nameof(command.TlsCaSha256), command.TlsCaSha256, 128);
        AddOptionalText(errors, nameof(command.TlsCertificateSha256), command.TlsCertificateSha256, 128);
        AddOptionalText(errors, nameof(command.ServerPairingKeySha256), command.ServerPairingKeySha256, 128);
        AddOptionalText(errors, nameof(command.RequestedBy), command.RequestedBy, 160);

        if (command.UrlCandidates?.Count > 20)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.UrlCandidates),
                "Pairing descriptor cannot include more than 20 URL candidates."));
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
