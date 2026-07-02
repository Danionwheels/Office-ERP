using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;

public sealed class QueueInstallationCommandHandler
{
    private static readonly TimeSpan DefaultCommandLifetime = TimeSpan.FromDays(7);

    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudInstallationCommandSigner _signer;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public QueueInstallationCommandHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationCommandRepository commands,
        IControlCloudInstallationCommandSigner signer,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _installations = installations;
        _commands = commands;
        _signer = signer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<QueueInstallationCommandResult> HandleAsync(
        QueueInstallationCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (installationId is null)
        {
            return QueueInstallationCommandResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before queueing a local-server command.");
        }

        var commandType = NormalizeText(command.CommandType);

        if (commandType is null)
        {
            return QueueInstallationCommandResult.Failure(
                "CommandTypeRequired",
                "Command type is required.");
        }

        if (!IsValidPayloadJson(command.PayloadJson))
        {
            return QueueInstallationCommandResult.Failure(
                "CommandPayloadInvalid",
                "Command payload must be valid JSON.");
        }

        var queuedAtUtc = _clock.UtcNow;
        var expiresAtUtc = command.ExpiresAtUtc ?? queuedAtUtc.Add(DefaultCommandLifetime);

        if (expiresAtUtc <= queuedAtUtc)
        {
            return QueueInstallationCommandResult.Failure(
                "CommandExpiryInvalid",
                "Command expiry must be after the queued time.");
        }

        var idempotencyKey = NormalizeText(command.IdempotencyKey)
            ?? $"command:{installationId}:{Guid.NewGuid():N}";

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var existingCommand = await _commands.GetByIdempotencyKeyAsync(
                        idempotencyKey,
                        token);

                    if (existingCommand is not null)
                    {
                        return QueueInstallationCommandResult.Success(existingCommand);
                    }

                    var installation = await _installations.GetByInstallationIdAsync(
                        installationId,
                        token);

                    if (installation is null)
                    {
                        return QueueInstallationCommandResult.Failure(
                            "InstallationNotFound",
                            "Installation must be registered before commands can be queued.");
                    }

                    if (installation.ClientId != command.ClientId)
                    {
                        return QueueInstallationCommandResult.Failure(
                            "InstallationClientMismatch",
                            "Installation id is already bound to another client.");
                    }

                    var latestVersion = await _commands.GetLatestCommandVersionAsync(
                        installationId,
                        token);
                    var commandId = Guid.NewGuid();
                    var commandVersion = latestVersion + 1;
                    var payloadJson = NormalizePayloadJson(command.PayloadJson);
                    var signature = _signer.Sign(
                        new ControlCloudInstallationCommandSigningPayload(
                            commandId,
                            command.ClientId,
                            installationId,
                            commandVersion,
                            commandType,
                            payloadJson,
                            queuedAtUtc,
                            command.NotBeforeUtc,
                            expiresAtUtc));
                    var queuedCommand = ControlCloudInstallationCommand.Queue(
                        commandId,
                        command.ClientId,
                        installationId,
                        commandVersion,
                        commandType,
                        idempotencyKey,
                        payloadJson,
                        signature,
                        queuedAtUtc,
                        command.NotBeforeUtc,
                        expiresAtUtc);

                    await _commands.AddAsync(queuedCommand, token);

                    return QueueInstallationCommandResult.Success(queuedCommand);
                },
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return QueueInstallationCommandResult.Failure(
                "CommandInvalid",
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return QueueInstallationCommandResult.Failure(
                "CommandInvalid",
                exception.Message);
        }
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsValidPayloadJson(string payloadJson)
    {
        try
        {
            using var _ = JsonDocument.Parse(NormalizePayloadJson(payloadJson));

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizePayloadJson(string payloadJson)
    {
        return string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson.Trim();
    }
}
