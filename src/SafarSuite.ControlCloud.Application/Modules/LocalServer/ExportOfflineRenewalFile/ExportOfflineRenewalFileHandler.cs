using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;

public sealed class ExportOfflineRenewalFileHandler
{
    private readonly GetClientPortalSignedEntitlementBundleHandler _bundleHandler;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudClock _clock;

    public ExportOfflineRenewalFileHandler(
        GetClientPortalSignedEntitlementBundleHandler bundleHandler,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock)
    {
        _bundleHandler = bundleHandler;
        _audit = audit;
        _clock = clock;
    }

    public async Task<ExportOfflineRenewalFileResult> HandleAsync(
        ExportOfflineRenewalFileQuery query,
        CancellationToken cancellationToken = default)
    {
        var installationId = query.InstallationId.Trim();

        if (query.ClientId == Guid.Empty)
        {
            return ExportOfflineRenewalFileResult.Failure(
                "ClientIdRequired",
                "Client id is required before exporting an offline renewal file.");
        }

        if (installationId.Length == 0)
        {
            return ExportOfflineRenewalFileResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before exporting an offline renewal file.");
        }

        var bundle = await _bundleHandler.HandleAsync(
            new GetClientPortalSignedEntitlementBundleQuery(
                query.ClientId,
                installationId),
            cancellationToken);

        if (!bundle.IsSuccess)
        {
            return ExportOfflineRenewalFileResult.Failure(
                bundle.FailureCode ?? "OfflineRenewalExportFailed",
                bundle.Detail ?? "Offline renewal file could not be exported.");
        }

        var generatedAtUtc = _clock.UtcNow;
        var renewalFile = new ControlCloudOfflineRenewalFileResponse(
            ControlCloudOfflineRenewalFileFormat.Version,
            Guid.NewGuid(),
            query.ClientId,
            installationId,
            generatedAtUtc,
            NormalizeGeneratedBy(query.GeneratedBy),
            NormalizeReason(query.Reason),
            ControlCloudEntitlementBundleContractMapper.ToResponse(bundle.Bundle!));

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                query.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                ClientPortalAuditEventTypes.OfflineRenewalFileGenerated,
                renewalFile.GeneratedBy,
                $"Offline renewal file generated for installation '{installationId}'. Reason: {renewalFile.Reason}",
                generatedAtUtc),
            cancellationToken);

        return ExportOfflineRenewalFileResult.Success(renewalFile);
    }

    private static string NormalizeGeneratedBy(string generatedBy)
    {
        return string.IsNullOrWhiteSpace(generatedBy)
            ? ClientPortalAuditActors.ControlDesk
            : generatedBy.Trim();
    }

    private static string NormalizeReason(string reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? "Offline renewal fallback"
            : reason.Trim();
    }
}
