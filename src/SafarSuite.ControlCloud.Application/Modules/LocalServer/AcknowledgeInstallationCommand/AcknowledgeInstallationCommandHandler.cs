using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;

public sealed class AcknowledgeInstallationCommandHandler
{
    private static readonly HashSet<string> AllowedResultStatuses =
    [
        ControlCloudInstallationCommandAcknowledgementStatuses.Applied,
        ControlCloudInstallationCommandAcknowledgementStatuses.Failed,
        ControlCloudInstallationCommandAcknowledgementStatuses.Rejected
    ];

    private readonly IControlCloudInstallationCommandRepository _commands;
    private readonly IControlCloudInstallationCommandAcknowledgementRepository _acknowledgements;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public AcknowledgeInstallationCommandHandler(
        IControlCloudInstallationCommandRepository commands,
        IControlCloudInstallationCommandAcknowledgementRepository acknowledgements,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _commands = commands;
        _acknowledgements = acknowledgements;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AcknowledgeInstallationCommandResult> HandleAsync(
        AcknowledgeInstallationCommandCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeText(command.InstallationId);

        if (installationId is null)
        {
            return AcknowledgeInstallationCommandResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before acknowledging a command.");
        }

        var resultStatus = NormalizeText(command.ResultStatus);

        if (resultStatus is null || !AllowedResultStatuses.Contains(resultStatus))
        {
            return AcknowledgeInstallationCommandResult.Failure(
                "AcknowledgementStatusInvalid",
                "Acknowledgement status must be Applied, Failed, or Rejected.");
        }

        if (!IsValidPayloadJson(command.PayloadJson))
        {
            return AcknowledgeInstallationCommandResult.Failure(
                "AcknowledgementPayloadInvalid",
                "Acknowledgement payload must be valid JSON.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var queuedCommand = await _commands.GetByCommandIdAsync(
                    command.CommandId,
                    token);

                if (queuedCommand is null)
                {
                    return AcknowledgeInstallationCommandResult.Failure(
                        "CommandNotFound",
                        "Command was not found.");
                }

                if (queuedCommand.InstallationId != installationId)
                {
                    return AcknowledgeInstallationCommandResult.Failure(
                        "InstallationCommandMismatch",
                        "Command does not belong to this installation.");
                }

                if (!queuedCommand.IsPending)
                {
                    return AcknowledgeInstallationCommandResult.Success(queuedCommand);
                }

                var acknowledgedAtUtc = _clock.UtcNow;
                var acknowledgement = new ControlCloudInstallationCommandAcknowledgement(
                    Guid.NewGuid(),
                    queuedCommand.CommandId,
                    queuedCommand.ClientId,
                    queuedCommand.InstallationId,
                    queuedCommand.CommandVersion,
                    resultStatus,
                    command.Detail,
                    NormalizePayloadJson(command.PayloadJson),
                    acknowledgedAtUtc);

                queuedCommand.Acknowledge(
                    resultStatus,
                    command.Detail,
                    acknowledgedAtUtc);

                await _commands.SaveAsync(queuedCommand, token);
                await _acknowledgements.AddAsync(acknowledgement, token);

                return AcknowledgeInstallationCommandResult.Success(queuedCommand);
            },
            cancellationToken);
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
