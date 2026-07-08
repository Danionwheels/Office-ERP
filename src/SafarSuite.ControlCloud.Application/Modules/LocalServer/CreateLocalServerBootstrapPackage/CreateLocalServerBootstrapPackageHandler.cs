using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;

public sealed class CreateLocalServerBootstrapPackageHandler
{
    private const string DefaultCloudBaseUrl = "https://control-cloud.safarsuite.local";
    private const string DefaultLocalServerVersion = "latest";

    private static readonly JsonSerializerOptions BootstrapBundleJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions RuntimeManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly CreateInstallationSetupTokenHandler _setupTokenHandler;
    private readonly IControlCloudBootstrapPackageSigner _bootstrapPackageSigner;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudClock _clock;

    public CreateLocalServerBootstrapPackageHandler(
        CreateInstallationSetupTokenHandler setupTokenHandler,
        IControlCloudBootstrapPackageSigner bootstrapPackageSigner,
        IClientPortalAuditRecorder audit,
        IControlCloudClock clock)
    {
        _setupTokenHandler = setupTokenHandler;
        _bootstrapPackageSigner = bootstrapPackageSigner;
        _audit = audit;
        _clock = clock;
    }

    public async Task<CreateLocalServerBootstrapPackageResult> HandleAsync(
        CreateLocalServerBootstrapPackageCommand command,
        CancellationToken cancellationToken = default)
    {
        var cloudBaseUrl = NormalizeAbsoluteHttpUrl(command.CloudBaseUrl, DefaultCloudBaseUrl);

        if (cloudBaseUrl is null)
        {
            return CreateLocalServerBootstrapPackageResult.Failure(
                "CloudBaseUrlInvalid",
                "Cloud base URL must be an absolute HTTP or HTTPS URL.");
        }

        var installScriptUrl = NormalizeAbsoluteHttpUrl(
            command.InstallScriptUrl,
            $"{cloudBaseUrl}/install/safarsuite-local-server/install.sh");

        if (installScriptUrl is null)
        {
            return CreateLocalServerBootstrapPackageResult.Failure(
                "InstallScriptUrlInvalid",
                "Install script URL must be an absolute HTTP or HTTPS URL.");
        }

        var setupTokenResult = await _setupTokenHandler.HandleAsync(
            new CreateInstallationSetupTokenCommand(
                command.ClientId,
                command.InstallationId,
                command.ExpiresInHours,
                command.CreatedBy,
                command.DeploymentMode,
                command.ClientDeploymentMode,
                command.SiteId,
                command.SiteRole,
                command.ParentSiteId,
                command.BranchCode,
                command.SyncTopologyId),
            cancellationToken);

        if (!setupTokenResult.IsSuccess)
        {
            return CreateLocalServerBootstrapPackageResult.Failure(
                setupTokenResult.FailureCode ?? "SetupTokenCreationFailed",
                setupTokenResult.Detail ?? "Setup token could not be created.");
        }

        var setupToken = setupTokenResult.SetupToken!;
        var localServerVersion = NormalizeText(command.LocalServerVersion) ?? DefaultLocalServerVersion;
        var safarSuiteAppVersion = NormalizeText(command.SafarSuiteAppVersion) ?? localServerVersion;
        var endpoints = BuildEndpoints(cloudBaseUrl, setupToken.ClientId, setupToken.InstallationId);
        var runtimePlan = BuildRuntimePlan(localServerVersion, safarSuiteAppVersion);
        var artifacts = BuildArtifacts(cloudBaseUrl, runtimePlan);
        var deploymentProfile = ToResponse(setupToken.DeploymentProfile);
        var generatedAtUtc = _clock.UtcNow;
        var bootstrapPackageId = Guid.NewGuid();
        var installCommand = BuildInstallCommand(
            installScriptUrl,
            cloudBaseUrl,
            setupToken.ClientId,
            setupToken.InstallationId,
            setupTokenResult.PlainSetupToken!,
            setupToken.DeploymentProfile,
            localServerVersion,
            safarSuiteAppVersion);
        var payload = new ControlCloudBootstrapPackagePayload(
            ControlCloudLocalServerBootstrapPackageFormat.Version,
            bootstrapPackageId,
            setupToken.SetupTokenId,
            setupToken.ClientId,
            setupToken.InstallationId,
            setupToken.DeploymentMode,
            setupToken.DeploymentProfile,
            cloudBaseUrl,
            localServerVersion,
            setupTokenResult.PlainSetupToken!,
            setupToken.ExpiresAtUtc,
            generatedAtUtc,
            installScriptUrl,
            installCommand,
            artifacts,
            runtimePlan,
            endpoints);
        var signedBundle = _bootstrapPackageSigner.Sign(payload);
        var signedBundleResponse = ToResponse(signedBundle);
        var package = new LocalServerBootstrapPackageResponse(
            ControlCloudLocalServerBootstrapPackageFormat.Version,
            bootstrapPackageId,
            setupToken.SetupTokenId,
            setupToken.ClientId,
            setupToken.InstallationId,
            setupToken.DeploymentMode,
            deploymentProfile,
            cloudBaseUrl,
            localServerVersion,
            setupTokenResult.PlainSetupToken!,
            setupToken.ExpiresAtUtc,
            generatedAtUtc,
            ToResponse(endpoints),
            installScriptUrl,
            installCommand,
            artifacts.Select(ToResponse).ToArray(),
            BuildBootstrapBundleFileName(setupToken.ClientId, setupToken.InstallationId, bootstrapPackageId),
            ControlCloudLocalServerBootstrapPackageFormat.BundleContentType,
            ComputeSha256(JsonSerializer.Serialize(signedBundleResponse, BootstrapBundleJsonOptions)),
            signedBundleResponse,
            ToResponse(runtimePlan));

        await ControlCloudAuditWriter.TryRecordAsync(
            _audit,
            new ClientPortalAuditRecord(
                Guid.NewGuid(),
                package.ClientId,
                InvitationId: null,
                UserId: null,
                SubjectEmail: "",
                ClientPortalAuditEventTypes.BootstrapPackageGenerated,
                ControlCloudAuditWriter.NormalizeActor(command.CreatedBy, ClientPortalAuditActors.ControlDesk),
                $"Bootstrap package '{package.BootstrapPackageId}' generated for installation '{package.InstallationId}' using setup token '{package.SetupTokenId}', local-server version '{package.LocalServerVersion}', and SafarSuite app version '{runtimePlan.SafarSuiteAppVersion}'.",
                package.GeneratedAtUtc),
            cancellationToken);

        return CreateLocalServerBootstrapPackageResult.Success(package);
    }

    private static ControlCloudBootstrapPackageEndpoints BuildEndpoints(
        string cloudBaseUrl,
        Guid clientId,
        string installationId)
    {
        var escapedInstallationId = Uri.EscapeDataString(installationId);

        return new ControlCloudBootstrapPackageEndpoints(
            $"{cloudBaseUrl}/api/v1/local-server/installations/{escapedInstallationId}/registration",
            $"{cloudBaseUrl}/api/v1/local-server/installations/{escapedInstallationId}/entitlement-bundle?clientId={clientId:D}",
            $"{cloudBaseUrl}/api/v1/local-server/installations/{escapedInstallationId}/heartbeat",
            $"{cloudBaseUrl}/api/v1/local-server/installations/{escapedInstallationId}/commands/pending",
            $"{cloudBaseUrl}/api/v1/local-server/installations/{escapedInstallationId}/diagnostics");
    }

    private static string BuildInstallCommand(
        string installScriptUrl,
        string cloudBaseUrl,
        Guid clientId,
        string installationId,
        string setupToken,
        ControlCloudInstallationDeploymentProfile deploymentProfile,
        string localServerVersion,
        string safarSuiteAppVersion)
    {
        return string.Join(
            " ",
            "curl",
            "-fsSL",
            QuoteForShell(installScriptUrl),
            "-o",
            "safarsuite-install.sh",
            "&&",
            "sudo",
            "env",
            $"SAFARSUITE_CONTROL_CLOUD_URL={QuoteForShell(cloudBaseUrl)}",
            $"SAFARSUITE_CLIENT_ID={QuoteForShell(clientId.ToString("D"))}",
            $"SAFARSUITE_INSTALLATION_ID={QuoteForShell(installationId)}",
            $"SAFARSUITE_SETUP_TOKEN={QuoteForShell(setupToken)}",
            $"SAFARSUITE_BOOTSTRAP_MODE={QuoteForShell(deploymentProfile.BootstrapMode)}",
            $"SAFARSUITE_CLIENT_DEPLOYMENT_MODE={QuoteForShell(deploymentProfile.ClientDeploymentMode)}",
            $"SAFARSUITE_SITE_ID={QuoteForShell(deploymentProfile.SiteId)}",
            $"SAFARSUITE_SITE_ROLE={QuoteForShell(deploymentProfile.SiteRole)}",
            $"SAFARSUITE_PARENT_SITE_ID={QuoteForShell(deploymentProfile.ParentSiteId ?? "")}",
            $"SAFARSUITE_BRANCH_CODE={QuoteForShell(deploymentProfile.BranchCode ?? "")}",
            $"SAFARSUITE_SYNC_TOPOLOGY_ID={QuoteForShell(deploymentProfile.SyncTopologyId ?? "")}",
            $"SAFARSUITE_LOCAL_SERVER_VERSION={QuoteForShell(localServerVersion)}",
            $"SAFARSUITE_APP_VERSION={QuoteForShell(safarSuiteAppVersion)}",
            "bash",
            "safarsuite-install.sh");
    }

    private static IReadOnlyCollection<ControlCloudBootstrapPackageArtifact> BuildArtifacts(
        string cloudBaseUrl,
        ControlCloudBootstrapRuntimePlan runtimePlan)
    {
        var dockerComposeContent = NormalizeTemplate(DockerComposeTemplate);
        var envTemplateContent = NormalizeTemplate(EnvironmentTemplate);
        var runtimeManifestContent = NormalizeTemplate(JsonSerializer.Serialize(
            ToResponse(runtimePlan),
            RuntimeManifestJsonOptions));

        return
        [
            new ControlCloudBootstrapPackageArtifact(
                "DockerComposeTemplate",
                "docker-compose.yml",
                $"{cloudBaseUrl}/install/safarsuite-local-server/docker-compose.yml",
                "/etc/safarsuite/local-server/docker-compose.yml",
                "application/yaml",
                ComputeSha256(dockerComposeContent),
                dockerComposeContent),
            new ControlCloudBootstrapPackageArtifact(
                "EnvironmentTemplate",
                "local-server.env.template",
                $"{cloudBaseUrl}/install/safarsuite-local-server/local-server.env.template",
                "/etc/safarsuite/local-server/local-server.env",
                "text/plain",
                ComputeSha256(envTemplateContent),
                envTemplateContent),
            new ControlCloudBootstrapPackageArtifact(
                "RuntimeServicesManifest",
                "runtime-services.manifest.json",
                $"{cloudBaseUrl}/install/safarsuite-local-server/runtime-services.manifest.json",
                "/etc/safarsuite/local-server/runtime-services.manifest.json",
                "application/json",
                ComputeSha256(runtimeManifestContent),
                runtimeManifestContent)
        ];
    }

    private static ControlCloudBootstrapRuntimePlan BuildRuntimePlan(
        string localServerVersion,
        string safarSuiteAppVersion)
    {
        return new ControlCloudBootstrapRuntimePlan(
            RuntimeMode: "DockerCompose",
            ComposeProjectName: "safarsuite-local-server",
            ConfigDirectory: "/etc/safarsuite/local-server",
            StateDirectory: "/var/lib/safarsuite/local-server",
            LocalServerVersion: localServerVersion,
            SafarSuiteAppVersion: safarSuiteAppVersion,
            Services:
            [
                new ControlCloudBootstrapRuntimeService(
                    "local-api",
                    "Local entitlement, diagnostics, and module-gateway API",
                    StartsByDefault: true,
                    ComposeProfile: null,
                    ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                    PublishedPortEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_HTTP_PORT",
                    InternalBaseUrl: "https://local-api:8080",
                    HealthUrl: "https://local-api:8080/health",
                    DependsOn: ["local-db"]),
                new ControlCloudBootstrapRuntimeService(
                    "local-worker",
                    "Background entitlement pull and heartbeat reporting",
                    StartsByDefault: true,
                    ComposeProfile: null,
                    ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                    PublishedPortEnvironmentVariable: null,
                    InternalBaseUrl: "n/a",
                    HealthUrl: "n/a",
                    DependsOn: ["local-db"]),
                new ControlCloudBootstrapRuntimeService(
                    "local-agent",
                    "Support command polling, diagnostics, and acknowledgement bridge",
                    StartsByDefault: true,
                    ComposeProfile: null,
                    ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                    PublishedPortEnvironmentVariable: null,
                    InternalBaseUrl: "n/a",
                    HealthUrl: "n/a",
                    DependsOn: ["local-db"]),
                new ControlCloudBootstrapRuntimeService(
                    "safarsuite-app",
                    "Customer-facing SafarSuite application runtime",
                    StartsByDefault: false,
                    ComposeProfile: "app-runtime",
                    ImageEnvironmentVariable: "SAFARSUITE_APP_IMAGE",
                    PublishedPortEnvironmentVariable: "SAFARSUITE_APP_HTTP_PORT",
                    InternalBaseUrl: "http://safarsuite-app:5280",
                    HealthUrl: "http://safarsuite-app:5280/health",
                    DependsOn: ["local-api", "local-db"])
            ]);
    }

    private static string? NormalizeAbsoluteHttpUrl(
        string? value,
        string fallback)
    {
        var normalized = NormalizeText(value) ?? fallback.Trim();
        normalized = normalized.TrimEnd('/');

        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : null;
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string QuoteForShell(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'")}'";
    }

    private static LocalServerBootstrapPackageEndpointsResponse ToResponse(
        ControlCloudBootstrapPackageEndpoints endpoints)
    {
        return new LocalServerBootstrapPackageEndpointsResponse(
            endpoints.RegistrationUrl,
            endpoints.EntitlementBundleUrl,
            endpoints.HeartbeatUrl,
            endpoints.PendingCommandsUrl,
            endpoints.DiagnosticsUrl);
    }

    private static LocalServerSignedBootstrapBundleResponse ToResponse(
        ControlCloudSignedBootstrapPackage signedBundle)
    {
        return new LocalServerSignedBootstrapBundleResponse(
            signedBundle.PayloadJson,
            new LocalServerBootstrapPackagePayloadResponse(
                signedBundle.Payload.FormatVersion,
                signedBundle.Payload.BootstrapPackageId,
                signedBundle.Payload.SetupTokenId,
                signedBundle.Payload.ClientId,
                signedBundle.Payload.InstallationId,
                signedBundle.Payload.DeploymentMode,
                ToResponse(signedBundle.Payload.DeploymentProfile),
                signedBundle.Payload.CloudBaseUrl,
                signedBundle.Payload.LocalServerVersion,
                signedBundle.Payload.SetupToken,
                signedBundle.Payload.SetupTokenExpiresAtUtc,
                signedBundle.Payload.GeneratedAtUtc,
                signedBundle.Payload.InstallScriptUrl,
                signedBundle.Payload.InstallCommand,
                signedBundle.Payload.Artifacts.Select(ToResponse).ToArray(),
                ToResponse(signedBundle.Payload.Endpoints),
                ToResponse(signedBundle.Payload.RuntimePlan)),
            new LocalServerBootstrapPackageSignatureResponse(
                signedBundle.Signature.Algorithm,
                signedBundle.Signature.KeyId,
                signedBundle.Signature.PayloadSha256,
                signedBundle.Signature.Value));
    }

    private static LocalServerBootstrapPackageArtifactResponse ToResponse(
        ControlCloudBootstrapPackageArtifact artifact)
    {
        return new LocalServerBootstrapPackageArtifactResponse(
            artifact.ArtifactType,
            artifact.FileName,
            artifact.DownloadUrl,
            artifact.TargetPath,
            artifact.ContentType,
            artifact.Sha256,
            artifact.Content);
    }

    private static LocalServerBootstrapRuntimePlanResponse ToResponse(
        ControlCloudBootstrapRuntimePlan runtimePlan)
    {
        return new LocalServerBootstrapRuntimePlanResponse(
            runtimePlan.RuntimeMode,
            runtimePlan.ComposeProjectName,
            runtimePlan.ConfigDirectory,
            runtimePlan.StateDirectory,
            runtimePlan.LocalServerVersion,
            runtimePlan.SafarSuiteAppVersion,
            runtimePlan.Services.Select(ToResponse).ToArray());
    }

    private static LocalServerDeploymentProfileResponse ToResponse(
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new LocalServerDeploymentProfileResponse(
            deploymentProfile.BootstrapMode,
            deploymentProfile.ClientDeploymentMode,
            deploymentProfile.SiteId,
            deploymentProfile.SiteRole,
            deploymentProfile.ParentSiteId,
            deploymentProfile.BranchCode,
            deploymentProfile.SyncTopologyId);
    }

    private static LocalServerBootstrapRuntimeServiceResponse ToResponse(
        ControlCloudBootstrapRuntimeService service)
    {
        return new LocalServerBootstrapRuntimeServiceResponse(
            service.ServiceName,
            service.ServiceRole,
            service.StartsByDefault,
            service.ComposeProfile,
            service.ImageEnvironmentVariable,
            service.PublishedPortEnvironmentVariable,
            service.InternalBaseUrl,
            service.HealthUrl,
            service.DependsOn);
    }

    private static string BuildBootstrapBundleFileName(
        Guid clientId,
        string installationId,
        Guid bootstrapPackageId)
    {
        var cleanInstallationId = new string(
            installationId
                .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                    ? character
                    : '-')
                .ToArray());

        return $"safarsuite-bootstrap-{clientId:N}-{cleanInstallationId}-{bootstrapPackageId:N}.json";
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeTemplate(string template)
    {
        return template.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private const string DockerComposeTemplate =
        """
        name: safarsuite-local-server

        services:
          local-db:
            image: ${SAFARSUITE_LOCAL_DB_IMAGE:-postgres:16-alpine}
            restart: unless-stopped
            environment:
              POSTGRES_DB: ${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}
              POSTGRES_USER: ${SAFARSUITE_LOCAL_DB_USER:-safarsuite}
              POSTGRES_PASSWORD: ${SAFARSUITE_LOCAL_DB_PASSWORD:?Set SAFARSUITE_LOCAL_DB_PASSWORD}
            volumes:
              - safarsuite-local-db:/var/lib/postgresql/data
            healthcheck:
              test: ["CMD-SHELL", "pg_isready -U ${SAFARSUITE_LOCAL_DB_USER:-safarsuite} -d ${SAFARSUITE_LOCAL_DB_NAME:-safarsuite_local}"]
              interval: 10s
              timeout: 5s
              retries: 5

          local-api:
            image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
            restart: unless-stopped
            command: ["safarsuite-local-api"]
            env_file:
              - ./local-server.env
            depends_on:
              local-db:
                condition: service_healthy
            ports:
              - "${SAFARSUITE_LOCAL_SERVER_HTTP_PORT:-8080}:8080"
            volumes:
              - safarsuite-local-data:/var/lib/safarsuite/local-server
              - ./certs/local-api:/etc/safarsuite/local-server/certs/local-api:ro
              - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

          local-worker:
            image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
            restart: unless-stopped
            command: ["safarsuite-local-worker"]
            env_file:
              - ./local-server.env
            depends_on:
              local-db:
                condition: service_healthy
            volumes:
              - safarsuite-local-data:/var/lib/safarsuite/local-server
              - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

          local-agent:
            image: ${SAFARSUITE_LOCAL_SERVER_IMAGE:?Set SAFARSUITE_LOCAL_SERVER_IMAGE}
            restart: unless-stopped
            command: ["safarsuite-local-agent"]
            env_file:
              - ./local-server.env
            depends_on:
              local-db:
                condition: service_healthy
            volumes:
              - safarsuite-local-data:/var/lib/safarsuite/local-server
              - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro

          safarsuite-app:
            image: ${SAFARSUITE_APP_IMAGE:?Set SAFARSUITE_APP_IMAGE}
            restart: unless-stopped
            profiles:
              - app-runtime
            env_file:
              - ./local-server.env
            depends_on:
              local-db:
                condition: service_healthy
              local-api:
                condition: service_started
            ports:
              - "${SAFARSUITE_APP_HTTP_PORT:-5280}:5280"
            environment:
              ASPNETCORE_URLS: http://0.0.0.0:5280
              SAFARSUITE_LOCAL_API_BASE_URL: ${SAFARSUITE_LOCAL_API_BASE_URL:-https://local-api:8080}
              SAFARSUITE_MODULE_GATEWAY_URL: ${SAFARSUITE_MODULE_GATEWAY_URL:-https://local-api:8080}
              SAFARSUITE_RUNTIME_MANIFEST_PATH: ${SAFARSUITE_RUNTIME_MANIFEST_PATH:-/etc/safarsuite/local-server/runtime-services.manifest.json}
            volumes:
              - safarsuite-local-data:/var/lib/safarsuite/local-server
              - ./certs/trust:/etc/safarsuite/local-server/certs/trust:ro
              - ./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro
              - safarsuite-app-data:/var/lib/safarsuite/app

        volumes:
          safarsuite-local-db:
          safarsuite-local-data:
          safarsuite-app-data:
        """;

    private const string EnvironmentTemplate =
        """
        SAFARSUITE_CONTROL_CLOUD_URL={{SAFARSUITE_CONTROL_CLOUD_URL}}
        SAFARSUITE_CLIENT_ID={{SAFARSUITE_CLIENT_ID}}
        SAFARSUITE_INSTALLATION_ID={{SAFARSUITE_INSTALLATION_ID}}
        SAFARSUITE_BOOTSTRAP_MODE={{SAFARSUITE_BOOTSTRAP_MODE}}
        SAFARSUITE_CLIENT_DEPLOYMENT_MODE={{SAFARSUITE_CLIENT_DEPLOYMENT_MODE}}
        SAFARSUITE_SITE_ID={{SAFARSUITE_SITE_ID}}
        SAFARSUITE_SITE_ROLE={{SAFARSUITE_SITE_ROLE}}
        SAFARSUITE_PARENT_SITE_ID={{SAFARSUITE_PARENT_SITE_ID}}
        SAFARSUITE_BRANCH_CODE={{SAFARSUITE_BRANCH_CODE}}
        SAFARSUITE_SYNC_TOPOLOGY_ID={{SAFARSUITE_SYNC_TOPOLOGY_ID}}
        SAFARSUITE_LOCAL_SERVER_VERSION={{SAFARSUITE_LOCAL_SERVER_VERSION}}
        SAFARSUITE_LOCAL_SERVER_IMAGE=ghcr.io/safarsuite/local-server:{{SAFARSUITE_LOCAL_SERVER_VERSION}}
        SAFARSUITE_LOCAL_SERVER_HTTP_PORT=8080
        SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=/etc/safarsuite/local-server
        SAFARSUITE_LOCAL_SERVER_STATE_DIR=/var/lib/safarsuite/local-server
        SAFARSUITE_APP_VERSION={{SAFARSUITE_APP_VERSION}}
        SAFARSUITE_APP_IMAGE=ghcr.io/danionwheels/localserver:{{SAFARSUITE_APP_VERSION}}
        SAFARSUITE_APP_HTTP_PORT=5280
        SAFARSUITE_LOCAL_API_BASE_URL=https://local-api:8080
        SAFARSUITE_LOCAL_API_ACCESS_KEY=change-me-before-start
        SAFARSUITE_LOCAL_API_TLS_MODE=GeneratedLocalCa
        SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=https://0.0.0.0:8080
        SAFARSUITE_LOCAL_API_CERTIFICATE_PATH=
        SAFARSUITE_LOCAL_API_CERTIFICATE_PASSWORD=
        SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH=
        SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES=local-api,localhost
        SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES=127.0.0.1
        SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS=825
        SAFARSUITE_MODULE_GATEWAY_URL=https://local-api:8080
        SAFARSUITE_RUNTIME_MANIFEST_PATH=/etc/safarsuite/local-server/runtime-services.manifest.json
        SAFARSUITE_LOCAL_DB_IMAGE=postgres:16-alpine
        SAFARSUITE_LOCAL_DB_NAME=safarsuite_local
        SAFARSUITE_LOCAL_DB_USER=safarsuite
        SAFARSUITE_LOCAL_DB_PASSWORD=change-me-before-start
        SAFARSUITE_REGISTRATION_URL={{SAFARSUITE_REGISTRATION_URL}}
        SAFARSUITE_ENTITLEMENT_BUNDLE_URL={{SAFARSUITE_ENTITLEMENT_BUNDLE_URL}}
        SAFARSUITE_HEARTBEAT_URL={{SAFARSUITE_HEARTBEAT_URL}}
        SAFARSUITE_PENDING_COMMANDS_URL={{SAFARSUITE_PENDING_COMMANDS_URL}}
        SAFARSUITE_DIAGNOSTICS_URL={{SAFARSUITE_DIAGNOSTICS_URL}}
        DeploymentSecrets__Provider=Environment
        ActivationSigning__SigningKeyId={{SAFARSUITE_APP_ACTIVATION_SIGNING_KEY_ID}}
        ActivationSigning__PublicKeyPem={{SAFARSUITE_APP_ACTIVATION_PUBLIC_KEY_PEM}}
        DeviceCredentials__SigningKeyId=safarsuite-app-device-local
        DeviceCredentials__SigningSecret=change-me-before-start
        UserSessions__SigningKeyId=safarsuite-app-session-local
        UserSessions__SigningSecret=change-me-before-start
        FirstManagerBootstrap__AllowSetupCodeFallback=false
        """;
}
