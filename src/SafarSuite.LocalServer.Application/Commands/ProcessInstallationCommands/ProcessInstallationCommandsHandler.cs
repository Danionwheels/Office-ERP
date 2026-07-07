using System.Runtime.InteropServices;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;
using SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;
using SafarSuite.LocalServer.Domain.Commands;

namespace SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;

public sealed class ProcessInstallationCommandsHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IControlCloudInstallationCommandClient _commandClient;
    private readonly ILocalServerInstallationCommandVerifier _commandVerifier;
    private readonly ILocalServerAppActivationRevocationStore _appActivationRevocations;
    private readonly ILocalServerClock _clock;
    private readonly PullEntitlementFromControlCloudHandler _entitlementPullHandler;
    private readonly CreateLocalServerDiagnosticsBundleHandler _diagnosticsBundleHandler;
    private readonly UploadDiagnosticsToControlCloudHandler _diagnosticsUploadHandler;

    public ProcessInstallationCommandsHandler(
        IControlCloudInstallationCommandClient commandClient,
        ILocalServerInstallationCommandVerifier commandVerifier,
        ILocalServerAppActivationRevocationStore appActivationRevocations,
        ILocalServerClock clock,
        PullEntitlementFromControlCloudHandler entitlementPullHandler,
        CreateLocalServerDiagnosticsBundleHandler diagnosticsBundleHandler,
        UploadDiagnosticsToControlCloudHandler diagnosticsUploadHandler)
    {
        _commandClient = commandClient;
        _commandVerifier = commandVerifier;
        _appActivationRevocations = appActivationRevocations;
        _clock = clock;
        _entitlementPullHandler = entitlementPullHandler;
        _diagnosticsBundleHandler = diagnosticsBundleHandler;
        _diagnosticsUploadHandler = diagnosticsUploadHandler;
    }

    public async Task<ProcessInstallationCommandsResult> HandleAsync(
        ProcessInstallationCommandsCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = command.InstallationId.Trim();

        if (command.ClientId == Guid.Empty)
        {
            return ProcessInstallationCommandsResult.Failure(
                "ClientIdRequired",
                "Client id is required before processing local-server commands.");
        }

        if (installationId.Length == 0)
        {
            return ProcessInstallationCommandsResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before processing local-server commands.");
        }

        var pendingResult = await _commandClient.GetPendingAsync(
            installationId,
            cancellationToken);

        if (!pendingResult.IsSuccess)
        {
            return ProcessInstallationCommandsResult.Failure(
                pendingResult.FailureCode ?? "ControlCloudCommandPullFailed",
                pendingResult.Detail ?? "Control Cloud did not return pending installation commands.");
        }

        var pendingCommands = pendingResult.Response!.Commands
            .OrderBy(queuedCommand => queuedCommand.CommandVersion)
            .ToArray();
        var results = new List<ProcessedInstallationCommandResult>();

        foreach (var queuedCommand in pendingCommands)
        {
            results.Add(await ProcessOneAsync(
                command.ClientId,
                installationId,
                command.LocalServerVersion,
                command.DeploymentProfile,
                queuedCommand,
                cancellationToken));
        }

        return ProcessInstallationCommandsResult.Success(
            pendingCommands.Length,
            results);
    }

    private async Task<ProcessedInstallationCommandResult> ProcessOneAsync(
        Guid clientId,
        string installationId,
        string localServerVersion,
        LocalServerDeploymentProfileResponse? deploymentProfile,
        InstallationCommandResponse queuedCommand,
        CancellationToken cancellationToken)
    {
        var verification = _commandVerifier.Verify(
            queuedCommand,
            clientId,
            installationId);

        if (!verification.IsSuccess)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Rejected,
                verification.Detail ?? "Command signature could not be verified.",
                new CommandFailurePayload(
                    verification.FailureCode ?? "CommandVerificationFailed",
                    verification.Detail ?? "Command signature could not be verified."),
                cancellationToken);
        }

        return queuedCommand.CommandType switch
        {
            LocalServerInstallationCommandTypes.RequestDiagnostics => await ProcessDiagnosticsAsync(
                clientId,
                installationId,
                localServerVersion,
                deploymentProfile,
                queuedCommand,
                cancellationToken),

            LocalServerInstallationCommandTypes.RefreshEntitlement => await ProcessEntitlementRefreshAsync(
                clientId,
                installationId,
                queuedCommand,
                cancellationToken),

            LocalServerInstallationCommandTypes.RevokeAppActivation => await ProcessAppActivationRevocationAsync(
                clientId,
                installationId,
                queuedCommand,
                cancellationToken),

            _ => await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Rejected,
                $"Command type '{queuedCommand.CommandType}' is not supported by this local server.",
                new CommandFailurePayload(
                    "CommandTypeUnsupported",
                    $"Command type '{queuedCommand.CommandType}' is not supported by this local server."),
                cancellationToken)
        };
    }

    private async Task<ProcessedInstallationCommandResult> ProcessDiagnosticsAsync(
        Guid clientId,
        string installationId,
        string localServerVersion,
        LocalServerDeploymentProfileResponse? deploymentProfile,
        InstallationCommandResponse queuedCommand,
        CancellationToken cancellationToken)
    {
        var payload = ReadSupportPayload(queuedCommand.Payload);
        var diagnosticsResult = await _diagnosticsBundleHandler.HandleAsync(
            new CreateLocalServerDiagnosticsBundleCommand(
                clientId,
                installationId,
                localServerVersion,
                GeneratedBy: "SafarSuite Local Server",
                payload.NormalizedReason,
                Environment.MachineName,
                RuntimeInformation.OSDescription,
                AsOfDate: null,
                DeploymentProfile: deploymentProfile),
            cancellationToken);

        if (!diagnosticsResult.IsSuccess)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Failed,
                diagnosticsResult.Detail ?? "Diagnostics bundle creation failed.",
                new CommandFailurePayload(
                    diagnosticsResult.FailureCode ?? "DiagnosticsBundleFailed",
                    diagnosticsResult.Detail ?? "Diagnostics bundle creation failed."),
                cancellationToken);
        }

        var uploadResult = await _diagnosticsUploadHandler.HandleAsync(
            new UploadDiagnosticsToControlCloudCommand(
                diagnosticsResult.Bundle!,
                UploadedBy: "SafarSuite Local Server",
                payload.NormalizedReason),
            cancellationToken);

        if (!uploadResult.IsSuccess)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Failed,
                uploadResult.Detail ?? "Diagnostics upload failed.",
                new CommandFailurePayload(
                    uploadResult.FailureCode ?? "DiagnosticsUploadFailed",
                    uploadResult.Detail ?? "Diagnostics upload failed."),
                cancellationToken);
        }

        return await AcknowledgeAsync(
            queuedCommand,
            LocalServerInstallationCommandAcknowledgementStatuses.Applied,
            "Diagnostics bundle exported and uploaded.",
            new DiagnosticsCommandAckPayload(
                uploadResult.Upload!.DiagnosticReportId,
                uploadResult.Upload.Status,
                uploadResult.Upload.ReceivedAtUtc),
            cancellationToken,
            diagnosticReportId: uploadResult.Upload.DiagnosticReportId);
    }

    private async Task<ProcessedInstallationCommandResult> ProcessEntitlementRefreshAsync(
        Guid clientId,
        string installationId,
        InstallationCommandResponse queuedCommand,
        CancellationToken cancellationToken)
    {
        var pullResult = await _entitlementPullHandler.HandleAsync(
            new PullEntitlementFromControlCloudCommand(
                clientId,
                installationId),
            cancellationToken);

        if (!pullResult.IsSuccess)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Failed,
                pullResult.Detail ?? "Entitlement refresh failed.",
                new CommandFailurePayload(
                    pullResult.FailureCode ?? "EntitlementRefreshFailed",
                    pullResult.Detail ?? "Entitlement refresh failed."),
                cancellationToken);
        }

        return await AcknowledgeAsync(
            queuedCommand,
            LocalServerInstallationCommandAcknowledgementStatuses.Applied,
            $"Entitlement version {pullResult.Entitlement!.EntitlementVersion} pulled and imported.",
            new EntitlementRefreshCommandAckPayload(
                pullResult.Entitlement.EntitlementVersion,
                pullResult.Entitlement.BundleIssueId,
                pullResult.Entitlement.PaidUntil,
                pullResult.Entitlement.OfflineValidUntil,
                pullResult.PulledAtUtc!.Value),
            cancellationToken,
            entitlementVersion: pullResult.Entitlement.EntitlementVersion);
    }

    private async Task<ProcessedInstallationCommandResult> ProcessAppActivationRevocationAsync(
        Guid clientId,
        string installationId,
        InstallationCommandResponse queuedCommand,
        CancellationToken cancellationToken)
    {
        AppActivationRevocationCommandPayload payload;

        try
        {
            payload = queuedCommand.Payload.Deserialize<AppActivationRevocationCommandPayload>(JsonOptions)
                ?? throw new JsonException("Command payload was empty.");
        }
        catch (JsonException exception)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Rejected,
                $"App activation revocation payload is invalid: {exception.Message}",
                new CommandFailurePayload(
                    "AppActivationRevocationPayloadInvalid",
                    $"App activation revocation payload is invalid: {exception.Message}"),
                cancellationToken);
        }

        var validationFailure = ValidateAppActivationRevocationPayload(
            payload,
            clientId,
            installationId);

        if (validationFailure is not null)
        {
            return await AcknowledgeAsync(
                queuedCommand,
                LocalServerInstallationCommandAcknowledgementStatuses.Rejected,
                validationFailure.Detail,
                new CommandFailurePayload(
                    validationFailure.FailureCode,
                    validationFailure.Detail),
                cancellationToken);
        }

        var recordedAtUtc = _clock.UtcNow;
        var record = new LocalServerAppActivationRevocationRecord(
            payload.ActivationIssueId,
            payload.ClientId,
            payload.InstallationId.Trim(),
            payload.AppServerInstallationId,
            payload.ActivationRequestId,
            payload.FingerprintHash.Trim(),
            payload.ServerPublicKeySha256.Trim(),
            payload.SigningKeyId.Trim(),
            payload.RevokedAtUtc,
            payload.RevokedBy.Trim(),
            payload.Reason.Trim(),
            queuedCommand.CommandId,
            queuedCommand.CommandVersion,
            recordedAtUtc);

        await _appActivationRevocations.SaveAsync(record, cancellationToken);

        return await AcknowledgeAsync(
            queuedCommand,
            LocalServerInstallationCommandAcknowledgementStatuses.Applied,
            $"App activation issue {payload.ActivationIssueId} revocation recorded for app server {payload.AppServerInstallationId}.",
            new AppActivationRevocationCommandAckPayload(
                payload.ActivationIssueId,
                payload.AppServerInstallationId,
                payload.RevokedAtUtc,
                recordedAtUtc,
                "Recorded"),
            cancellationToken);
    }

    private async Task<ProcessedInstallationCommandResult> AcknowledgeAsync(
        InstallationCommandResponse queuedCommand,
        string resultStatus,
        string detail,
        object payload,
        CancellationToken cancellationToken,
        Guid? diagnosticReportId = null,
        long? entitlementVersion = null)
    {
        var acknowledgement = await _commandClient.AcknowledgeAsync(
            queuedCommand.InstallationId,
            queuedCommand.CommandId,
            new AcknowledgeInstallationCommandRequest(
                resultStatus,
                detail,
                JsonSerializer.SerializeToElement(payload, JsonOptions)),
            cancellationToken);

        if (!acknowledgement.IsSuccess)
        {
            return new ProcessedInstallationCommandResult(
                queuedCommand.CommandId,
                queuedCommand.CommandVersion,
                queuedCommand.CommandType,
                LocalServerInstallationCommandAcknowledgementStatuses.Failed,
                Acknowledged: false,
                acknowledgement.FailureCode ?? "CommandAcknowledgementFailed",
                acknowledgement.Detail ?? "Control Cloud did not accept the command acknowledgement.");
        }

        return new ProcessedInstallationCommandResult(
            queuedCommand.CommandId,
            queuedCommand.CommandVersion,
            queuedCommand.CommandType,
            resultStatus,
            Acknowledged: true,
            FailureCode: resultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Applied
                ? null
                : GetFailureCode(payload),
            detail,
            diagnosticReportId,
            entitlementVersion);
    }

    private static SupportCommandPayload ReadSupportPayload(JsonElement payload)
    {
        try
        {
            return payload.Deserialize<SupportCommandPayload>(JsonOptions)
                ?? SupportCommandPayload.Empty;
        }
        catch (JsonException)
        {
            return SupportCommandPayload.Empty;
        }
    }

    private static string? GetFailureCode(object payload)
    {
        return payload is CommandFailurePayload failure ? failure.FailureCode : null;
    }

    private static CommandFailurePayload? ValidateAppActivationRevocationPayload(
        AppActivationRevocationCommandPayload payload,
        Guid clientId,
        string installationId)
    {
        if (payload.PayloadFormatVersion != "safarsuite-app-activation-revocation-command-v1")
        {
            return new CommandFailurePayload(
                "AppActivationRevocationPayloadVersionUnsupported",
                "App activation revocation payload version is not supported.");
        }

        if (payload.CommandType != LocalServerInstallationCommandTypes.RevokeAppActivation)
        {
            return new CommandFailurePayload(
                "AppActivationRevocationCommandTypeMismatch",
                "App activation revocation payload command type does not match the signed command type.");
        }

        if (payload.ActivationIssueId == Guid.Empty
            || payload.ClientId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.InstallationId)
            || payload.AppServerInstallationId == Guid.Empty
            || payload.ActivationRequestId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.FingerprintHash)
            || string.IsNullOrWhiteSpace(payload.ServerPublicKeySha256)
            || string.IsNullOrWhiteSpace(payload.SigningKeyId)
            || string.IsNullOrWhiteSpace(payload.RevokedBy)
            || string.IsNullOrWhiteSpace(payload.Reason))
        {
            return new CommandFailurePayload(
                "AppActivationRevocationPayloadIncomplete",
                "App activation revocation payload is missing required identity or revocation fields.");
        }

        if (payload.ClientId != clientId)
        {
            return new CommandFailurePayload(
                "AppActivationRevocationClientMismatch",
                "App activation revocation payload belongs to another client.");
        }

        if (!payload.InstallationId.Trim().Equals(installationId, StringComparison.Ordinal))
        {
            return new CommandFailurePayload(
                "AppActivationRevocationInstallationMismatch",
                "App activation revocation payload belongs to another installation.");
        }

        return null;
    }

    private sealed record SupportCommandPayload(
        string? RequestedBy,
        string? Reason)
    {
        public static SupportCommandPayload Empty { get; } = new(null, null);

        public string NormalizedReason =>
            string.IsNullOrWhiteSpace(Reason)
                ? "Support command requested from Control Cloud."
                : Reason.Trim();
    }

    private sealed record CommandFailurePayload(
        string FailureCode,
        string Detail);

    private sealed record DiagnosticsCommandAckPayload(
        Guid DiagnosticReportId,
        string UploadStatus,
        DateTimeOffset ReceivedAtUtc);

    private sealed record EntitlementRefreshCommandAckPayload(
        long EntitlementVersion,
        Guid BundleIssueId,
        DateOnly PaidUntil,
        DateOnly OfflineValidUntil,
        DateTimeOffset PulledAtUtc);

    private sealed record AppActivationRevocationCommandPayload(
        string PayloadFormatVersion,
        string CommandType,
        Guid ClientId,
        string InstallationId,
        Guid AppServerInstallationId,
        Guid ActivationIssueId,
        Guid ActivationRequestId,
        string FingerprintHash,
        string ServerPublicKeySha256,
        string SigningKeyId,
        DateTimeOffset RevokedAtUtc,
        string RevokedBy,
        string Reason);

    private sealed record AppActivationRevocationCommandAckPayload(
        Guid ActivationIssueId,
        Guid AppServerInstallationId,
        DateTimeOffset RevokedAtUtc,
        DateTimeOffset RecordedAtUtc,
        string RevocationStatus);
}
