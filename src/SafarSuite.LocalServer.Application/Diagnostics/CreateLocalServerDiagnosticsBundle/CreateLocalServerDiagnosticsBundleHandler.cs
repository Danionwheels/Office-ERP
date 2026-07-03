using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Diagnostics.Ports;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Domain.Entitlements;
using System.Runtime.InteropServices;

namespace SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;

public sealed class CreateLocalServerDiagnosticsBundleHandler
{
    private readonly ILocalServerEntitlementCache _cache;
    private readonly ILocalServerEntitlementTrustStateStore _trustStateStore;
    private readonly LocalServerEntitlementPolicy _policy;
    private readonly ILocalServerClock _clock;
    private readonly ILocalServerEntitlementImportAuditStore? _importAuditStore;
    private readonly ILocalServerRuntimeDiagnosticsCollector? _runtimeDiagnosticsCollector;

    public CreateLocalServerDiagnosticsBundleHandler(
        ILocalServerEntitlementCache cache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        LocalServerEntitlementPolicy policy,
        ILocalServerClock clock,
        ILocalServerEntitlementImportAuditStore? importAuditStore = null,
        ILocalServerRuntimeDiagnosticsCollector? runtimeDiagnosticsCollector = null)
    {
        _cache = cache;
        _trustStateStore = trustStateStore;
        _policy = policy;
        _clock = clock;
        _importAuditStore = importAuditStore;
        _runtimeDiagnosticsCollector = runtimeDiagnosticsCollector;
    }

    public async Task<CreateLocalServerDiagnosticsBundleResult> HandleAsync(
        CreateLocalServerDiagnosticsBundleCommand command,
        CancellationToken cancellationToken = default)
    {
        var installationId = NormalizeRequiredText(command.InstallationId, 160);

        if (command.ClientId == Guid.Empty)
        {
            return CreateLocalServerDiagnosticsBundleResult.Failure(
                "ClientIdRequired",
                "Client id is required before creating local-server diagnostics.");
        }

        if (installationId is null)
        {
            return CreateLocalServerDiagnosticsBundleResult.Failure(
                "InstallationIdRequired",
                "Installation id is required before creating local-server diagnostics.");
        }

        var generatedAtUtc = _clock.UtcNow;
        var entitlement = await _cache.GetCurrentAsync(cancellationToken);
        var trustState = await _trustStateStore.GetAsync(installationId, cancellationToken);
        var asOfDate = command.AsOfDate
            ?? DateOnly.FromDateTime(generatedAtUtc.UtcDateTime);
        var state = _policy.EvaluateEntitlementState(
            entitlement,
            installationId,
            asOfDate);
        var localServerVersion = NormalizeOptionalText(command.LocalServerVersion, 80) ?? "Unknown";
        var machineName = NormalizeOptionalText(command.MachineName, 160) ?? "Unknown";
        var operatingSystem = NormalizeOptionalText(command.OperatingSystem, 200) ?? "Unknown";
        var collectedRuntime = await CollectRuntimeDiagnosticsAsync(
            command,
            localServerVersion,
            machineName,
            operatingSystem,
            cancellationToken);
        var runtime = NormalizeRuntime(
            command.Runtime ?? collectedRuntime.Runtime,
            localServerVersion,
            machineName,
            operatingSystem);
        var bootstrap = NormalizeBootstrap(command.Bootstrap ?? collectedRuntime.Bootstrap);
        var services = NormalizeServices(command.Services ?? collectedRuntime.Services);
        var recentErrors = NormalizeRecentErrors(command.RecentErrors ?? collectedRuntime.RecentErrors);
        var importAudit = command.ImportAudit is not null
            ? NormalizeImportAudit(command.ImportAudit)
            : await LoadRecentImportAuditAsync(installationId, cancellationToken);
        var diagnostics = new LocalServerDiagnosticBundleResponse(
            ControlCloudLocalServerDiagnosticsBundleFormat.Version,
            Guid.NewGuid(),
            command.ClientId,
            installationId,
            generatedAtUtc,
            NormalizeOptionalText(command.GeneratedBy, 120) ?? "SafarSuite Local Server",
            NormalizeOptionalText(command.Reason, 500) ?? "Support diagnostics",
            localServerVersion,
            machineName,
            operatingSystem,
            state.AccessState,
            ToEntitlementResponse(entitlement),
            ToTrustStateResponse(trustState),
            BuildChecks(
                installationId,
                entitlement,
                trustState,
                state,
                runtime,
                bootstrap,
                services,
                recentErrors,
                importAudit),
            runtime,
            bootstrap,
            services,
            recentErrors,
            importAudit,
            command.DeploymentProfile);

        return CreateLocalServerDiagnosticsBundleResult.Success(diagnostics);
    }

    private async Task<LocalServerRuntimeDiagnosticsSnapshot> CollectRuntimeDiagnosticsAsync(
        CreateLocalServerDiagnosticsBundleCommand command,
        string localServerVersion,
        string machineName,
        string operatingSystem,
        CancellationToken cancellationToken)
    {
        if (_runtimeDiagnosticsCollector is null
            || (command.Runtime is not null
                && command.Bootstrap is not null
                && command.Services is not null
                && command.RecentErrors is not null))
        {
            return LocalServerRuntimeDiagnosticsSnapshot.Empty;
        }

        try
        {
            return await _runtimeDiagnosticsCollector.CollectAsync(
                new LocalServerRuntimeDiagnosticsContext(
                    command.ClientId,
                    command.InstallationId,
                    localServerVersion,
                    machineName,
                    operatingSystem,
                    command.DeploymentProfile),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return LocalServerRuntimeDiagnosticsSnapshot.Empty;
        }
    }

    private static LocalServerDiagnosticEntitlementResponse ToEntitlementResponse(
        LocalServerCachedEntitlement? entitlement)
    {
        if (entitlement is null)
        {
            return new LocalServerDiagnosticEntitlementResponse(
                HasCachedEntitlement: false,
                BundleVersion: null,
                BundleIssueId: null,
                EntitlementVersion: null,
                Status: null,
                BundleIssuedAtUtc: null,
                ImportedAtUtc: null,
                ValidFrom: null,
                PaidUntil: null,
                WarningStartsAt: null,
                GraceUntil: null,
                OfflineValidUntil: null,
                AllowedDevices: null,
                AllowedBranches: null,
                SignatureKeyId: null,
                PayloadSha256: null,
                Modules: Array.Empty<LocalServerDiagnosticModuleResponse>());
        }

        return new LocalServerDiagnosticEntitlementResponse(
            HasCachedEntitlement: true,
            entitlement.BundleVersion,
            entitlement.BundleIssueId,
            entitlement.EntitlementVersion,
            entitlement.Status,
            entitlement.BundleIssuedAtUtc,
            entitlement.ImportedAtUtc,
            entitlement.ValidFrom,
            entitlement.PaidUntil,
            entitlement.WarningStartsAt,
            entitlement.GraceUntil,
            entitlement.OfflineValidUntil,
            entitlement.AllowedDevices,
            entitlement.AllowedBranches,
            entitlement.SignatureKeyId,
            entitlement.PayloadSha256,
            entitlement.Modules
                .OrderBy(module => module.ModuleCode, StringComparer.OrdinalIgnoreCase)
                .Select(module => new LocalServerDiagnosticModuleResponse(
                    module.ModuleCode,
                    module.Status,
                    module.IsEnabled))
                .ToArray());
    }

    private static LocalServerDiagnosticTrustStateResponse? ToTrustStateResponse(
        LocalServerEntitlementTrustState? trustState)
    {
        return trustState is null
            ? null
            : new LocalServerDiagnosticTrustStateResponse(
                trustState.LastAcceptedEntitlementVersion,
                trustState.LastAcceptedBundleIssueId,
                trustState.LastAcceptedBundleIssuedAtUtc,
                trustState.LastAcceptedAtUtc,
                trustState.LastSuccessfulCloudTimeUtc,
                trustState.LastLocalCheckAtUtc,
                trustState.ClockMovedBackwards,
                trustState.ClockMovedBackwardsDetectedAtUtc,
                trustState.LastClockWarning,
                trustState.LastReplayWarning,
                trustState.LastReplayWarningAtUtc,
                trustState.UpdatedAtUtc);
    }

    private static IReadOnlyCollection<LocalServerDiagnosticCheckResponse> BuildChecks(
        string installationId,
        LocalServerCachedEntitlement? entitlement,
        LocalServerEntitlementTrustState? trustState,
        LocalServerEntitlementStateDecision state,
        LocalServerDiagnosticRuntimeResponse runtime,
        LocalServerDiagnosticBootstrapResponse? bootstrap,
        IReadOnlyCollection<LocalServerDiagnosticServiceResponse> services,
        IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse> recentErrors,
        IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse> importAudit)
    {
        var checks = new List<LocalServerDiagnosticCheckResponse>
        {
            new(
                "cached-entitlement",
                entitlement is null ? "Warning" : "Ok",
                entitlement is null
                    ? "No signed entitlement bundle is cached."
                    : $"Cached entitlement version {entitlement.EntitlementVersion} is available."),
            new(
                "license-state",
                state.IsAllowed ? "Ok" : "Warning",
                $"{state.AccessState}: {state.Reason}"),
            new(
                "runtime-version",
                runtime.Version.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? "Warning" : "Ok",
                $"Local server version '{runtime.Version}' on '{runtime.RuntimeMode}' channel '{runtime.BuildChannel}'.")
        };

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "docker-availability",
            runtime.DockerAvailable switch
            {
                true => "Ok",
                false => "Warning",
                _ => "Info"
            },
            runtime.DockerAvailable switch
            {
                true => $"Docker is available{FormatVersion(runtime.DockerVersion)}.",
                false => "Docker is not available to the runtime diagnostics collector.",
                _ => "Docker availability has not been reported yet."
            }));

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "docker-compose-availability",
            runtime.DockerComposeAvailable switch
            {
                true => "Ok",
                false => "Warning",
                _ => "Info"
            },
            runtime.DockerComposeAvailable switch
            {
                true => $"Docker Compose is available{FormatVersion(runtime.DockerComposeVersion)}.",
                false => "Docker Compose is not available to the runtime diagnostics collector.",
                _ => "Docker Compose availability has not been reported yet."
            }));

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "bootstrap-config",
            bootstrap is null ? "Info" : ToBootstrapStatus(bootstrap),
            bootstrap is null
                ? "Bootstrap config facts have not been reported yet."
                : $"Bootstrap status '{bootstrap.BootstrapStatus}' in '{bootstrap.ConfigDirectory}'."));

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "service-status",
            ToServiceStatus(services),
            services.Count == 0
                ? "No local runtime service status has been reported yet."
                : BuildServiceDetail(services)));

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "recent-errors",
            recentErrors.Count == 0 ? "Ok" : "Warning",
            recentErrors.Count == 0
                ? "No recent runtime errors were reported."
                : $"{recentErrors.Count} recent runtime error(s) were reported."));

        checks.Add(new LocalServerDiagnosticCheckResponse(
            "import-audit",
            ToImportAuditStatus(importAudit),
            importAudit.Count == 0
                ? "No local entitlement import audit records were found."
                : BuildImportAuditDetail(importAudit)));

        if (entitlement is not null)
        {
            var installationMatches = string.Equals(
                entitlement.InstallationId,
                installationId,
                StringComparison.Ordinal);
            checks.Add(new LocalServerDiagnosticCheckResponse(
                "installation-binding",
                installationMatches ? "Ok" : "Failure",
                installationMatches
                    ? "Cached entitlement is bound to this installation."
                    : "Cached entitlement is bound to a different installation."));
        }

        if (trustState is null)
        {
            checks.Add(new LocalServerDiagnosticCheckResponse(
                "trust-state",
                "Warning",
                "No local entitlement trust state has been recorded yet."));
        }
        else
        {
            checks.Add(new LocalServerDiagnosticCheckResponse(
                "clock-rollback",
                trustState.ClockMovedBackwards ? "Warning" : "Ok",
                trustState.ClockMovedBackwards
                    ? trustState.LastClockWarning ?? "Local clock moved backwards."
                    : "No local clock rollback warning is recorded."));
            checks.Add(new LocalServerDiagnosticCheckResponse(
                "replay-warning",
                string.IsNullOrWhiteSpace(trustState.LastReplayWarning) ? "Ok" : "Warning",
                string.IsNullOrWhiteSpace(trustState.LastReplayWarning)
                    ? "No entitlement replay warning is recorded."
                    : trustState.LastReplayWarning));
        }

        return checks;
    }

    private static LocalServerDiagnosticRuntimeResponse NormalizeRuntime(
        LocalServerDiagnosticRuntimeResponse? runtime,
        string fallbackVersion,
        string fallbackMachineName,
        string fallbackOperatingSystem)
    {
        return runtime is null
            ? new LocalServerDiagnosticRuntimeResponse(
                fallbackVersion,
                BuildChannel: "Unknown",
                BuildCommit: "Unknown",
                RuntimeMode: "LocalServer",
                fallbackMachineName,
                fallbackOperatingSystem,
                RuntimeInformation.OSArchitecture.ToString(),
                Environment.ProcessorCount,
                DockerAvailable: null,
                DockerVersion: null,
                DockerComposeAvailable: null,
                DockerComposeVersion: null)
            : new LocalServerDiagnosticRuntimeResponse(
                NormalizeOptionalText(runtime.Version, 80) ?? fallbackVersion,
                NormalizeOptionalText(runtime.BuildChannel, 80) ?? "Unknown",
                NormalizeOptionalText(runtime.BuildCommit, 120) ?? "Unknown",
                NormalizeOptionalText(runtime.RuntimeMode, 80) ?? "LocalServer",
                NormalizeOptionalText(runtime.MachineName, 160) ?? fallbackMachineName,
                NormalizeOptionalText(runtime.OperatingSystem, 200) ?? fallbackOperatingSystem,
                NormalizeOptionalText(runtime.HostArchitecture, 80) ?? RuntimeInformation.OSArchitecture.ToString(),
                runtime.ProcessorCount > 0 ? runtime.ProcessorCount : Environment.ProcessorCount,
                runtime.DockerAvailable,
                NormalizeOptionalText(runtime.DockerVersion, 120),
                runtime.DockerComposeAvailable,
                NormalizeOptionalText(runtime.DockerComposeVersion, 120));
    }

    private static LocalServerDiagnosticBootstrapResponse? NormalizeBootstrap(
        LocalServerDiagnosticBootstrapResponse? bootstrap)
    {
        return bootstrap is null
            ? null
            : new LocalServerDiagnosticBootstrapResponse(
                NormalizeOptionalText(bootstrap.ConfigDirectory, 260) ?? "Unknown",
                NormalizeOptionalText(bootstrap.BootstrapStatus, 80) ?? "Unknown",
                NormalizeOptionalText(bootstrap.BootstrapConfigSha256, 128),
                NormalizeOptionalText(bootstrap.ComposeFileSha256, 128),
                NormalizeOptionalText(bootstrap.EnvironmentFileSha256, 128),
                bootstrap.LastRegistrationAttemptUtc,
                bootstrap.LastRegistrationSucceededAtUtc,
                bootstrap.LastHeartbeatSentAtUtc,
                bootstrap.LastEntitlementPullAtUtc);
    }

    private static IReadOnlyCollection<LocalServerDiagnosticServiceResponse> NormalizeServices(
        IReadOnlyCollection<LocalServerDiagnosticServiceResponse>? services)
    {
        return services is null
            ? Array.Empty<LocalServerDiagnosticServiceResponse>()
            : services
                .Where(service => !string.IsNullOrWhiteSpace(service.ServiceName))
                .Select(service => new LocalServerDiagnosticServiceResponse(
                    NormalizeOptionalText(service.ServiceName, 120) ?? "Unknown",
                    NormalizeOptionalText(service.ExpectedState, 80) ?? "Unknown",
                    NormalizeOptionalText(service.CurrentState, 80),
                    NormalizeOptionalText(service.ContainerName, 160),
                    service.LastStartedAtUtc,
                    NormalizeOptionalText(service.Detail, 500)))
                .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private async Task<IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse>> LoadRecentImportAuditAsync(
        string installationId,
        CancellationToken cancellationToken)
    {
        if (_importAuditStore is null)
        {
            return Array.Empty<LocalServerDiagnosticImportAuditResponse>();
        }

        try
        {
            var records = await _importAuditStore.GetRecentAsync(
                installationId,
                take: 20,
                cancellationToken);

            return NormalizeImportAudit(
                records.Select(record => new LocalServerDiagnosticImportAuditResponse(
                    record.AuditRecordId,
                    record.InstallationId,
                    record.ClientId,
                    record.ImportSource,
                    record.ResultStatus,
                    record.EntitlementVersion,
                    record.BundleIssueId,
                    record.FailureCode,
                    record.Detail,
                    record.PayloadSha256,
                    record.SignatureKeyId,
                    record.OccurredAtUtc))
                .ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return Array.Empty<LocalServerDiagnosticImportAuditResponse>();
        }
    }

    private static IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse> NormalizeImportAudit(
        IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse>? importAudit)
    {
        return importAudit is null
            ? Array.Empty<LocalServerDiagnosticImportAuditResponse>()
            : importAudit
                .Where(record => !string.IsNullOrWhiteSpace(record.InstallationId))
                .Select(record => new LocalServerDiagnosticImportAuditResponse(
                    record.AuditRecordId == Guid.Empty ? Guid.NewGuid() : record.AuditRecordId,
                    NormalizeOptionalText(record.InstallationId, 160) ?? "Unknown",
                    record.ClientId,
                    NormalizeOptionalText(record.ImportSource, 80) ?? "Unknown",
                    NormalizeOptionalText(record.ResultStatus, 40) ?? "Unknown",
                    record.EntitlementVersion,
                    record.BundleIssueId,
                    NormalizeOptionalText(record.FailureCode, 120),
                    NormalizeOptionalText(record.Detail, 500),
                    NormalizeOptionalText(record.PayloadSha256, 128),
                    NormalizeOptionalText(record.SignatureKeyId, 120),
                    record.OccurredAtUtc))
                .OrderByDescending(record => record.OccurredAtUtc)
                .Take(20)
                .ToArray();
    }

    private static IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse> NormalizeRecentErrors(
        IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse>? recentErrors)
    {
        return recentErrors is null
            ? Array.Empty<LocalServerDiagnosticRecentErrorResponse>()
            : recentErrors
                .Where(error => !string.IsNullOrWhiteSpace(error.Message))
                .Select(error => new LocalServerDiagnosticRecentErrorResponse(
                    NormalizeOptionalText(error.Source, 120) ?? "Runtime",
                    NormalizeOptionalText(error.Severity, 40) ?? "Error",
                    NormalizeOptionalText(error.Message, 500) ?? "Runtime error",
                    error.OccurredAtUtc))
                .OrderByDescending(error => error.OccurredAtUtc ?? DateTimeOffset.MinValue)
                .Take(20)
                .ToArray();
    }

    private static string ToBootstrapStatus(LocalServerDiagnosticBootstrapResponse bootstrap)
    {
        if (bootstrap.BootstrapStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase)
            || bootstrap.BootstrapStatus.Equals("Invalid", StringComparison.OrdinalIgnoreCase))
        {
            return "Failure";
        }

        if (string.IsNullOrWhiteSpace(bootstrap.BootstrapConfigSha256)
            || string.IsNullOrWhiteSpace(bootstrap.ComposeFileSha256)
            || string.IsNullOrWhiteSpace(bootstrap.EnvironmentFileSha256))
        {
            return "Warning";
        }

        return bootstrap.BootstrapStatus.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Info"
            : "Ok";
    }

    private static string ToServiceStatus(IReadOnlyCollection<LocalServerDiagnosticServiceResponse> services)
    {
        if (services.Count == 0)
        {
            return "Info";
        }

        if (services.Any(service => IsFailureState(service.CurrentState)))
        {
            return "Failure";
        }

        if (services.Any(service => string.IsNullOrWhiteSpace(service.CurrentState)
            || IsWarningState(service.CurrentState)))
        {
            return "Warning";
        }

        return "Ok";
    }

    private static string ToImportAuditStatus(
        IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse> importAudit)
    {
        if (importAudit.Count == 0)
        {
            return "Info";
        }

        return importAudit.Any(record => record.ResultStatus.Equals(
            LocalServerEntitlementImportResultStatuses.Rejected,
            StringComparison.OrdinalIgnoreCase))
                ? "Warning"
                : "Ok";
    }

    private static string BuildServiceDetail(
        IReadOnlyCollection<LocalServerDiagnosticServiceResponse> services)
    {
        var serviceSummary = string.Join(
            ", ",
            services.Select(service => $"{service.ServiceName}: {service.CurrentState ?? "Unknown"}"));

        return $"Reported services: {serviceSummary}.";
    }

    private static string BuildImportAuditDetail(
        IReadOnlyCollection<LocalServerDiagnosticImportAuditResponse> importAudit)
    {
        var acceptedCount = importAudit.Count(record => record.ResultStatus.Equals(
            LocalServerEntitlementImportResultStatuses.Accepted,
            StringComparison.OrdinalIgnoreCase));
        var rejectedCount = importAudit.Count(record => record.ResultStatus.Equals(
            LocalServerEntitlementImportResultStatuses.Rejected,
            StringComparison.OrdinalIgnoreCase));
        var latest = importAudit
            .OrderByDescending(record => record.OccurredAtUtc)
            .First();

        return $"Recent entitlement imports: {acceptedCount} accepted, {rejectedCount} rejected; latest source '{latest.ImportSource}' at {latest.OccurredAtUtc:O}.";
    }

    private static bool IsFailureState(string? currentState)
    {
        return currentState is not null
            && (currentState.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Exited", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Stopped", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWarningState(string? currentState)
    {
        return currentState is not null
            && (currentState.Equals("Starting", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Restarting", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Degraded", StringComparison.OrdinalIgnoreCase)
                || currentState.Equals("Unknown", StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatVersion(string? version)
    {
        return string.IsNullOrWhiteSpace(version)
            ? ""
            : $" ({version.Trim()})";
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
