using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.ReceiveInstallationDiagnostics;

public sealed class ReceiveInstallationDiagnosticsHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IControlCloudClientInstallationRepository _installations;
    private readonly IControlCloudInstallationDiagnosticReportRepository _reports;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public ReceiveInstallationDiagnosticsHandler(
        IControlCloudClientInstallationRepository installations,
        IControlCloudInstallationDiagnosticReportRepository reports,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _installations = installations;
        _reports = reports;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ReceiveInstallationDiagnosticsResult> HandleAsync(
        ReceiveInstallationDiagnosticsCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(command.InstallationId, 160);

        if (command.ClientId == Guid.Empty)
        {
            return ReceiveInstallationDiagnosticsResult.Failure(
                "ClientIdRequired",
                "Client id is required before receiving diagnostics.");
        }

        if (installationId is null)
        {
            return ReceiveInstallationDiagnosticsResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before receiving diagnostics.");
        }

        if (command.Bundle.ClientId != command.ClientId)
        {
            return ReceiveInstallationDiagnosticsResult.Failure(
                "DiagnosticsClientMismatch",
                "Diagnostics bundle belongs to another client.");
        }

        if (!string.Equals(command.Bundle.InstallationId, installationId, StringComparison.Ordinal))
        {
            return ReceiveInstallationDiagnosticsResult.Failure(
                "DiagnosticsInstallationMismatch",
                "Diagnostics bundle belongs to another installation.");
        }

        if (!string.Equals(
                command.Bundle.FormatVersion,
                ControlCloudLocalServerDiagnosticsBundleFormat.Version,
                StringComparison.Ordinal))
        {
            return ReceiveInstallationDiagnosticsResult.Failure(
                "DiagnosticsFormatUnsupported",
                "Diagnostics bundle format is not supported.");
        }

        var receivedAtUtc = _clock.UtcNow;
        var report = new ControlCloudInstallationDiagnosticReport(
            Guid.NewGuid(),
            command.ClientId,
            installationId,
            ControlCloudInstallationDiagnosticReportStatuses.Received,
            receivedAtUtc,
            command.Bundle.GeneratedAtUtc,
            NormalizeOptionalText(command.UploadedBy, 120) ?? "SafarSuite Local Server",
            NormalizeOptionalText(command.Reason, 500) ?? command.Bundle.Reason,
            NormalizeOptionalText(command.Bundle.LocalServerVersion, 80) ?? "Unknown",
            NormalizeOptionalText(command.Bundle.LicenseStatus, 32) ?? "Unknown",
            JsonSerializer.Serialize(command.Bundle, JsonOptions));

        var result = await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var installation = await _installations.GetByInstallationIdAsync(
                    installationId,
                    token);

                if (installation is null)
                {
                    return ReceiveInstallationDiagnosticsResult.Failure(
                        "InstallationNotFound",
                        "Installation is not registered.");
                }

                if (installation.ClientId != command.ClientId)
                {
                    return ReceiveInstallationDiagnosticsResult.Failure(
                        "InstallationClientMismatch",
                        "Installation id is already bound to another client.");
                }

                await _reports.AddAsync(report, token);

                return ReceiveInstallationDiagnosticsResult.Success(report);
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await ControlCloudAuditWriter.TryRecordAsync(
                _audit,
                new ClientPortalAuditRecord(
                    Guid.NewGuid(),
                    report.ClientId,
                    InvitationId: null,
                    UserId: null,
                    SubjectEmail: "",
                    ClientPortalAuditEventTypes.LocalServerDiagnosticsUploaded,
                    report.UploadedBy,
                    $"Diagnostics report '{report.DiagnosticReportId}' received for installation '{report.InstallationId}' with license status '{report.LicenseStatus}'.",
                    receivedAtUtc),
                cancellationToken);
        }

        return result;
    }

    private static string? NormalizeRequiredText(string? value, int maxLength)
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

    private static string? NormalizeOptionalText(string? value, int maxLength)
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
