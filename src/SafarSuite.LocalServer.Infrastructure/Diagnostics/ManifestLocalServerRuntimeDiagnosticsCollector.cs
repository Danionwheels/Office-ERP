using System.Runtime.InteropServices;
using System.Text.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Diagnostics.Ports;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Infrastructure.Diagnostics;

public sealed class ManifestLocalServerRuntimeDiagnosticsCollector
    : ILocalServerRuntimeDiagnosticsCollector
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(5);
    private const int ComposeLogTailLines = 80;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILocalServerBootstrapConfigurationStore _configurationStore;
    private readonly ILocalServerRuntimeCommandRunner _commandRunner;

    public ManifestLocalServerRuntimeDiagnosticsCollector(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerRuntimeCommandRunner commandRunner)
    {
        _configurationStore = configurationStore;
        _commandRunner = commandRunner;
    }

    public async Task<LocalServerRuntimeDiagnosticsSnapshot> CollectAsync(
        LocalServerRuntimeDiagnosticsContext context,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return LocalServerRuntimeDiagnosticsSnapshot.Empty;
        }

        var payload = TryReadPayload(configuration.PayloadJson);
        var runtimePlan = configuration.RuntimePlan;
        var docker = await ProbeDockerAsync(cancellationToken);
        var compose = await ProbeComposeAsync(runtimePlan, docker, cancellationToken);
        var runtime = BuildRuntime(context, configuration, runtimePlan, docker, compose);
        var bootstrap = BuildBootstrap(configuration, payload);
        var services = BuildServices(runtimePlan, compose);
        var logEntries = await CollectComposeLogTailAsync(
            runtimePlan,
            compose,
            cancellationToken);
        var recentErrors = BuildRecentErrors(docker, compose, services, logEntries);

        return new LocalServerRuntimeDiagnosticsSnapshot(
            runtime,
            bootstrap,
            services,
            recentErrors);
    }

    private static LocalServerDiagnosticRuntimeResponse BuildRuntime(
        LocalServerRuntimeDiagnosticsContext context,
        LocalServerBootstrapConfiguration configuration,
        LocalServerBootstrapRuntimePlan? runtimePlan,
        RuntimeCommandProbe docker,
        ComposeProbe compose)
    {
        var localServerVersion = NormalizeText(
            Environment.GetEnvironmentVariable("SAFARSUITE_LOCAL_SERVER_VERSION"))
            ?? NormalizeText(runtimePlan?.LocalServerVersion)
            ?? NormalizeText(configuration.LocalServerVersion)
            ?? context.LocalServerVersion;
        var runtimeMode = NormalizeText(runtimePlan?.RuntimeMode) ?? "LocalServer";

        return new LocalServerDiagnosticRuntimeResponse(
            localServerVersion,
            BuildChannel: NormalizeText(Environment.GetEnvironmentVariable("SAFARSUITE_BUILD_CHANNEL")) ?? "Unknown",
            BuildCommit: NormalizeText(Environment.GetEnvironmentVariable("SAFARSUITE_BUILD_COMMIT")) ?? "Unknown",
            runtimeMode,
            context.MachineName,
            context.OperatingSystem,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            docker.IsAvailable,
            docker.Version,
            compose.IsAvailable,
            compose.Version);
    }

    private static LocalServerDiagnosticBootstrapResponse BuildBootstrap(
        LocalServerBootstrapConfiguration configuration,
        LocalServerBootstrapPackagePayloadResponse? payload)
    {
        var configDirectory = NormalizeText(configuration.RuntimePlan?.ConfigDirectory)
            ?? "/etc/safarsuite/local-server";

        return new LocalServerDiagnosticBootstrapResponse(
            configDirectory,
            configuration.RegistrationStatus,
            configuration.PayloadSha256,
            FindArtifactSha(payload, "DockerComposeTemplate"),
            FindArtifactSha(payload, "EnvironmentTemplate"),
            configuration.LastRegistrationAttemptUtc,
            configuration.LastRegistrationSucceededAtUtc,
            LastHeartbeatSentAtUtc: null,
            LastEntitlementPullAtUtc: null);
    }

    private static IReadOnlyCollection<LocalServerDiagnosticServiceResponse> BuildServices(
        LocalServerBootstrapRuntimePlan? runtimePlan,
        ComposeProbe compose)
    {
        if (runtimePlan is null || runtimePlan.Services.Count == 0)
        {
            return Array.Empty<LocalServerDiagnosticServiceResponse>();
        }

        return runtimePlan.Services
            .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Select(service =>
            {
                var optionalProfile = !service.StartsByDefault
                    && !string.IsNullOrWhiteSpace(service.ComposeProfile);
                var liveService = compose.Services.GetValueOrDefault(service.ServiceName);
                var expectedState = optionalProfile && liveService is null
                    ? "ProfileDisabled"
                    : "Running";
                var currentState = liveService is null
                    ? optionalProfile ? "ProfileDisabled" : ToUnreportedState(compose)
                    : ToRuntimeServiceState(liveService);
                var detail = BuildServiceDetail(service, liveService, compose, optionalProfile);

                return new LocalServerDiagnosticServiceResponse(
                    service.ServiceName,
                    expectedState,
                    currentState,
                    liveService?.ContainerName,
                    liveService?.CreatedAtUtc,
                    detail);
            })
            .ToArray();
    }

    private async Task<RuntimeCommandProbe> ProbeDockerAsync(
        CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(
            "docker",
            ["--version"],
            workingDirectory: null,
            CommandTimeout,
            cancellationToken);

        return result.IsSuccess
            ? RuntimeCommandProbe.Available(NormalizeText(result.StandardOutput) ?? "Available")
            : RuntimeCommandProbe.Unavailable(BuildCommandFailureDetail("Docker", result));
    }

    private async Task<ComposeProbe> ProbeComposeAsync(
        LocalServerBootstrapRuntimePlan? runtimePlan,
        RuntimeCommandProbe docker,
        CancellationToken cancellationToken)
    {
        if (!docker.IsAvailable)
        {
            return ComposeProbe.Unavailable("Docker is not available.");
        }

        var versionResult = await _commandRunner.RunAsync(
            "docker",
            ["compose", "version"],
            workingDirectory: null,
            CommandTimeout,
            cancellationToken);

        if (!versionResult.IsSuccess)
        {
            return ComposeProbe.Unavailable(BuildCommandFailureDetail("Docker Compose", versionResult));
        }

        var projectName = NormalizeText(runtimePlan?.ComposeProjectName)
            ?? "safarsuite-local-server";
        var configDirectory = NormalizeText(runtimePlan?.ConfigDirectory);
        var arguments = BuildComposePsArguments(projectName, configDirectory);
        var psResult = await _commandRunner.RunAsync(
            "docker",
            arguments,
            configDirectory,
            CommandTimeout,
            cancellationToken);

        if (!psResult.IsSuccess)
        {
            return ComposeProbe.Available(
                NormalizeText(versionResult.StandardOutput) ?? "Available",
                Services: new Dictionary<string, ComposeServiceProbe>(StringComparer.OrdinalIgnoreCase),
                FailureDetail: BuildCommandFailureDetail("Docker Compose ps", psResult));
        }

        return ComposeProbe.Available(
            NormalizeText(versionResult.StandardOutput) ?? "Available",
            ParseComposeServices(psResult.StandardOutput),
            FailureDetail: null);
    }

    private async Task<IReadOnlyCollection<RuntimeLogEntry>> CollectComposeLogTailAsync(
        LocalServerBootstrapRuntimePlan? runtimePlan,
        ComposeProbe compose,
        CancellationToken cancellationToken)
    {
        if (!compose.IsAvailable || runtimePlan is null)
        {
            return Array.Empty<RuntimeLogEntry>();
        }

        var serviceNames = runtimePlan.Services
            .Where(service => service.StartsByDefault || compose.Services.ContainsKey(service.ServiceName))
            .Select(service => service.ServiceName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (serviceNames.Length == 0)
        {
            return Array.Empty<RuntimeLogEntry>();
        }

        var projectName = NormalizeText(runtimePlan.ComposeProjectName)
            ?? "safarsuite-local-server";
        var configDirectory = NormalizeText(runtimePlan.ConfigDirectory);
        var result = await _commandRunner.RunAsync(
            "docker",
            BuildComposeLogsArguments(projectName, configDirectory, serviceNames),
            configDirectory,
            CommandTimeout,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return
            [
                new RuntimeLogEntry(
                    "docker-compose",
                    "Warning",
                    BuildCommandFailureDetail("Docker Compose logs", result),
                    OccurredAtUtc: null)
            ];
        }

        return ParseComposeLogEntries(result.StandardOutput, serviceNames);
    }

    private static IReadOnlyCollection<string> BuildComposePsArguments(
        string projectName,
        string? configDirectory)
    {
        var arguments = new List<string>
        {
            "compose",
            "--project-name",
            projectName
        };

        if (!string.IsNullOrWhiteSpace(configDirectory))
        {
            var composeFilePath = Path.Combine(configDirectory, "docker-compose.yml");

            if (File.Exists(composeFilePath))
            {
                arguments.Add("--file");
                arguments.Add(composeFilePath);
            }
        }

        arguments.Add("ps");
        arguments.Add("--format");
        arguments.Add("json");

        return arguments;
    }

    private static IReadOnlyCollection<string> BuildComposeLogsArguments(
        string projectName,
        string? configDirectory,
        IReadOnlyCollection<string> serviceNames)
    {
        var arguments = new List<string>
        {
            "compose",
            "--project-name",
            projectName
        };

        if (!string.IsNullOrWhiteSpace(configDirectory))
        {
            var composeFilePath = Path.Combine(configDirectory, "docker-compose.yml");

            if (File.Exists(composeFilePath))
            {
                arguments.Add("--file");
                arguments.Add(composeFilePath);
            }
        }

        arguments.Add("logs");
        arguments.Add("--no-color");
        arguments.Add("--tail");
        arguments.Add(ComposeLogTailLines.ToString(System.Globalization.CultureInfo.InvariantCulture));

        foreach (var serviceName in serviceNames)
        {
            arguments.Add(serviceName);
        }

        return arguments;
    }

    private static IReadOnlyDictionary<string, ComposeServiceProbe> ParseComposeServices(
        string output)
    {
        var services = new Dictionary<string, ComposeServiceProbe>(StringComparer.OrdinalIgnoreCase);
        var trimmed = output.Trim();

        if (trimmed.Length == 0)
        {
            return services;
        }

        try
        {
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                using var document = JsonDocument.Parse(trimmed);

                foreach (var item in document.RootElement.EnumerateArray())
                {
                    AddService(services, item);
                }
            }
            else
            {
                foreach (var line in trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    using var document = JsonDocument.Parse(line);
                    AddService(services, document.RootElement);
                }
            }
        }
        catch (JsonException)
        {
            return services;
        }

        return services;
    }

    private static void AddService(
        Dictionary<string, ComposeServiceProbe> services,
        JsonElement element)
    {
        var serviceName = GetString(element, "Service")
            ?? GetString(element, "service")
            ?? GetString(element, "Name");

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return;
        }

        services[serviceName.Trim()] = new ComposeServiceProbe(
            serviceName.Trim(),
            GetString(element, "Name"),
            GetString(element, "State"),
            GetString(element, "Health"),
            GetString(element, "Status"),
            ParseDateTimeOffset(GetString(element, "CreatedAt")));
    }

    private static string ToUnreportedState(ComposeProbe compose)
    {
        if (!compose.IsAvailable)
        {
            return "Unknown";
        }

        return string.IsNullOrWhiteSpace(compose.FailureDetail)
            ? "Stopped"
            : "Unknown";
    }

    private static string ToRuntimeServiceState(
        ComposeServiceProbe service)
    {
        var state = NormalizeText(service.State);
        var health = NormalizeText(service.Health);
        var status = NormalizeText(service.Status);

        if (health?.Equals("unhealthy", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Unhealthy";
        }

        if (health?.Equals("starting", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Starting";
        }

        if (state?.Equals("running", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Running";
        }

        if (state?.Equals("exited", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Exited";
        }

        if (state?.Equals("dead", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Failed";
        }

        if (status?.StartsWith("Up", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Running";
        }

        if (status?.Contains("Exited", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Exited";
        }

        return state is null ? "Unknown" : NormalizeStateName(state);
    }

    private static string BuildServiceDetail(
        LocalServerBootstrapRuntimeService expectedService,
        ComposeServiceProbe? liveService,
        ComposeProbe compose,
        bool optionalProfile)
    {
        var manifestIntent = BuildExpectedServiceIntentDetail(expectedService);

        if (liveService is not null)
        {
            var state = NormalizeText(liveService.State) ?? "unknown";
            var health = NormalizeText(liveService.Health);
            var healthDetail = health is null ? "" : $", health '{health}'";
            var container = NormalizeText(liveService.ContainerName) ?? "unknown container";

            return $"Compose service '{expectedService.ServiceName}' is '{state}'{healthDetail} in '{container}'. {manifestIntent}";
        }

        if (optionalProfile)
        {
            return $"Optional Compose profile '{expectedService.ComposeProfile}' is present but not expected to start by default. {manifestIntent}";
        }

        if (!compose.IsAvailable)
        {
            return $"Live Compose probing unavailable: {compose.FailureDetail ?? "Docker Compose is not available."} {manifestIntent}";
        }

        if (!string.IsNullOrWhiteSpace(compose.FailureDetail))
        {
            return $"Live Compose service state could not be read: {compose.FailureDetail} {manifestIntent}";
        }

        return $"Service is declared in the signed runtime manifest but was not reported by Docker Compose. {manifestIntent}";
    }

    private static string BuildExpectedServiceIntentDetail(
        LocalServerBootstrapRuntimeService expectedService)
    {
        var role = NormalizeText(expectedService.ServiceRole) ?? "runtime service";
        var profile = NormalizeText(expectedService.ComposeProfile);
        var profileDetail = profile is null
            ? "default Compose profile"
            : $"Compose profile '{profile}'";
        var imageEnvironmentVariable = NormalizeText(expectedService.ImageEnvironmentVariable)
            ?? "unknown";
        var publishedPortEnvironmentVariable = NormalizeText(expectedService.PublishedPortEnvironmentVariable);
        var portDetail = publishedPortEnvironmentVariable is null
            ? "no host port env"
            : $"host port env '{publishedPortEnvironmentVariable}'";
        var internalBaseUrl = NormalizeText(expectedService.InternalBaseUrl) ?? "n/a";
        var healthUrl = NormalizeText(expectedService.HealthUrl) ?? "n/a";
        var dependencies = expectedService.DependsOn
            .Select(NormalizeText)
            .Where(dependency => dependency is not null)
            .Select(dependency => dependency!)
            .ToArray();
        var dependencyDetail = dependencies.Length == 0
            ? "none"
            : string.Join(", ", dependencies);

        return $"Manifest intent: role '{role}', {profileDetail}, image env '{imageEnvironmentVariable}', {portDetail}, internal '{internalBaseUrl}', health '{healthUrl}', depends on '{dependencyDetail}'.";
    }

    private static IReadOnlyCollection<RuntimeLogEntry> ParseComposeLogEntries(
        string output,
        IReadOnlyCollection<string> expectedServiceNames)
    {
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => ParseComposeLogLine(line, expectedServiceNames))
            .Where(entry => entry is not null)
            .Select(entry => entry!)
            .Where(entry => IsInterestingLogSeverity(entry.Severity, entry.Message))
            .TakeLast(12)
            .ToArray();
    }

    private static RuntimeLogEntry? ParseComposeLogLine(
        string line,
        IReadOnlyCollection<string> expectedServiceNames)
    {
        var normalized = NormalizeText(line);

        if (normalized is null)
        {
            return null;
        }

        var source = "runtime";
        var message = normalized;
        var separatorIndex = normalized.IndexOf('|', StringComparison.Ordinal);

        if (separatorIndex >= 0)
        {
            var rawSource = NormalizeText(normalized[..separatorIndex]);
            message = NormalizeText(normalized[(separatorIndex + 1)..]) ?? "";
            source = ResolveServiceName(rawSource, expectedServiceNames) ?? rawSource ?? source;
        }

        if (message.Length == 0)
        {
            return null;
        }

        return new RuntimeLogEntry(
            source,
            InferLogSeverity(message),
            message.Length <= 500 ? message : message[..500],
            TryParseLeadingTimestamp(message));
    }

    private static string? ResolveServiceName(
        string? rawSource,
        IReadOnlyCollection<string> expectedServiceNames)
    {
        if (rawSource is null)
        {
            return null;
        }

        return expectedServiceNames.FirstOrDefault(serviceName =>
            rawSource.Equals(serviceName, StringComparison.OrdinalIgnoreCase)
            || rawSource.StartsWith($"{serviceName}-", StringComparison.OrdinalIgnoreCase)
            || rawSource.Contains($"-{serviceName}-", StringComparison.OrdinalIgnoreCase))
            ?? rawSource;
    }

    private static bool IsInterestingLogSeverity(
        string severity,
        string message)
    {
        return severity.Equals("Warning", StringComparison.OrdinalIgnoreCase)
            || severity.Equals("Error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("warn", StringComparison.OrdinalIgnoreCase)
            || message.Contains("error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("fail", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferLogSeverity(string message)
    {
        if (message.Contains("critical", StringComparison.OrdinalIgnoreCase)
            || message.Contains("fatal", StringComparison.OrdinalIgnoreCase)
            || message.Contains("error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return "Error";
        }

        return message.Contains("warn", StringComparison.OrdinalIgnoreCase)
            ? "Warning"
            : "Info";
    }

    private static DateTimeOffset? TryParseLeadingTimestamp(string message)
    {
        var firstSpace = message.IndexOf(' ', StringComparison.Ordinal);
        var candidate = firstSpace > 0 ? message[..firstSpace] : message;

        return DateTimeOffset.TryParse(candidate, out var parsed)
            ? parsed
            : null;
    }

    private static IReadOnlyCollection<LocalServerDiagnosticRecentErrorResponse> BuildRecentErrors(
        RuntimeCommandProbe docker,
        ComposeProbe compose,
        IReadOnlyCollection<LocalServerDiagnosticServiceResponse> services,
        IReadOnlyCollection<RuntimeLogEntry> logEntries)
    {
        var errors = new List<LocalServerDiagnosticRecentErrorResponse>();

        if (!docker.IsAvailable)
        {
            errors.Add(new LocalServerDiagnosticRecentErrorResponse(
                "docker",
                "Warning",
                docker.FailureDetail ?? "Docker is not available.",
                OccurredAtUtc: null));
        }

        if (!compose.IsAvailable || !string.IsNullOrWhiteSpace(compose.FailureDetail))
        {
            errors.Add(new LocalServerDiagnosticRecentErrorResponse(
                "docker-compose",
                "Warning",
                compose.FailureDetail ?? "Docker Compose is not available.",
                OccurredAtUtc: null));
        }

        foreach (var service in services.Where(service => IsProblemServiceState(service.CurrentState)))
        {
            errors.Add(new LocalServerDiagnosticRecentErrorResponse(
                service.ServiceName,
                "Warning",
                service.Detail ?? $"Service '{service.ServiceName}' is '{service.CurrentState}'.",
                OccurredAtUtc: service.LastStartedAtUtc));
        }

        errors.AddRange(logEntries.Select(entry => new LocalServerDiagnosticRecentErrorResponse(
            entry.Source,
            entry.Severity,
            entry.Message,
            entry.OccurredAtUtc)));

        return errors
            .Take(20)
            .ToArray();
    }

    private static bool IsProblemServiceState(string? state)
    {
        return state is not null
            && (state.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase)
                || state.Equals("Exited", StringComparison.OrdinalIgnoreCase)
                || state.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                || state.Equals("Stopped", StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildCommandFailureDetail(
        string commandName,
        LocalServerRuntimeCommandResult result)
    {
        if (result.TimedOut)
        {
            return $"{commandName} command timed out.";
        }

        var detail = NormalizeText(result.StandardError)
            ?? NormalizeText(result.StandardOutput)
            ?? $"Command exited with code {result.ExitCode}.";

        return $"{commandName} unavailable: {detail}";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static string NormalizeStateName(string state)
    {
        return string.Concat(
            state[..1].ToUpperInvariant(),
            state[1..].ToLowerInvariant());
    }

    private static LocalServerBootstrapPackagePayloadResponse? TryReadPayload(
        string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<LocalServerBootstrapPackagePayloadResponse>(
                payloadJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindArtifactSha(
        LocalServerBootstrapPackagePayloadResponse? payload,
        string artifactType)
    {
        return payload?.Artifacts
            .FirstOrDefault(artifact => artifact.ArtifactType.Equals(
                artifactType,
                StringComparison.OrdinalIgnoreCase))
            ?.Sha256;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record RuntimeCommandProbe(
        bool IsAvailable,
        string? Version,
        string? FailureDetail)
    {
        public static RuntimeCommandProbe Available(string version) => new(
            IsAvailable: true,
            version,
            FailureDetail: null);

        public static RuntimeCommandProbe Unavailable(string detail) => new(
            IsAvailable: false,
            Version: null,
            detail);
    }

    private sealed record ComposeProbe(
        bool IsAvailable,
        string? Version,
        IReadOnlyDictionary<string, ComposeServiceProbe> Services,
        string? FailureDetail)
    {
        public static ComposeProbe Available(
            string version,
            IReadOnlyDictionary<string, ComposeServiceProbe> Services,
            string? FailureDetail) => new(
            IsAvailable: true,
            version,
            Services,
            FailureDetail);

        public static ComposeProbe Unavailable(string detail) => new(
            IsAvailable: false,
            Version: null,
            Services: new Dictionary<string, ComposeServiceProbe>(StringComparer.OrdinalIgnoreCase),
            detail);
    }

    private sealed record ComposeServiceProbe(
        string ServiceName,
        string? ContainerName,
        string? State,
        string? Health,
        string? Status,
        DateTimeOffset? CreatedAtUtc);

    private sealed record RuntimeLogEntry(
        string Source,
        string Severity,
        string Message,
        DateTimeOffset? OccurredAtUtc);
}
