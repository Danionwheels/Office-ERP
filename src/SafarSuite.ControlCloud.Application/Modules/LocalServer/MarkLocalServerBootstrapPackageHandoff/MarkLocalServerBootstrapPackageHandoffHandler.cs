using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;

public sealed class MarkLocalServerBootstrapPackageHandoffHandler
{
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudClock _clock;
    private readonly IControlCloudInstallationSetupTokenRepository _setupTokens;

    public MarkLocalServerBootstrapPackageHandoffHandler(
        IControlCloudInstallationSetupTokenRepository setupTokens,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock)
    {
        _setupTokens = setupTokens;
        _audit = audit;
        _clock = clock;
    }

    public async Task<MarkLocalServerBootstrapPackageHandoffResult> HandleAsync(
        MarkLocalServerBootstrapPackageHandoffCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeOptionalText(command.InstallationId, 160);
        var channel = NormalizeOptionalText(command.Channel, 40);
        var recipient = NormalizeOptionalText(command.Recipient, 160);
        var markedBy = NormalizeOptionalText(command.MarkedBy, 120);
        var note = NormalizeOptionalText(command.Note, 500);

        if (command.ClientId == Guid.Empty)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "ClientIdRequired",
                "Client id is required before marking bootstrap package handoff.");
        }

        if (installationId is null)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before marking bootstrap package handoff.");
        }

        if (command.BootstrapPackageId == Guid.Empty)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "BootstrapPackageIdRequired",
                "Bootstrap package id is required before marking handoff.");
        }

        if (channel is null)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "HandoffChannelRequired",
                "Handoff channel is required.");
        }

        if (markedBy is null)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "HandoffMarkedByRequired",
                "Marked by is required.");
        }

        var setupToken = await _setupTokens.GetBootstrapPackageAsync(
            command.ClientId,
            installationId,
            command.BootstrapPackageId,
            cancellationToken);

        if (setupToken is null || setupToken.BootstrapPackageId is null)
        {
            return MarkLocalServerBootstrapPackageHandoffResult.Failure(
                "BootstrapPackageNotFound",
                "Bootstrap package was not found for this installation.");
        }

        var markedAtUtc = _clock.UtcNow;
        var response = new LocalServerBootstrapPackageHandoffResponse(
            BootstrapPackageId: setupToken.BootstrapPackageId.Value,
            SetupTokenId: setupToken.SetupTokenId,
            ClientId: setupToken.ClientId,
            InstallationId: setupToken.InstallationId,
            HandoffStatus: "HandedOff",
            Channel: channel,
            Recipient: recipient ?? "",
            MarkedBy: markedBy,
            Note: note,
            MarkedAtUtc: markedAtUtc);

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                setupToken.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                ClientPortalAuditEventTypes.BootstrapPackageHandedOff,
                ControlCloudAuditWriter.NormalizeActor(markedBy, ClientPortalAuditActors.ControlDesk),
                BuildAuditDetail(response),
                markedAtUtc),
            cancellationToken);

        return MarkLocalServerBootstrapPackageHandoffResult.Success(response);
    }

    private static string BuildAuditDetail(
        LocalServerBootstrapPackageHandoffResponse handoff)
    {
        var recipient = string.IsNullOrWhiteSpace(handoff.Recipient)
            ? "unspecified recipient"
            : handoff.Recipient;
        var detail =
            $"Bootstrap package '{handoff.BootstrapPackageId}' handed off for installation '{handoff.InstallationId}' via '{handoff.Channel}' to '{recipient}'.";

        return string.IsNullOrWhiteSpace(handoff.Note)
            ? detail
            : $"{detail} Note: {handoff.Note}";
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
