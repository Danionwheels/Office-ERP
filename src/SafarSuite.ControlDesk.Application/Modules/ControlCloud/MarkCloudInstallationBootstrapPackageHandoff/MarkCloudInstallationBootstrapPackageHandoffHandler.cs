using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.MarkCloudInstallationBootstrapPackageHandoff;

public sealed class MarkCloudInstallationBootstrapPackageHandoffHandler
{
    private readonly IControlCloudInstallationProvisioningClient _provisioningClient;

    public MarkCloudInstallationBootstrapPackageHandoffHandler(
        IControlCloudInstallationProvisioningClient provisioningClient)
    {
        _provisioningClient = provisioningClient;
    }

    public async Task<Result<LocalServerBootstrapPackageHandoffResponse>> HandleAsync(
        MarkCloudInstallationBootstrapPackageHandoffCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);

        if (validationErrors.Count > 0)
        {
            return Result<LocalServerBootstrapPackageHandoffResponse>.Failure(validationErrors);
        }

        var request = new MarkLocalServerBootstrapPackageHandoffRequest(
            command.Channel.Trim(),
            command.Recipient.Trim(),
            CloudInstallationProvisioningValidator.NormalizeActor(command.MarkedBy),
            NormalizePreflightAcknowledgements(command.PreflightAcknowledgements),
            CloudInstallationProvisioningValidator.OptionalText(command.Note));

        var result = await _provisioningClient.MarkBootstrapPackageHandoffAsync(
            command.ClientId,
            command.InstallationId.Trim(),
            command.BootstrapPackageId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<LocalServerBootstrapPackageHandoffResponse>.Success(result.Response!)
            : Result<LocalServerBootstrapPackageHandoffResponse>.Failure(
                CloudInstallationProvisioningValidator.ToApplicationError(
                    result.FailureCode,
                    result.Detail));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        MarkCloudInstallationBootstrapPackageHandoffCommand command)
    {
        var errors = new List<ApplicationError>();

        if (command.ClientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.ClientId),
                "Client id is required."));
        }

        if (command.BootstrapPackageId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(command.BootstrapPackageId),
                "Bootstrap package id is required."));
        }

        AddRequiredText(errors, nameof(command.InstallationId), command.InstallationId, 160);
        AddRequiredText(errors, nameof(command.Channel), command.Channel, 40);
        AddOptionalText(errors, nameof(command.Recipient), command.Recipient, 160);
        AddRequiredText(errors, nameof(command.MarkedBy), command.MarkedBy, 120);
        AddOptionalText(errors, nameof(command.Note), command.Note, 500);
        AddPreflightAcknowledgements(errors, command.PreflightAcknowledgements);

        return errors;
    }

    private static void AddPreflightAcknowledgements(
        ICollection<ApplicationError> errors,
        IReadOnlyCollection<string>? acknowledgements)
    {
        var normalized = NormalizePreflightAcknowledgements(acknowledgements);
        var unsupported = GetUnsupportedPreflightAcknowledgements(acknowledgements);
        var missing = LocalServerBootstrapPackageHandoffPreflight.RequiredKeys
            .Where(requiredKey => !normalized.Contains(requiredKey, StringComparer.Ordinal))
            .Select(LocalServerBootstrapPackageHandoffPreflight.ToLabel)
            .ToArray();

        if (unsupported.Count > 0)
        {
            errors.Add(ApplicationError.Validation(
                "PreflightAcknowledgements",
                $"Unsupported handoff preflight acknowledgement(s): {string.Join(", ", unsupported)}."));
        }

        if (missing.Length > 0)
        {
            errors.Add(ApplicationError.Validation(
                "PreflightAcknowledgements",
                $"Acknowledge setup preflight before handoff: {string.Join(", ", missing)}."));
        }
    }

    private static IReadOnlyCollection<string> NormalizePreflightAcknowledgements(
        IReadOnlyCollection<string>? acknowledgements)
    {
        var acknowledgedKeys = (acknowledgements ?? Array.Empty<string>())
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        return LocalServerBootstrapPackageHandoffPreflight.RequiredKeys
            .Where(acknowledgedKeys.Contains)
            .ToArray();
    }

    private static IReadOnlyCollection<string> GetUnsupportedPreflightAcknowledgements(
        IReadOnlyCollection<string>? acknowledgements)
    {
        return (acknowledgements ?? Array.Empty<string>())
            .Select(value => value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .Where(value => !LocalServerBootstrapPackageHandoffPreflight.RequiredKeys.Contains(value, StringComparer.Ordinal))
            .ToArray();
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
