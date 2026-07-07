using System.Globalization;
using System.Text.Json;
using SafarSuite.ControlDesk.Application.Common.Abstractions;
using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;

namespace SafarSuite.ControlDesk.Application.Modules.ControlCloud.QueueCloudInstallationSupportCommand;

public sealed class QueueCloudInstallationSupportCommandHandler
{
    private const string PayloadFormatVersion = "safarsuite-control-desk-support-command-v1";
    private const int MaximumCommandLifetimeHours = 168;

    private static readonly HashSet<string> SupportedCommandTypes = new(StringComparer.Ordinal)
    {
        "request_diagnostics",
        "refresh_entitlement"
    };

    private readonly IControlCloudInstallationCommandClient _commandClient;
    private readonly IClock _clock;

    public QueueCloudInstallationSupportCommandHandler(
        IControlCloudInstallationCommandClient commandClient,
        IClock clock)
    {
        _commandClient = commandClient;
        _clock = clock;
    }

    public async Task<Result<QueueCloudInstallationSupportCommandResponse>> HandleAsync(
        QueueCloudInstallationSupportCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = command.InstallationId.Trim();
        var commandType = NormalizeCommandType(command.CommandType);
        var reason = NormalizeRequiredText(command.Reason, "reason", 500);
        var requestedBy = CloudInstallationProvisioningValidator.NormalizeActor(command.RequestedBy);
        var errors = Validate(
            command.ClientId,
            installationId,
            commandType,
            reason,
            command.ExpiresInHours);

        if (errors.Count > 0)
        {
            return Result<QueueCloudInstallationSupportCommandResponse>.Failure(errors);
        }

        var requestedAtUtc = _clock.UtcNow;
        var expiresAtUtc = requestedAtUtc.AddHours(
            Math.Clamp(command.ExpiresInHours, 1, MaximumCommandLifetimeHours));
        var payload = JsonSerializer.SerializeToElement(
            new SupportCommandPayload(
                PayloadFormatVersion,
                commandType!,
                command.ClientId,
                installationId,
                requestedBy,
                reason!,
                requestedAtUtc));
        var request = new QueueInstallationCommandRequest(
            commandType!,
            payload,
            NotBeforeUtc: null,
            expiresAtUtc,
            BuildIdempotencyKey(
                command.ClientId,
                installationId,
                commandType!,
                requestedAtUtc));
        var result = await _commandClient.QueueCommandAsync(
            command.ClientId,
            installationId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? Result<QueueCloudInstallationSupportCommandResponse>.Success(
                ToResponse(result.Command!))
            : Result<QueueCloudInstallationSupportCommandResponse>.Failure(
                ToApplicationError(result));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(
        Guid clientId,
        string installationId,
        string? commandType,
        string? reason,
        int expiresInHours)
    {
        var errors = new List<ApplicationError>();

        if (clientId == Guid.Empty)
        {
            errors.Add(ApplicationError.Validation(
                nameof(clientId),
                "Client id is required."));
        }

        if (string.IsNullOrWhiteSpace(installationId))
        {
            errors.Add(ApplicationError.Validation(
                nameof(installationId),
                "Installation id is required."));
        }
        else if (installationId.Length > 160)
        {
            errors.Add(ApplicationError.Validation(
                nameof(installationId),
                "Installation id cannot exceed 160 characters."));
        }

        if (string.IsNullOrWhiteSpace(commandType))
        {
            errors.Add(ApplicationError.Validation(
                nameof(commandType),
                "Command type is required."));
        }
        else if (!SupportedCommandTypes.Contains(commandType))
        {
            errors.Add(ApplicationError.Validation(
                nameof(commandType),
                "Command type is not available from Control Desk."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            errors.Add(ApplicationError.Validation(
                nameof(reason),
                "Reason is required before queueing a support command."));
        }

        if (expiresInHours is < 1 or > MaximumCommandLifetimeHours)
        {
            errors.Add(ApplicationError.Validation(
                nameof(expiresInHours),
                "Command expiry must be between 1 and 168 hours."));
        }

        return errors;
    }

    private static QueueCloudInstallationSupportCommandResponse ToResponse(
        InstallationCommandResponse command)
    {
        return new QueueCloudInstallationSupportCommandResponse(
            command.CommandId,
            command.ClientId,
            command.InstallationId,
            command.CommandVersion,
            command.CommandType,
            command.Status,
            command.IdempotencyKey,
            command.QueuedAtUtc,
            command.NotBeforeUtc,
            command.ExpiresAtUtc,
            command.AcknowledgedAtUtc,
            command.AcknowledgementStatus,
            command.AcknowledgementDetail,
            command.Signature.KeyId,
            command.Signature.PayloadSha256);
    }

    private static ApplicationError ToApplicationError(
        ControlCloudInstallationCommandClientResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.Detail)
            ? "Control Cloud command request failed."
            : result.Detail;

        return result.FailureCode switch
        {
            "InstallationNotFound" => ApplicationError.NotFound("installationId", detail),
            "InstallationClientMismatch" => ApplicationError.Conflict("installationId", detail),
            "CommandTypeRequired" => ApplicationError.Validation("commandType", detail),
            "CommandPayloadInvalid" => ApplicationError.Validation("payload", detail),
            "CommandExpiryInvalid" => ApplicationError.Validation("expiresInHours", detail),
            "CommandInvalid" => ApplicationError.Validation("commandType", detail),
            "ControlCloudCommandNotConfigured" => ApplicationError.ServiceUnavailable(detail),
            "ControlCloudCommandUnavailable" => ApplicationError.ServiceUnavailable(detail),
            "ControlCloudCommandResponseInvalid" => ApplicationError.ServiceUnavailable(detail),
            _ => ApplicationError.Unexpected(detail)
        };
    }

    private static string? NormalizeCommandType(string value)
    {
        var normalized = value.Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToLowerInvariant();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeRequiredText(
        string value,
        string target,
        int maxLength)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string BuildIdempotencyKey(
        Guid clientId,
        string installationId,
        string commandType,
        DateTimeOffset requestedAtUtc)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"support:{clientId:N}:{installationId}:{commandType}:{requestedAtUtc:yyyyMMddHHmmss}:{Guid.NewGuid():N}");
    }

    private sealed record SupportCommandPayload(
        string FormatVersion,
        string CommandType,
        Guid ClientId,
        string InstallationId,
        string RequestedBy,
        string Reason,
        DateTimeOffset RequestedAtUtc);
}
