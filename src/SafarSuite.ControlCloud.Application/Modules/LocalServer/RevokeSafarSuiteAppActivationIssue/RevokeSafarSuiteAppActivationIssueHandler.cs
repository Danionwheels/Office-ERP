using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.RevokeSafarSuiteAppActivationIssue;

public sealed class RevokeSafarSuiteAppActivationIssueHandler
{
    private const string AppActivationRevocationCommandType = "revoke_app_activation";
    private const string AppActivationRevocationPayloadVersion = "safarsuite-app-activation-revocation-command-v1";
    private static readonly TimeSpan RevocationCommandLifetime = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IControlCloudAppActivationIssueRepository _activationIssues;
    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudInstallationCommandSigner _signer;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public RevokeSafarSuiteAppActivationIssueHandler(
        IControlCloudAppActivationIssueRepository activationIssues,
        IControlCloudInstallationCommandRepository commands,
        IControlCloudInstallationCommandSigner signer,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _activationIssues = activationIssues;
        _commands = commands;
        _signer = signer;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RevokeSafarSuiteAppActivationIssueResult> HandleAsync(
        RevokeSafarSuiteAppActivationIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ClientIdRequired",
                "Client id is required before revoking an app activation issue.");
        }

        if (command.ActivationIssueId == Guid.Empty)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueIdRequired",
                "Activation issue id is required before revoking an app activation issue.");
        }

        var revokedBy = NormalizeText(command.RevokedBy);
        var reason = NormalizeText(command.Reason);

        if (revokedBy is null)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueRevokedByRequired",
                "Revoked-by is required before revoking an app activation issue.");
        }

        if (reason is null)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueRevocationReasonRequired",
                "Revocation reason is required before revoking an app activation issue.");
        }

        var issue = await _activationIssues.GetByIdAsync(
            command.ActivationIssueId,
            cancellationToken);

        if (issue is null)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueNotFound",
                "App activation issue was not found.");
        }

        if (issue.ClientId != command.ClientId)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueClientMismatch",
                "App activation issue belongs to another client.");
        }

        if (issue.Status == ControlCloudAppActivationIssueStatuses.Revoked)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueAlreadyRevoked",
                "App activation issue is already revoked.");
        }

        var now = _clock.UtcNow;
        ControlCloudInstallationCommand? revocationCommand = null;

        try
        {
            revocationCommand = await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    issue.Revoke(revokedBy, reason, now);

                    var queuedCommand = await QueueRevocationCommandAsync(
                        issue,
                        revokedBy,
                        reason,
                        now,
                        token);

                    await _activationIssues.SaveAsync(issue, token);

                    return queuedCommand;
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueRevocationCommandInvalid",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return RevokeSafarSuiteAppActivationIssueResult.Failure(
                "ActivationIssueRevocationCommandInvalid",
                exception.Message);
        }

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                issue.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                ClientPortalAuditEventTypes.AppActivationTokenRevoked,
                ControlCloudAuditWriter.NormalizeActor(revokedBy, ClientPortalAuditActors.ControlCloud),
                $"App activation issue '{issue.ActivationIssueId}' revoked for installation '{issue.InstallationId}', app server '{issue.AppServerInstallationId}', signing key '{issue.SigningKeyId}', and revocation command '{revocationCommand?.CommandId}'. Reason: {reason}",
                now),
            cancellationToken);

        return RevokeSafarSuiteAppActivationIssueResult.Success(ToResponse(issue));
    }

    private static SafarSuiteAppActivationIssueResponse ToResponse(
        ControlCloudAppActivationIssue issue)
    {
        return new SafarSuiteAppActivationIssueResponse(
            issue.ActivationIssueId,
            issue.ClientId,
            issue.InstallationId,
            issue.AppServerInstallationId,
            issue.ActivationRequestId,
            issue.ReplacesActivationIssueId,
            issue.FingerprintHash,
            issue.ServerPublicKeySha256,
            issue.EntitlementVersion,
            issue.SigningKeyId,
            issue.Status,
            issue.RequestedBy,
            issue.IssuedAtUtc,
            issue.ExpiresAtUtc,
            issue.RevokedAtUtc,
            issue.RevokedBy,
            issue.RevocationReason);
    }

    private async Task<ControlCloudInstallationCommand> QueueRevocationCommandAsync(
        ControlCloudAppActivationIssue issue,
        string revokedBy,
        string reason,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = BuildRevocationCommandIdempotencyKey(issue);
        var existingCommand = await _commands.GetByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);

        if (existingCommand is not null)
        {
            return existingCommand;
        }

        var latestVersion = await _commands.GetLatestCommandVersionAsync(
            issue.InstallationId,
            cancellationToken);
        var commandId = Guid.NewGuid();
        var commandVersion = latestVersion + 1;
        var payloadJson = JsonSerializer.Serialize(
            new AppActivationRevocationCommandPayload(
                AppActivationRevocationPayloadVersion,
                AppActivationRevocationCommandType,
                issue.ClientId,
                issue.InstallationId,
                issue.AppServerInstallationId,
                issue.ActivationIssueId,
                issue.ActivationRequestId,
                issue.FingerprintHash,
                issue.ServerPublicKeySha256,
                issue.SigningKeyId,
                revokedAtUtc,
                revokedBy,
                reason),
            JsonOptions);
        var expiresAtUtc = revokedAtUtc.Add(RevocationCommandLifetime);
        var signature = _signer.Sign(
            new ControlCloudInstallationCommandSigningPayload(
                commandId,
                issue.ClientId,
                issue.InstallationId,
                commandVersion,
                AppActivationRevocationCommandType,
                payloadJson,
                revokedAtUtc,
                NotBeforeUtc: null,
                expiresAtUtc));
        var command = ControlCloudInstallationCommand.Queue(
            commandId,
            issue.ClientId,
            issue.InstallationId,
            commandVersion,
            AppActivationRevocationCommandType,
            idempotencyKey,
            payloadJson,
            signature,
            revokedAtUtc,
            notBeforeUtc: null,
            expiresAtUtc);

        await _commands.AddAsync(command, cancellationToken);

        return command;
    }

    private static string BuildRevocationCommandIdempotencyKey(
        ControlCloudAppActivationIssue issue)
    {
        return string.Concat(
            "app-activation-revoke:",
            issue.ClientId.ToString("N"),
            ":",
            issue.InstallationId,
            ":",
            issue.ActivationIssueId.ToString("N"));
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

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
}
