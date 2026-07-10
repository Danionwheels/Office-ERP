using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.ClientPortal;
using SafarSuite.ControlCloud.Infrastructure.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands;
using SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;
using SafarSuite.LocalServer.Application.Commands.Ports;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommands;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Diagnostics.CreateLocalServerDiagnosticsBundle;
using SafarSuite.LocalServer.Application.Diagnostics.UploadDiagnosticsToControlCloud;
using SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;
using SafarSuite.LocalServer.Application.Entitlements.ImportOfflineRenewalFile;
using SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;
using SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationWithControlCloud;
using SafarSuite.LocalServer.Domain.Entitlements;
using SafarSuite.LocalServer.Domain.Pairing;
using SafarSuite.LocalServer.Domain.Registration;
using SafarSuite.LocalServer.Infrastructure.Commands;
using SafarSuite.LocalServer.Infrastructure.Diagnostics;
using SafarSuite.LocalServer.Infrastructure.Entitlements;
using SafarSuite.LocalServer.Infrastructure.Heartbeats;
using SafarSuite.LocalServer.Infrastructure.Pairing;
using SafarSuite.LocalServer.Infrastructure.Registration;

var installationId = "office-main";
var clientId = Guid.NewGuid();
var cachePath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-entitlement-smoke-{Guid.NewGuid():N}.json");
var trustStatePath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-entitlement-trust-state-smoke-{Guid.NewGuid():N}.json");
var importAuditPath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-entitlement-import-audit-smoke-{Guid.NewGuid():N}.json");
var appActivationRevocationPath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-app-activation-revocations-smoke-{Guid.NewGuid():N}.json");
var bootstrapConfigurationPath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-bootstrap-configuration-smoke-{Guid.NewGuid():N}.json");
var pairingStorePath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-device-pairings-smoke-{Guid.NewGuid():N}.json");
var trustOptions = new LocalServerEntitlementTrustOptions
{
    CacheStorePath = cachePath,
    TrustStateStorePath = trustStatePath,
    ImportAuditStorePath = importAuditPath,
    SigningKeys =
    [
        new LocalServerEntitlementTrustKeyOptions
        {
            KeyId = "local-entitlement-dev",
            Secret = "local-entitlement-signing-secret-change-before-cloud"
        }
    ]
};
var bootstrapTrustOptions = new LocalServerBootstrapTrustOptions
{
    ConfigurationStorePath = bootstrapConfigurationPath,
    SigningKeys =
    [
        new LocalServerBootstrapTrustKeyOptions
        {
            KeyId = "bootstrap-smoke",
            Secret = "bootstrap-signing-secret-change-before-cloud"
        }
    ]
};
var clock = new FixedLocalServerClock(
    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
var cache = new FileLocalServerEntitlementCache(trustOptions);
var trustStateStore = new FileLocalServerEntitlementTrustStateStore(trustOptions);
var importAuditStore = new FileLocalServerEntitlementImportAuditStore(trustOptions);
var bootstrapConfigurationStore = new FileLocalServerBootstrapConfigurationStore(bootstrapTrustOptions);
var pairingStore = new FileLocalServerDevicePairingStore(
    new LocalServerPairingStoreOptions
    {
        DeviceStorePath = pairingStorePath
    });
var firstManagerSetupTokenVerifier = new HmacLocalServerFirstManagerSetupTokenVerifier(
    bootstrapTrustOptions);
var deviceCredentialService = new HmacLocalServerDeviceCredentialService(
    new LocalServerDeviceCredentialOptions
    {
        SigningKeyId = "device-smoke",
        SigningSecret = "device-signing-secret-change-before-cloud",
        ExpiresInDays = 3650
    });
var runtimeCommandRunner = new StaticRuntimeCommandRunner();
var runtimeDiagnosticsCollector = new ManifestLocalServerRuntimeDiagnosticsCollector(
    bootstrapConfigurationStore,
    runtimeCommandRunner);
var verifier = new HmacLocalServerEntitlementBundleVerifier(trustOptions);
var bootstrapVerifier = new HmacLocalServerBootstrapBundleVerifier(bootstrapTrustOptions);
var importHandler = new ImportSignedEntitlementBundleHandler(
    verifier,
    cache,
    trustStateStore,
    clock,
    importAuditStore);
var offlineRenewalImportHandler = new ImportOfflineRenewalFileHandler(importHandler);
var evaluateHandler = new EvaluateFeatureAccessHandler(
    cache,
    trustStateStore,
    new LocalServerEntitlementPolicy(),
    clock);
var moduleGatewayHandler = new EvaluateModuleAccessGatewayHandler(
    evaluateHandler,
    clock);
var diagnosticsBundleHandler = new CreateLocalServerDiagnosticsBundleHandler(
    cache,
    trustStateStore,
    new LocalServerEntitlementPolicy(),
    clock,
    importAuditStore,
    runtimeDiagnosticsCollector);

var signedBundle = CreateSignedBundle(
    clientId,
    installationId,
    entitlementVersion: 100);
var controlCloudOptions = new ControlCloudEntitlementPullOptions
{
    BaseUrl = new Uri("https://control-cloud.local")
};
var registrationHttpHandler = new StaticRegistrationHttpMessageHandler();
var registrationClient = new HttpControlCloudInstallationRegistrationClient(
    new HttpClient(registrationHttpHandler)
    {
        BaseAddress = new Uri("https://control-cloud.local")
    },
    controlCloudOptions);
var registrationHandler = new RegisterInstallationWithControlCloudHandler(registrationClient);
var bootstrapPackage = await CreateBootstrapPackageAsync(clientId, installationId);
Require(bootstrapPackage.DeploymentMode == ControlCloudBootstrapModes.OnlineBootstrap, "Bootstrap package should use the canonical online bootstrap mode.");
Require(bootstrapPackage.DeploymentProfile.ClientDeploymentMode == SafarSuiteClientDeploymentModes.CloudSyncMultiBranch, "Bootstrap package should carry the client deployment mode.");
Require(bootstrapPackage.DeploymentProfile.SiteId == "hq-main", "Bootstrap package should carry the site id.");
Require(bootstrapPackage.DeploymentProfile.SiteRole == SafarSuiteDeploymentSiteRoles.Hq, "Bootstrap package should carry the site role.");
Require(bootstrapPackage.SignedBundle.Payload.DeploymentProfile.SyncTopologyId == "sync-main", "Signed bootstrap bundle should carry the sync topology id.");
Require(bootstrapPackage.AppActivationSigningKey?.SigningKeyId == "app-activation-smoke", "Bootstrap package should carry the app activation signing key id.");
Require(bootstrapPackage.SignedBundle.Payload.AppActivationSigningKey?.SigningKeyId == "app-activation-smoke", "Signed bootstrap bundle should carry the app activation signing key id.");
Require(
    !string.IsNullOrWhiteSpace(bootstrapPackage.AppActivationSigningKey?.PublicKeyPem)
    && bootstrapPackage.AppActivationSigningKey.PublicKeyPem == bootstrapPackage.SignedBundle.Payload.AppActivationSigningKey?.PublicKeyPem,
    "Bootstrap package and signed payload should carry the same app activation public key.");
Require(bootstrapPackage.RuntimePlan?.Services.Count == 4, "Bootstrap package should describe local runtime services.");
var bootstrapRuntimePlan = bootstrapPackage.RuntimePlan
    ?? throw new InvalidOperationException("Bootstrap runtime plan should exist.");
Require(bootstrapRuntimePlan.Services.Any(service =>
    service.ServiceName == "safarsuite-app"
    && service.ComposeProfile == "app-runtime"
    && !service.StartsByDefault), "Bootstrap package should include the optional SafarSuite app runtime slot.");
var dockerComposeArtifact = RequireArtifact(bootstrapPackage, "DockerComposeTemplate");
var environmentTemplateArtifact = RequireArtifact(bootstrapPackage, "EnvironmentTemplate");
var runtimeManifestArtifact = RequireArtifact(bootstrapPackage, "RuntimeServicesManifest");
Require(dockerComposeArtifact.FileName == "docker-compose.yml", "Bootstrap package should include the Docker Compose artifact filename.");
Require(dockerComposeArtifact.TargetPath.EndsWith("/docker-compose.yml", StringComparison.Ordinal), "Bootstrap Docker Compose artifact should target the runtime config directory.");
Require(dockerComposeArtifact.Content.Contains("safarsuite-app:", StringComparison.Ordinal), "Bootstrap Docker Compose template should include the SafarSuite app service.");
Require(dockerComposeArtifact.Content.Contains("${SAFARSUITE_LOCAL_DB_IMAGE:-postgres:16-alpine}", StringComparison.Ordinal), "Bootstrap Docker Compose template should allow the local database image to be overridden.");
Require(dockerComposeArtifact.Content.Contains("profiles:", StringComparison.Ordinal)
    && dockerComposeArtifact.Content.Contains("- app-runtime", StringComparison.Ordinal), "Bootstrap Docker Compose template should keep the SafarSuite app behind the app-runtime profile.");
Require(dockerComposeArtifact.Content.Contains("${SAFARSUITE_APP_IMAGE:?Set SAFARSUITE_APP_IMAGE}", StringComparison.Ordinal), "Bootstrap Docker Compose template should use the app image environment variable.");
Require(dockerComposeArtifact.Content.Contains("${SAFARSUITE_APP_HTTP_PORT:-5280}:5280", StringComparison.Ordinal), "Bootstrap Docker Compose template should publish the app through the configured host port.");
Require(dockerComposeArtifact.Content.Contains("${SAFARSUITE_APP_HTTP_PORT:-5280}:5280/udp", StringComparison.Ordinal), "Bootstrap Docker Compose template should publish the app UDP discovery port through the configured host port.");
Require(dockerComposeArtifact.Content.Contains("SAFARSUITE_MODULE_GATEWAY_URL", StringComparison.Ordinal), "Bootstrap Docker Compose template should wire the app to the local module gateway.");
Require(dockerComposeArtifact.Content.Contains("SAFARSUITE_LOCAL_API_BASE_URL:-https://local-api:8080", StringComparison.Ordinal), "Bootstrap Docker Compose template should default the app to the HTTPS Local API.");
Require(dockerComposeArtifact.Content.Contains("SAFARSUITE_MODULE_GATEWAY_URL:-https://local-api:8080", StringComparison.Ordinal), "Bootstrap Docker Compose template should default module-gateway URL to the HTTPS Local API.");
Require(dockerComposeArtifact.Content.Contains("./runtime-services.manifest.json:/etc/safarsuite/local-server/runtime-services.manifest.json:ro", StringComparison.Ordinal), "Bootstrap Docker Compose template should mount the runtime manifest into local services and app runtime.");
Require(dockerComposeArtifact.Content.Contains("./certs/local-api:/etc/safarsuite/local-server/certs/local-api:ro", StringComparison.Ordinal), "Bootstrap Docker Compose template should mount local API server certificates only into the local API runtime.");
Require(dockerComposeArtifact.Content.Contains("./certs/trust:/etc/safarsuite/local-server/certs/trust:ro", StringComparison.Ordinal), "Bootstrap Docker Compose template should mount trusted local API CA certificates into the app runtime.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_SITE_ID", StringComparison.Ordinal), "Bootstrap environment template should carry site identity placeholders.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=/etc/safarsuite/local-server", StringComparison.Ordinal), "Bootstrap environment template should expose the local-server container config directory.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_SERVER_STATE_DIR=/var/lib/safarsuite/local-server", StringComparison.Ordinal), "Bootstrap environment template should expose the shared local-server state directory.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_DB_IMAGE=postgres:16-alpine", StringComparison.Ordinal), "Bootstrap environment template should default the local database image.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_APP_IMAGE=ghcr.io/danionwheels/localserver:{{SAFARSUITE_APP_VERSION}}", StringComparison.Ordinal), "Bootstrap environment template should default the app image to the verified SafarSuite runtime image.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_MODULE_GATEWAY_URL=https://local-api:8080", StringComparison.Ordinal), "Bootstrap environment template should default the app gateway to the HTTPS local API.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_ACCESS_KEY=change-me-before-start", StringComparison.Ordinal), "Bootstrap environment template should carry the local API access key placeholder.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_BASE_URL=https://local-api:8080", StringComparison.Ordinal), "Bootstrap environment template should default app-to-provider Local API traffic to HTTPS.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_TLS_MODE=GeneratedLocalCa", StringComparison.Ordinal), "Bootstrap environment template should default Local API TLS automation to generated local CA mode.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=https://0.0.0.0:8080", StringComparison.Ordinal), "Bootstrap environment template should bind the local API over HTTPS by default.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_CERTIFICATE_PATH=", StringComparison.Ordinal), "Bootstrap environment template should expose the local API TLS certificate path.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH=", StringComparison.Ordinal), "Bootstrap environment template should expose the trusted local API CA certificate path.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES=local-api,localhost", StringComparison.Ordinal), "Bootstrap environment template should expose local API certificate DNS names.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES=127.0.0.1", StringComparison.Ordinal), "Bootstrap environment template should expose local API certificate IP addresses.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_PAIRING_MODE=ManagerApproval", StringComparison.Ordinal), "Bootstrap environment template should expose the LocalServer pairing mode.");
Require(environmentTemplateArtifact.Content.Contains("LocalServer__Pairing__RequestExpiresInHours=24", StringComparison.Ordinal), "Bootstrap environment template should expose the default pairing request expiry window.");
Require(environmentTemplateArtifact.Content.Contains("SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET=change-me-before-start", StringComparison.Ordinal), "Bootstrap environment template should carry the local manager session signing secret placeholder.");
Require(environmentTemplateArtifact.Content.Contains("DeploymentSecrets__Provider=Environment", StringComparison.Ordinal), "Bootstrap environment template should select environment-backed app runtime secrets.");
Require(environmentTemplateArtifact.Content.Contains("ActivationSigning__SigningKeyId=app-activation-smoke", StringComparison.Ordinal), "Bootstrap environment template should carry the app activation signing key id.");
Require(environmentTemplateArtifact.Content.Contains("ActivationSigning__PublicKeyPem=\"-----BEGIN PUBLIC KEY-----\\n", StringComparison.Ordinal), "Bootstrap environment template should carry the escaped app activation public key.");
Require(!environmentTemplateArtifact.Content.Contains("{{SAFARSUITE_APP_ACTIVATION_", StringComparison.Ordinal), "Bootstrap environment template should not leave app activation signing placeholders unresolved.");
Require(environmentTemplateArtifact.Content.Contains("DeviceCredentials__SigningSecret=change-me-before-start", StringComparison.Ordinal), "Bootstrap environment template should carry the app device signing secret placeholder.");
Require(environmentTemplateArtifact.Content.Contains("DeviceCredentials__ExpiresInDays=3650", StringComparison.Ordinal), "Bootstrap environment template should carry the app device credential lifetime.");
Require(environmentTemplateArtifact.Content.Contains("DeviceCredentials__RefreshWindowDays=30", StringComparison.Ordinal), "Bootstrap environment template should carry the app device credential refresh window.");
Require(environmentTemplateArtifact.Content.Contains("DeviceCredentials__RefreshGraceHours=24", StringComparison.Ordinal), "Bootstrap environment template should carry the app device credential refresh grace.");
Require(environmentTemplateArtifact.Content.Contains("UserSessions__SigningSecret=change-me-before-start", StringComparison.Ordinal), "Bootstrap environment template should carry the app session signing secret placeholder.");
Require(environmentTemplateArtifact.Content.Contains("FirstManagerBootstrap__AllowSetupCodeFallback=false", StringComparison.Ordinal), "Bootstrap environment template should keep first-manager fallback setup codes disabled.");
Require(runtimeManifestArtifact.Content.Contains("\"serviceName\": \"safarsuite-app\"", StringComparison.Ordinal), "Bootstrap runtime manifest should include the SafarSuite app service.");
Require(runtimeManifestArtifact.Content.Contains("\"internalBaseUrl\": \"https://local-api:8080\"", StringComparison.Ordinal), "Bootstrap runtime manifest should describe the Local API over HTTPS by default.");
Require(runtimeManifestArtifact.Content.Contains("\"composeProfile\": \"app-runtime\"", StringComparison.Ordinal), "Bootstrap runtime manifest should describe the SafarSuite app profile.");
Require(runtimeManifestArtifact.Content.Contains("\"publishedPortEnvironmentVariable\": \"SAFARSUITE_APP_HTTP_PORT\"", StringComparison.Ordinal), "Bootstrap runtime manifest should describe the app port variable.");
Require(bootstrapPackage.Endpoints.DiagnosticsUrl?.EndsWith("/diagnostics", StringComparison.Ordinal) == true, "Bootstrap package should include the diagnostics endpoint.");
Require(bootstrapPackage.InstallCommand.Contains("SAFARSUITE_APP_VERSION", StringComparison.Ordinal), "Bootstrap install command should carry SafarSuite app version.");
Require(bootstrapPackage.InstallCommand.Contains("SAFARSUITE_APP_ACTIVATION_SIGNING_KEY_ID", StringComparison.Ordinal), "Bootstrap install command should carry the app activation signing key id.");
Require(bootstrapPackage.InstallCommand.Contains("SAFARSUITE_APP_ACTIVATION_PUBLIC_KEY_PEM", StringComparison.Ordinal), "Bootstrap install command should carry the app activation public key.");
Require(bootstrapPackage.InstallCommand.Contains("SAFARSUITE_CLIENT_DEPLOYMENT_MODE", StringComparison.Ordinal), "Bootstrap install command should carry client deployment mode.");
Require(bootstrapPackage.BundleContentType == ControlCloudLocalServerBootstrapPackageFormat.BundleContentType, "Bootstrap package should expose the signed bundle content type.");
Require(bootstrapPackage.BundleFileName.StartsWith("safarsuite-bootstrap-", StringComparison.Ordinal), "Bootstrap package should use a stable signed bundle filename prefix.");
Require(bootstrapPackage.BundleSha256 == ComputeSha256(JsonSerializer.Serialize(
    bootstrapPackage.SignedBundle,
    new JsonSerializerOptions(JsonSerializerDefaults.Web))), "Bootstrap bundle checksum should match the downloadable signed bundle.");
Require(bootstrapPackage.SignedBundle.Signature.PayloadSha256 == ComputeSha256(bootstrapPackage.SignedBundle.PayloadJson), "Bootstrap signature payload checksum should match the signed payload JSON.");
RequireLocalServerRuntimeImageContract();

var bootstrapRegistrationHandler = new RegisterInstallationFromBootstrapBundleHandler(
    bootstrapVerifier,
    bootstrapConfigurationStore,
    registrationHandler,
    clock);
var registrationResult = await bootstrapRegistrationHandler.HandleAsync(
    new RegisterInstallationFromBootstrapBundleCommand(
        bootstrapPackage.SignedBundle,
        installationId));

Require(registrationResult.IsSuccess, "Local server should verify a signed bootstrap bundle and register with Control Cloud.");
Require(registrationHttpHandler.LastRequest?.SetupToken == bootstrapPackage.SetupToken, "Registration should send the setup token from the signed bootstrap bundle.");
var registeredInstallation = registrationResult.Registration
    ?? throw new InvalidOperationException("Registration response should exist.");
Require(registeredInstallation.DeploymentProfile?.ClientDeploymentMode == SafarSuiteClientDeploymentModes.CloudSyncMultiBranch, "Registration response should carry the deployment profile.");
var savedBootstrapConfiguration = await bootstrapConfigurationStore.GetCurrentAsync()
    ?? throw new InvalidOperationException("Verified bootstrap configuration should be persisted.");
Require(savedBootstrapConfiguration.RegistrationStatus == LocalServerBootstrapRegistrationStatuses.Registered, "Bootstrap configuration should record a successful registration.");
Require(savedBootstrapConfiguration.SignatureKeyId == "bootstrap-smoke", "Bootstrap configuration should retain the trusted signing key id.");
Require(savedBootstrapConfiguration.Endpoints.HeartbeatUrl.EndsWith("/heartbeat", StringComparison.Ordinal), "Bootstrap configuration should carry the heartbeat endpoint.");
Require(savedBootstrapConfiguration.DeploymentProfile.SyncTopologyId == "sync-main", "Bootstrap configuration should carry the signed sync topology.");

var firstManagerPairingRequestId = Guid.NewGuid();
var firstManagerDeviceId = Guid.NewGuid();
var firstManagerDevice = new LocalServerDevicePairingRecord(
    firstManagerPairingRequestId,
    firstManagerDeviceId,
    clientId,
    installationId,
    LocalServerPairingFormats.DevicePairingRequestVersion,
    "Owner laptop",
    "first-manager-device-public-key",
    ComputeSha256("first-manager-device-public-key"),
    "owner-laptop-fingerprint",
    "owner",
    "smoke-app",
    "server-nonce",
    "client-nonce",
    LocalServerDevicePairingRecordStatuses.Pending,
    LocalServerDevicePairingRecordStatuses.Pending,
    clock.UtcNow,
    clock.UtcNow.AddHours(24),
    clock.UtcNow);
await pairingStore.SaveAsync(firstManagerDevice);
var firstManagerCloudInstallations = new InMemoryClientInstallationRepository();
await firstManagerCloudInstallations.AddAsync(ControlCloudClientInstallation.Register(
    clientId,
    installationId,
    clock.UtcNow,
    ControlCloudInstallationDeploymentProfile.Create(
        installationId,
        ControlCloudBootstrapModes.OnlineBootstrap,
        SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
        "hq-main",
        SafarSuiteDeploymentSiteRoles.Hq,
        parentSiteId: null,
        branchCode: "HQ",
        syncTopologyId: "sync-main")));
var cloudFirstManagerSetupTokenIssueRepository = new InMemoryFirstManagerSetupTokenIssueRepository();
var cloudFirstManagerSetupTokenHandler = new IssueLocalServerFirstManagerSetupTokenHandler(
    firstManagerCloudInstallations,
    new HmacControlCloudFirstManagerSetupTokenSigner(
        new ControlCloudEntitlementSigningOptions
        {
            ActiveKeyId = "bootstrap-smoke",
            SigningKeys =
            [
                new ControlCloudEntitlementSigningKeyOptions
                {
                    KeyId = "bootstrap-smoke",
                    Secret = "bootstrap-signing-secret-change-before-cloud"
                }
            ]
        }),
    cloudFirstManagerSetupTokenIssueRepository,
    new InMemoryClientPortalAuditRecorder(),
    new FixedControlCloudClock(clock.UtcNow));
var cloudFirstManagerSetupTokenResult = await cloudFirstManagerSetupTokenHandler.HandleAsync(
    new IssueLocalServerFirstManagerSetupTokenCommand(
        clientId,
        installationId,
        firstManagerPairingRequestId,
        "Owner",
        "owner@safarsuite.local",
        "Smoke",
        ExpiresInHours: 24));

Require(cloudFirstManagerSetupTokenResult.IsSuccess, "Control Cloud should issue a first-manager setup token for a registered installation.");
var signedFirstManagerToken = cloudFirstManagerSetupTokenResult.Response!.SignedToken;
var cloudFirstManagerIssue = await cloudFirstManagerSetupTokenIssueRepository.GetByTokenIdAsync(
    signedFirstManagerToken.Payload.TokenId);
var firstManagerTokenVerification = firstManagerSetupTokenVerifier.Verify(
    signedFirstManagerToken,
    clock.UtcNow);
var badFirstManagerTokenVerification = firstManagerSetupTokenVerifier.Verify(
    signedFirstManagerToken with
    {
        Signature = signedFirstManagerToken.Signature with { Value = "invalid-first-manager-signature" }
    },
    clock.UtcNow);
var expiredFirstManagerTokenVerification = firstManagerSetupTokenVerifier.Verify(
    CreateSignedFirstManagerSetupToken(
        clientId,
        installationId,
        firstManagerPairingRequestId,
        clock.UtcNow.AddHours(-2),
        "bootstrap-smoke",
        "bootstrap-signing-secret-change-before-cloud",
        expiresAtUtc: clock.UtcNow.AddHours(-1)),
    clock.UtcNow);
var missingFirstManagerTokenVerification = firstManagerSetupTokenVerifier.Verify(
    null,
    clock.UtcNow);

Require(firstManagerTokenVerification.IsSuccess, "Signed first-manager setup token should verify with bootstrap trust.");
Require(cloudFirstManagerIssue is not null, "Control Cloud should persist first-manager setup token issue metadata.");
Require(cloudFirstManagerIssue?.PayloadSha256 == signedFirstManagerToken.Signature.PayloadSha256, "Persisted first-manager issue should retain the signed payload hash.");
Require(firstManagerTokenVerification.Payload?.PendingDeviceRequestId == firstManagerPairingRequestId, "First-manager setup token should bind the pending device request id.");
Require(badFirstManagerTokenVerification.FailureCode == "SignatureInvalid", "Bad first-manager setup token signature should be rejected.");
Require(expiredFirstManagerTokenVerification.FailureCode == "FirstManagerSetupTokenExpired", "Expired first-manager setup token should be rejected.");
Require(missingFirstManagerTokenVerification.FailureCode == "FirstManagerSetupTokenRequired", "Missing first-manager setup token should be rejected.");
var firstManagerDeviceCredentialId = Guid.NewGuid();
var signedFirstManagerDeviceCredential = deviceCredentialService.Issue(
    firstManagerDevice,
    firstManagerDeviceCredentialId,
    "FirstManagerDevice",
    clock.UtcNow);
var firstManagerDeviceCredential = signedFirstManagerDeviceCredential.CompactToken;
var firstManagerDeviceCredentialVerification = deviceCredentialService.Verify(
    firstManagerDeviceCredential,
    clock.UtcNow);
var approvedFirstManagerDevice = firstManagerDevice.Approve(
    "first-manager:owner@safarsuite.local",
    "FirstManagerDevice",
    firstManagerDeviceCredentialId.ToString("D"),
    ComputeSha256(firstManagerDeviceCredential),
    clock.UtcNow);
var firstManagerWriteResult = await pairingStore.SaveDeviceAndFirstManagerSetupTokenConsumptionAsync(
    approvedFirstManagerDevice,
    new LocalServerFirstManagerSetupTokenConsumptionRecord(
        signedFirstManagerToken.Payload.TokenId,
        clientId,
        installationId,
        firstManagerPairingRequestId,
        firstManagerDeviceId,
        signedFirstManagerToken.Payload.ManagerDisplayName,
        signedFirstManagerToken.Payload.ManagerEmail,
        signedFirstManagerToken.Payload.CreatedBy,
        signedFirstManagerToken.Signature.KeyId,
        signedFirstManagerToken.Signature.PayloadSha256,
        signedFirstManagerToken.Payload.IssuedAtUtc,
        signedFirstManagerToken.Payload.ExpiresAtUtc,
        clock.UtcNow));
var consumedFirstManagerToken = await pairingStore.GetFirstManagerSetupTokenConsumptionAsync(
    signedFirstManagerToken.Payload.TokenId);
var persistedFirstManagerDevice = await pairingStore.GetByDeviceIdAsync(firstManagerDeviceId);

Require(firstManagerWriteResult.Succeeded, "First-manager setup token should atomically approve the device and record consumption.");
Require(consumedFirstManagerToken is not null, "Consumed first-manager setup token should be persisted.");
Require(persistedFirstManagerDevice?.DeviceStatus == LocalServerDevicePairingRecordStatuses.Approved, "First-manager setup token should approve the pending device.");
Require(firstManagerDeviceCredentialVerification.IsSuccess, "Signed first-manager device credential should verify.");
Require(firstManagerDeviceCredentialVerification.Payload?.DeviceId == firstManagerDeviceId, "Signed first-manager device credential should bind the approved device.");

var badBootstrapBundle = bootstrapPackage.SignedBundle with
{
    Signature = bootstrapPackage.SignedBundle.Signature with { Value = "invalid-bootstrap-signature" }
};
var badBootstrapRegistrationResult = await bootstrapRegistrationHandler.HandleAsync(
    new RegisterInstallationFromBootstrapBundleCommand(
        badBootstrapBundle,
        installationId));

Require(!badBootstrapRegistrationResult.IsSuccess, "Bad bootstrap signature should be rejected before registration.");
Require(badBootstrapRegistrationResult.FailureCode == "SignatureInvalid", "Bad bootstrap signature failure code should be SignatureInvalid.");

var pullClient = new HttpControlCloudEntitlementBundleClient(
    new HttpClient(new StaticBundleHttpMessageHandler(signedBundle))
    {
        BaseAddress = new Uri("https://control-cloud.local")
    },
    controlCloudOptions);
var pullHandler = new PullEntitlementFromControlCloudHandler(
    pullClient,
    importHandler,
    clock);
var pullResult = await pullHandler.HandleAsync(
    new PullEntitlementFromControlCloudCommand(clientId, installationId));

Require(pullResult.IsSuccess, "Signed entitlement bundle should pull and import from Control Cloud.");
Require(pullResult.Entitlement!.EntitlementVersion == 100, "Pulled entitlement version should be 100.");

var heartbeatHttpHandler = new StaticHeartbeatHttpMessageHandler();
var heartbeatClient = new HttpControlCloudHeartbeatClient(
    new HttpClient(heartbeatHttpHandler)
    {
        BaseAddress = new Uri("https://control-cloud.local")
    },
    controlCloudOptions);
var heartbeatHandler = new ReportHeartbeatToControlCloudHandler(
    heartbeatClient,
    cache,
    trustStateStore,
    new LocalServerEntitlementPolicy(),
    clock);
var heartbeatFromBootstrapHandler = new ReportHeartbeatFromBootstrapConfigurationHandler(
    bootstrapConfigurationStore,
    heartbeatHandler);
var heartbeatResult = await heartbeatFromBootstrapHandler.HandleAsync(
    new ReportHeartbeatFromBootstrapConfigurationCommand(
        Detail: "Local entitlement smoke heartbeat."));

Require(heartbeatResult.IsSuccess, "Heartbeat should report current entitlement state to Control Cloud.");
Require(heartbeatResult.Heartbeat!.HeartbeatStatus == "Received", "Heartbeat status should be Received.");
Require(heartbeatResult.Heartbeat.LicenseStatus == LocalServerEntitlementAccessStates.Active, "Heartbeat license status should be Active.");
Require(heartbeatHttpHandler.LastRequest?.EntitlementVersion == 100, "Heartbeat should report cached entitlement version 100.");
var postHeartbeatTrustState = await trustStateStore.GetAsync(installationId)
    ?? throw new InvalidOperationException("Trust state should exist after heartbeat.");
Require(postHeartbeatTrustState.LastSuccessfulCloudTimeUtc == heartbeatResult.Heartbeat.ReceivedAtUtc, "Trust state should record the last trusted Control Cloud time.");

var badSignatureBundle = signedBundle with
{
    Signature = signedBundle.Signature with { Value = "invalid-signature" }
};
var badSignatureResult = await importHandler.HandleAsync(
    new ImportSignedEntitlementBundleCommand(installationId, badSignatureBundle));

Require(!badSignatureResult.IsSuccess, "Bad signature should be rejected.");
Require(badSignatureResult.FailureCode == "SignatureInvalid", "Bad signature failure code should be SignatureInvalid.");

var olderBundle = CreateSignedBundle(
    clientId,
    installationId,
    entitlementVersion: 99);
var olderImportResult = await importHandler.HandleAsync(
    new ImportSignedEntitlementBundleCommand(installationId, olderBundle));

Require(!olderImportResult.IsSuccess, "Older entitlement version should be rejected.");
Require(olderImportResult.FailureCode == "EntitlementVersionRejected", "Older version failure code should be EntitlementVersionRejected.");

var offlineRenewalBundle = CreateSignedBundle(
    clientId,
    installationId,
    entitlementVersion: 101,
    paidUntil: new DateOnly(2026, 9, 30),
    warningStartsAt: new DateOnly(2026, 9, 23),
    graceUntil: new DateOnly(2026, 10, 7),
    offlineValidUntil: new DateOnly(2026, 10, 14));
var offlineRenewalFile = new ControlCloudOfflineRenewalFileResponse(
    ControlCloudOfflineRenewalFileFormat.Version,
    Guid.NewGuid(),
    clientId,
    installationId,
    clock.UtcNow,
    "Smoke",
    "Offline renewal smoke import",
    offlineRenewalBundle);
var offlineRenewalJson = JsonSerializer.Serialize(
    offlineRenewalFile,
    new JsonSerializerOptions(JsonSerializerDefaults.Web));
var offlineRenewalResult = await offlineRenewalImportHandler.HandleAsync(
    new ImportOfflineRenewalFileCommand(installationId, offlineRenewalJson));

Require(offlineRenewalResult.IsSuccess, "Offline renewal file should import through the signed bundle verifier.");
var offlineRenewedEntitlement = offlineRenewalResult.Entitlement
    ?? throw new InvalidOperationException("Offline renewal entitlement should exist.");
Require(offlineRenewedEntitlement.EntitlementVersion == 101, "Offline renewal should update cached entitlement version to 101.");
Require(offlineRenewedEntitlement.PaidUntil == new DateOnly(2026, 9, 30), "Offline renewal should extend paid-until date.");
var replayedOfflineRenewalResult = await offlineRenewalImportHandler.HandleAsync(
    new ImportOfflineRenewalFileCommand(installationId, offlineRenewalJson));

Require(!replayedOfflineRenewalResult.IsSuccess, "Replaying the same offline renewal file should be rejected.");
Require(replayedOfflineRenewalResult.FailureCode == "EntitlementReplayRejected", "Replay failure code should be EntitlementReplayRejected.");

var importAuditRecords = await importAuditStore.GetRecentAsync(installationId, 10);
Require(importAuditRecords.Count >= 5, "Local import audit should retain accepted and rejected entitlement imports.");
Require(importAuditRecords.Any(record =>
    record.ImportSource == LocalServerEntitlementImportSources.ControlCloudPull
    && record.ResultStatus == LocalServerEntitlementImportResultStatuses.Accepted
    && record.EntitlementVersion == 100), "Local import audit should record accepted Control Cloud pulls.");
Require(importAuditRecords.Any(record =>
    record.ImportSource == LocalServerEntitlementImportSources.DirectBundle
    && record.ResultStatus == LocalServerEntitlementImportResultStatuses.Rejected
    && record.FailureCode == "SignatureInvalid"), "Local import audit should record rejected direct bundle imports.");
Require(importAuditRecords.Any(record =>
    record.ImportSource == LocalServerEntitlementImportSources.OfflineRenewalFile
    && record.ResultStatus == LocalServerEntitlementImportResultStatuses.Accepted
    && record.EntitlementVersion == 101), "Local import audit should record accepted offline renewal imports.");
Require(importAuditRecords.Any(record =>
    record.ImportSource == LocalServerEntitlementImportSources.OfflineRenewalFile
    && record.ResultStatus == LocalServerEntitlementImportResultStatuses.Rejected
    && record.FailureCode == "EntitlementReplayRejected"), "Local import audit should record rejected offline renewal replays.");

var activeDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 15));
Require(activeDecision.IsAllowed, "Accounting should be allowed during active period.");
Require(activeDecision.AccessState == LocalServerEntitlementAccessStates.Active, "Accounting should be active before warning.");

var warningDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 23));
Require(warningDecision.IsAllowed, "Accounting should be allowed during warning.");
Require(warningDecision.AccessState == LocalServerEntitlementAccessStates.Warning, "Accounting should enter warning state.");

var graceDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 10, 5));
Require(graceDecision.IsAllowed, "Accounting should be allowed during grace.");
Require(graceDecision.AccessState == LocalServerEntitlementAccessStates.Grace, "Accounting should enter grace state.");

var restrictedDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 10, 8));
Require(!restrictedDecision.IsAllowed, "Accounting should be restricted after grace.");
Require(restrictedDecision.AccessState == LocalServerEntitlementAccessStates.Restricted, "Accounting should enter restricted state after grace.");

var expiredDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 10, 15));
Require(!expiredDecision.IsAllowed, "Accounting should be denied after offline validity.");
Require(expiredDecision.AccessState == LocalServerEntitlementAccessStates.Expired, "Accounting should be expired after offline validity.");

var disabledDecision = await EvaluateAsync("Reports", new DateOnly(2026, 9, 15));
Require(!disabledDecision.IsAllowed, "Reports should be denied when disabled.");
Require(disabledDecision.AccessState == LocalServerEntitlementAccessStates.ModuleDisabled, "Reports should be module-disabled.");

var moduleGatewayAllowedResult = await moduleGatewayHandler.HandleAsync(
    new EvaluateModuleAccessGatewayCommand(
        installationId,
        "Accounting",
        new DateOnly(2026, 9, 15),
        RequestedBy: "safarsuite-app-smoke"));
Require(moduleGatewayAllowedResult.IsSuccess, "Module gateway should return an access response for enabled modules.");
Require(moduleGatewayAllowedResult.Access!.FormatVersion == LocalServerModuleGatewayFormat.Version, "Module gateway should return the shared response format.");
Require(moduleGatewayAllowedResult.Access.IsAllowed, "Module gateway should allow Accounting during active period.");

var moduleGatewayDisabledResult = await moduleGatewayHandler.HandleAsync(
    new EvaluateModuleAccessGatewayCommand(
        installationId,
        "Reports",
        new DateOnly(2026, 9, 15),
        RequestedBy: "safarsuite-app-smoke"));
Require(moduleGatewayDisabledResult.IsSuccess, "Module gateway should return an access response for disabled modules.");
Require(!moduleGatewayDisabledResult.Access!.IsAllowed, "Module gateway should deny disabled module access.");
Require(moduleGatewayDisabledResult.Access.AccessState == LocalServerEntitlementAccessStates.ModuleDisabled, "Module gateway should expose module-disabled state.");

var moduleGatewayExpiredResult = await moduleGatewayHandler.HandleAsync(
    new EvaluateModuleAccessGatewayCommand(
        installationId,
        "Accounting",
        new DateOnly(2026, 10, 15),
        RequestedBy: "safarsuite-app-smoke"));
Require(moduleGatewayExpiredResult.IsSuccess, "Module gateway should return an access response for expired state.");
Require(moduleGatewayExpiredResult.Access!.AccessState == LocalServerEntitlementAccessStates.Expired, "Module gateway should expose expired state.");

var moduleGatewayMissingModuleResult = await moduleGatewayHandler.HandleAsync(
    new EvaluateModuleAccessGatewayCommand(
        installationId,
        "",
        new DateOnly(2026, 9, 15),
        RequestedBy: "safarsuite-app-smoke"));
Require(!moduleGatewayMissingModuleResult.IsSuccess, "Module gateway should reject missing module code.");
Require(moduleGatewayMissingModuleResult.FailureCode == "ModuleCodeRequired", "Module gateway missing module failure code should be stable.");

clock.UtcNow = clock.UtcNow.AddMinutes(-30);
_ = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 15));
var clockRollbackTrustState = await trustStateStore.GetAsync(installationId)
    ?? throw new InvalidOperationException("Trust state should exist after clock rollback check.");
Require(clockRollbackTrustState.ClockMovedBackwards, "Trust state should flag local clock rollback.");

var diagnosticsResult = await diagnosticsBundleHandler.HandleAsync(
    new CreateLocalServerDiagnosticsBundleCommand(
        clientId,
        installationId,
        "local-server-smoke",
        "Smoke",
        "Diagnostics smoke export",
        "smoke-host",
        "smoke-os",
        new DateOnly(2026, 9, 15),
        new LocalServerDiagnosticRuntimeResponse(
            "local-server-smoke",
            "dev",
            "diagnostic-smoke",
            "DockerCompose",
            "smoke-host",
            "smoke-os",
            "x64",
            4,
            DockerAvailable: true,
            DockerVersion: "Docker 27 smoke",
            DockerComposeAvailable: true,
            DockerComposeVersion: "Docker Compose v2 smoke"),
        new LocalServerDiagnosticBootstrapResponse(
            "/etc/safarsuite/local-server",
            "Configured",
            "bootstrap-sha-smoke",
            "compose-sha-smoke",
            "env-sha-smoke",
            LastRegistrationAttemptUtc: new DateTimeOffset(2026, 8, 1, 10, 0, 1, TimeSpan.Zero),
            LastRegistrationSucceededAtUtc: new DateTimeOffset(2026, 8, 1, 10, 0, 1, TimeSpan.Zero),
            LastHeartbeatSentAtUtc: heartbeatResult.Heartbeat.ReceivedAtUtc,
            LastEntitlementPullAtUtc: pullResult.PulledAtUtc),
        [
            new LocalServerDiagnosticServiceResponse(
                "safarsuite-local-api",
                "Running",
                "Running",
                "safarsuite-local-api-1",
                new DateTimeOffset(2026, 8, 1, 10, 1, 0, TimeSpan.Zero),
                "API service is healthy."),
            new LocalServerDiagnosticServiceResponse(
                "safarsuite-local-agent",
                "Running",
                "Running",
                "safarsuite-local-agent-1",
                new DateTimeOffset(2026, 8, 1, 10, 1, 0, TimeSpan.Zero),
                "Agent service is healthy.")
        ],
        [
            new LocalServerDiagnosticRecentErrorResponse(
                "local-agent",
                "Warning",
                "Previous entitlement pull retried once during smoke.",
                new DateTimeOffset(2026, 8, 1, 10, 2, 0, TimeSpan.Zero))
        ],
        DeploymentProfile: new LocalServerDeploymentProfileResponse(
            ControlCloudBootstrapModes.OnlineBootstrap,
            SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
            "hq-main",
            SafarSuiteDeploymentSiteRoles.Hq,
            ParentSiteId: null,
            BranchCode: "HQ",
            SyncTopologyId: "sync-main")));

Require(diagnosticsResult.IsSuccess, "Diagnostics bundle should be created from local entitlement state.");
var diagnosticsBundle = diagnosticsResult.Bundle
    ?? throw new InvalidOperationException("Diagnostics bundle should exist.");
Require(diagnosticsBundle.CachedEntitlement.EntitlementVersion == 101, "Diagnostics should include cached entitlement version 101.");
Require(diagnosticsBundle.TrustState?.ClockMovedBackwards == true, "Diagnostics should include clock rollback trust warning.");
Require(diagnosticsBundle.Runtime?.DockerAvailable == true, "Diagnostics should include Docker availability.");
Require(diagnosticsBundle.Bootstrap?.ComposeFileSha256 == "compose-sha-smoke", "Diagnostics should include compose template checksum.");
Require(diagnosticsBundle.Services?.Count == 2, "Diagnostics should include runtime service status.");
Require(diagnosticsBundle.RecentErrors?.Count == 1, "Diagnostics should include recent runtime errors.");
Require(diagnosticsBundle.ImportAudit?.Count >= 5, "Diagnostics should include recent local import audit records.");
Require(diagnosticsBundle.DeploymentProfile?.SiteId == "hq-main", "Diagnostics should include deployment profile site identity.");
Require(diagnosticsBundle.Checks.Any(check => check.Code == "clock-rollback" && check.Status == "Warning"), "Diagnostics should include a clock rollback warning check.");
Require(diagnosticsBundle.Checks.Any(check => check.Code == "service-status" && check.Status == "Ok"), "Diagnostics should include an OK service status check.");
Require(diagnosticsBundle.Checks.Any(check => check.Code == "recent-errors" && check.Status == "Warning"), "Diagnostics should flag recent runtime errors.");
Require(diagnosticsBundle.Checks.Any(check => check.Code == "import-audit" && check.Status == "Warning"), "Diagnostics should flag rejected local import audit records.");

var diagnosticsHttpHandler = new StaticDiagnosticsHttpMessageHandler();
var diagnosticsUploadClient = new HttpControlCloudDiagnosticsClient(
    new HttpClient(diagnosticsHttpHandler)
    {
        BaseAddress = new Uri("https://control-cloud.local")
    },
    controlCloudOptions);
var diagnosticsUploadHandler = new UploadDiagnosticsToControlCloudHandler(diagnosticsUploadClient);
var diagnosticsUploadResult = await diagnosticsUploadHandler.HandleAsync(
    new UploadDiagnosticsToControlCloudCommand(
        diagnosticsBundle,
        "Smoke",
        "Diagnostics smoke upload"));

Require(diagnosticsUploadResult.IsSuccess, "Diagnostics bundle should upload to Control Cloud.");
Require(diagnosticsHttpHandler.LastRequest?.Bundle.DiagnosticBundleId == diagnosticsBundle.DiagnosticBundleId, "Diagnostics upload should send the generated bundle.");

var commandSigner = new HmacControlCloudInstallationCommandSigner(
    new ControlCloudEntitlementSigningOptions
    {
        ActiveKeyId = "local-entitlement-dev",
        SigningKeys =
        [
            new ControlCloudEntitlementSigningKeyOptions
            {
                KeyId = "local-entitlement-dev",
                Secret = "local-entitlement-signing-secret-change-before-cloud"
            }
        ]
    });
var diagnosticCommandPayload = CreateSupportCommandPayload(
    clientId,
    installationId,
    LocalServerInstallationCommandTypes.RequestDiagnostics,
    "Smoke",
    "Command diagnostics smoke",
    clock.UtcNow);
var diagnosticCommand = CreateCommandResponse(
    commandSigner,
    clientId,
    installationId,
    commandVersion: 1,
    LocalServerInstallationCommandTypes.RequestDiagnostics,
    diagnosticCommandPayload,
    clock.UtcNow.AddTicks(7),
    ReorderSupportCommandPayload(diagnosticCommandPayload));
var refreshCommandBundle = CreateSignedBundle(
    clientId,
    installationId,
    entitlementVersion: 102);
var refreshCommand = CreateCommandResponse(
    commandSigner,
    clientId,
    installationId,
    commandVersion: 2,
    LocalServerInstallationCommandTypes.RefreshEntitlement,
    CreateSupportCommandPayload(
        clientId,
        installationId,
        LocalServerInstallationCommandTypes.RefreshEntitlement,
        "Smoke",
        "Command entitlement refresh smoke",
        clock.UtcNow),
    clock.UtcNow.AddMinutes(1));
var appActivationIssueId = Guid.NewGuid();
var appServerInstallationId = Guid.NewGuid();
var appActivationRevocationCommand = CreateCommandResponse(
    commandSigner,
    clientId,
    installationId,
    commandVersion: 3,
    LocalServerInstallationCommandTypes.RevokeAppActivation,
    CreateAppActivationRevocationCommandPayload(
        clientId,
        installationId,
        appServerInstallationId,
        appActivationIssueId,
        activationRequestId: Guid.NewGuid(),
        clock.UtcNow.AddMinutes(2),
        "Smoke",
        "Rotate app activation mapping"),
    clock.UtcNow.AddMinutes(2));
var commandClient = new StaticInstallationCommandClient(
    installationId,
    [diagnosticCommand, refreshCommand, appActivationRevocationCommand]);
var appActivationRevocationStore = new FileLocalServerAppActivationRevocationStore(
    new LocalServerCommandOptions
    {
        AppActivationRevocationStorePath = appActivationRevocationPath
    });
var commandDiagnosticsHttpHandler = new StaticDiagnosticsHttpMessageHandler();
var commandDiagnosticsUploadHandler = new UploadDiagnosticsToControlCloudHandler(
    new HttpControlCloudDiagnosticsClient(
        new HttpClient(commandDiagnosticsHttpHandler)
        {
            BaseAddress = new Uri("https://control-cloud.local")
        },
        controlCloudOptions));
var commandPullHandler = new PullEntitlementFromControlCloudHandler(
    new HttpControlCloudEntitlementBundleClient(
        new HttpClient(new StaticBundleHttpMessageHandler(refreshCommandBundle))
        {
            BaseAddress = new Uri("https://control-cloud.local")
        },
        controlCloudOptions),
    importHandler,
    clock);
var commandProcessor = new ProcessInstallationCommandsHandler(
    commandClient,
    new HmacLocalServerInstallationCommandVerifier(trustOptions),
    appActivationRevocationStore,
    clock,
    commandPullHandler,
    diagnosticsBundleHandler,
    commandDiagnosticsUploadHandler);
var commandProcessorFromBootstrap = new ProcessInstallationCommandsFromBootstrapConfigurationHandler(
    bootstrapConfigurationStore,
    commandProcessor);
var commandProcessingResult = await commandProcessorFromBootstrap.HandleAsync(
    new ProcessInstallationCommandsFromBootstrapConfigurationCommand());

Require(commandProcessingResult.IsSuccess, "Local-server command processing should complete.");
Require(commandProcessingResult.PendingCommandCount == 3, "Command processor should pull three pending commands.");
Require(commandProcessingResult.AppliedCount == 3, "Diagnostics, refresh, and app activation revocation commands should be applied.");
Require(commandProcessingResult.Commands.All(command => command.Acknowledged), "Processed commands should be acknowledged to Control Cloud.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Reason == "Command diagnostics smoke", "Diagnostics command should upload with the command reason.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Runtime?.RuntimeMode == "DockerCompose", "Command diagnostics should collect runtime mode from the bootstrap manifest.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Runtime?.DockerAvailable == true, "Command diagnostics should probe Docker availability.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Runtime?.DockerComposeAvailable == true, "Command diagnostics should probe Docker Compose availability.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Bootstrap?.ComposeFileSha256 is not null, "Command diagnostics should collect compose artifact checksum from the signed bootstrap payload.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Services?.Any(service =>
    service.ServiceName == "local-api"
    && service.ExpectedState == "Running"
    && service.CurrentState == "Running") == true, "Command diagnostics should include live Compose state for the local API service.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.Services?.Any(service =>
    service.ServiceName == "safarsuite-app"
    && service.ExpectedState == "ProfileDisabled"
    && service.CurrentState == "ProfileDisabled"
    && service.Detail?.Contains("SAFARSUITE_APP_IMAGE", StringComparison.Ordinal) == true
    && service.Detail?.Contains("SAFARSUITE_APP_HTTP_PORT", StringComparison.Ordinal) == true
    && service.Detail?.Contains("http://safarsuite-app:5280/health", StringComparison.Ordinal) == true) == true, "Command diagnostics should include the optional SafarSuite app runtime service slot with manifest intent.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.RecentErrors?.Any(error =>
    error.Source == "local-api"
    && error.Severity == "Error"
    && error.Message.Contains("module gateway retry failed", StringComparison.OrdinalIgnoreCase)) == true, "Command diagnostics should include recent runtime log errors.");
Require(commandDiagnosticsHttpHandler.LastRequest?.Bundle.RecentErrors?.Any(error =>
    error.Source == "local-worker"
    && error.Severity == "Warning"
    && error.Message.Contains("heartbeat delayed", StringComparison.OrdinalIgnoreCase)) == true, "Command diagnostics should include recent runtime log warnings.");
Require(commandClient.Acknowledgements.Count == 3, "Command client should record all three acknowledgements.");
Require(commandClient.Acknowledgements.Any(ack => ack.Value.ResultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Applied), "At least one command acknowledgement should be applied.");
var recordedAppActivationRevocation = await appActivationRevocationStore.GetByActivationIssueIdAsync(appActivationIssueId);
Require(recordedAppActivationRevocation is not null, "App activation revocation command should be recorded locally.");
Require(recordedAppActivationRevocation?.AppServerInstallationId == appServerInstallationId, "App activation revocation should preserve the app server identity.");
var appActivationRevocationStatusHandler = new GetAppActivationRevocationStatusHandler(
    appActivationRevocationStore,
    bootstrapConfigurationStore,
    clock);
var appActivationRevocationStatusResult = await appActivationRevocationStatusHandler.HandleAsync(
    new GetAppActivationRevocationStatusQuery(
        clientId,
        installationId,
        appServerInstallationId,
        appActivationIssueId,
        "fingerprint-smoke",
        new string('a', 64),
        "safarsuite-app-smoke"));
Require(appActivationRevocationStatusResult.IsSuccess, "App activation revocation status should resolve from the local ledger.");
Require(appActivationRevocationStatusResult.Status?.IsRevoked == true, "Recorded app activation revocation should block the app activation issue.");
Require(appActivationRevocationStatusResult.Status?.IdentityMatched == true, "Recorded app activation revocation should match the app server identity.");
Require(appActivationRevocationStatusResult.Status?.RevocationState == LocalServerAppActivationRevocationStates.Revoked, "Recorded app activation revocation should expose the revoked state.");
var mismatchedAppActivationRevocationStatusResult = await appActivationRevocationStatusHandler.HandleAsync(
    new GetAppActivationRevocationStatusQuery(
        clientId,
        installationId,
        Guid.NewGuid(),
        appActivationIssueId,
        "fingerprint-smoke",
        new string('a', 64),
        "safarsuite-app-smoke"));
Require(mismatchedAppActivationRevocationStatusResult.IsSuccess, "Mismatched app activation revocation status should still resolve.");
Require(mismatchedAppActivationRevocationStatusResult.Status?.IsRevoked == true, "Mismatched app activation identity should fail closed.");
Require(mismatchedAppActivationRevocationStatusResult.Status?.IdentityMatched == false, "Mismatched app activation identity should be marked explicitly.");
Require(mismatchedAppActivationRevocationStatusResult.Status?.RevocationState == LocalServerAppActivationRevocationStates.RevokedIdentityMismatch, "Mismatched app activation identity should expose a stable mismatch state.");
var unrevokedAppActivationStatusResult = await appActivationRevocationStatusHandler.HandleAsync(
    new GetAppActivationRevocationStatusQuery(
        clientId,
        installationId,
        appServerInstallationId,
        Guid.NewGuid(),
        "fingerprint-smoke",
        new string('a', 64),
        "safarsuite-app-smoke"));
Require(unrevokedAppActivationStatusResult.IsSuccess, "Unrevoked app activation status should resolve from the local ledger.");
Require(unrevokedAppActivationStatusResult.Status?.IsRevoked == false, "Unknown app activation issue should not be marked revoked locally.");
Require(unrevokedAppActivationStatusResult.Status?.RevocationState == LocalServerAppActivationRevocationStates.NotRevoked, "Unknown app activation issue should expose the not-revoked state.");

var cachedEntitlement = await cache.GetCurrentAsync();

Require(cachedEntitlement?.EntitlementVersion == 102, "Cached entitlement should be updated by the refresh-entitlement command.");
var verifiedCachedEntitlement = cachedEntitlement
    ?? throw new InvalidOperationException("Cached entitlement should exist.");
var importedEntitlement = pullResult.Entitlement
    ?? throw new InvalidOperationException("Pulled entitlement should exist.");

Console.WriteLine(JsonSerializer.Serialize(
    new
    {
        status = "Passed",
        installationId,
        registrationStatus = registeredInstallation.InstallationStatus,
        registrationDeploymentMode = registeredInstallation.DeploymentProfile?.ClientDeploymentMode,
        bootstrapConfigurationStatus = savedBootstrapConfiguration.RegistrationStatus,
        bootstrapSignatureKeyId = savedBootstrapConfiguration.SignatureKeyId,
        bootstrapRuntimeServices = bootstrapRuntimePlan.Services.Count,
        bootstrapArtifacts = bootstrapPackage.Artifacts.Count,
        bootstrapDeploymentMode = bootstrapPackage.DeploymentProfile.ClientDeploymentMode,
        bootstrapSiteId = bootstrapPackage.DeploymentProfile.SiteId,
        bootstrapSiteRole = bootstrapPackage.DeploymentProfile.SiteRole,
        cloudFirstManagerSetupTokenIssued = cloudFirstManagerSetupTokenResult.IsSuccess,
        cloudFirstManagerSetupTokenIssuePersisted = cloudFirstManagerIssue is not null,
        firstManagerSetupTokenStatus = firstManagerTokenVerification.IsSuccess ? "Verified" : firstManagerTokenVerification.FailureCode,
        badFirstManagerSetupTokenRejected = badFirstManagerTokenVerification.FailureCode,
        expiredFirstManagerSetupTokenRejected = expiredFirstManagerTokenVerification.FailureCode,
        missingFirstManagerSetupTokenRejected = missingFirstManagerTokenVerification.FailureCode,
        firstManagerDeviceStatus = persistedFirstManagerDevice?.DeviceStatus,
        firstManagerDeviceCredentialVerified = firstManagerDeviceCredentialVerification.IsSuccess,
        firstManagerSetupTokenConsumed = consumedFirstManagerToken is not null,
        cachedVersion = verifiedCachedEntitlement.EntitlementVersion,
        importedBundleIssueId = importedEntitlement.BundleIssueId,
        pulledAtUtc = pullResult.PulledAtUtc,
        heartbeatStatus = heartbeatResult.Heartbeat.HeartbeatStatus,
        heartbeatLicenseStatus = heartbeatResult.Heartbeat.LicenseStatus,
        heartbeatSource = "BootstrapConfiguration",
        badBootstrapSignatureRejected = badBootstrapRegistrationResult.FailureCode,
        badSignatureRejected = badSignatureResult.FailureCode,
        olderVersionRejected = olderImportResult.FailureCode,
        offlineRenewalFileId = offlineRenewalResult.RenewalFileId,
        offlineRenewalImportedVersion = offlineRenewedEntitlement.EntitlementVersion,
        replayedOfflineRenewalRejected = replayedOfflineRenewalResult.FailureCode,
        localImportAuditRecords = importAuditRecords.Count,
        lastTrustedCloudTimeUtc = clockRollbackTrustState.LastSuccessfulCloudTimeUtc,
        clockMovedBackwards = clockRollbackTrustState.ClockMovedBackwards,
        diagnosticsLicenseStatus = diagnosticsBundle.LicenseStatus,
        diagnosticsRuntimeMode = diagnosticsBundle.Runtime?.RuntimeMode,
        diagnosticsSiteId = diagnosticsBundle.DeploymentProfile?.SiteId,
        diagnosticsServices = diagnosticsBundle.Services?.Count,
        diagnosticsImportAudit = diagnosticsBundle.ImportAudit?.Count,
        diagnosticsUploadStatus = diagnosticsUploadResult.Upload!.Status,
        commandProcessingApplied = commandProcessingResult.AppliedCount,
        commandProcessingAcknowledgements = commandClient.Acknowledgements.Count,
        commandDiagnosticsRuntimeServices = commandDiagnosticsHttpHandler.LastRequest?.Bundle.Services?.Count,
        commandDiagnosticsRuntimeErrors = commandDiagnosticsHttpHandler.LastRequest?.Bundle.RecentErrors?.Count,
        appActivationRevocationState = appActivationRevocationStatusResult.Status?.RevocationState,
        appActivationRevocationIdentityMismatchState = mismatchedAppActivationRevocationStatusResult.Status?.RevocationState,
        appActivationNotRevokedState = unrevokedAppActivationStatusResult.Status?.RevocationState,
        moduleGatewayAccountingAllowed = moduleGatewayAllowedResult.Access.IsAllowed,
        moduleGatewayReportsState = moduleGatewayDisabledResult.Access.AccessState,
        moduleGatewayExpiredState = moduleGatewayExpiredResult.Access.AccessState,
        activeDecision,
        warningDecision,
        graceDecision,
        restrictedDecision,
        expiredDecision,
        disabledDecision
    },
    new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    }));

static async Task<LocalServerBootstrapPackageResponse> CreateBootstrapPackageAsync(
    Guid clientId,
    string installationId)
{
    var cloudClock = new FixedControlCloudClock(
        new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
    var setupTokenRepository = new InMemorySetupTokenRepository();
    var auditRecorder = new InMemoryClientPortalAuditRecorder();
    var unitOfWork = new PassthroughControlCloudUnitOfWork();
    var setupTokenHandler = new CreateInstallationSetupTokenHandler(
        new StaticCommercialProjectionRepository(clientId),
        new InMemoryClientInstallationRepository(),
        setupTokenRepository,
        new StaticSetupTokenService(),
        auditRecorder,
        unitOfWork,
        cloudClock);
    var bootstrapHandler = new CreateLocalServerBootstrapPackageHandler(
        setupTokenHandler,
        setupTokenRepository,
        new HmacControlCloudBootstrapPackageSigner(
            new ControlCloudEntitlementSigningOptions
            {
                ActiveKeyId = "bootstrap-smoke",
                SigningKeys =
                [
                    new ControlCloudEntitlementSigningKeyOptions
                    {
                        KeyId = "bootstrap-smoke",
                        Secret = "bootstrap-signing-secret-change-before-cloud"
                    }
                ]
            }),
        new EcdsaControlCloudAppActivationTokenSigner(
            new ControlCloudAppActivationSigningOptions
            {
                ActiveKeyId = "app-activation-smoke"
            }),
        auditRecorder,
        unitOfWork,
        cloudClock);
    var result = await bootstrapHandler.HandleAsync(
        new CreateLocalServerBootstrapPackageCommand(
            clientId,
            installationId,
            ExpiresInHours: 24,
            CreatedBy: "Smoke",
            DeploymentMode: ControlCloudBootstrapModes.OnlineBootstrap,
            LocalServerVersion: "local-server-smoke",
            CloudBaseUrl: "https://control-cloud.local",
            InstallScriptUrl: "https://control-cloud.local/install/safarsuite-local-server/install.sh",
            SafarSuiteAppVersion: "safarsuite-app-smoke",
            ClientDeploymentMode: SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
            SiteId: "hq-main",
            SiteRole: SafarSuiteDeploymentSiteRoles.Hq,
            ParentSiteId: null,
            BranchCode: "HQ",
            SyncTopologyId: "sync-main"));

    Require(result.IsSuccess, "Bootstrap package should generate through the Control Cloud signer.");
    var generatedPackage = result.BootstrapPackage
        ?? throw new InvalidOperationException("Bootstrap package should exist.");

    var handoffHandler = new MarkLocalServerBootstrapPackageHandoffHandler(
        setupTokenRepository,
        auditRecorder,
        cloudClock);
    var handoffResult = await handoffHandler.HandleAsync(
        new MarkLocalServerBootstrapPackageHandoffCommand(
            clientId,
            installationId,
            generatedPackage.BootstrapPackageId,
            Channel: "Secure email",
            Recipient: "customer.ops@example.test",
            MarkedBy: "Smoke",
            Note: "Smoke handoff marker"));
    var handoffAuditRecord = auditRecorder.Records.LastOrDefault(record =>
        record.EventType == ClientPortalAuditEventTypes.BootstrapPackageHandedOff
        && record.Detail.Contains(generatedPackage.BootstrapPackageId.ToString("D"), StringComparison.Ordinal));

    Require(handoffResult.IsSuccess, "Bootstrap package handoff should be marked for an existing package.");
    Require(handoffResult.Response?.HandoffStatus == "HandedOff", "Bootstrap package handoff response should expose the marked state.");
    Require(handoffAuditRecord is not null, "Bootstrap package handoff should be recorded in audit history.");
    Require(
        handoffAuditRecord!.Detail.Contains("Secure email", StringComparison.Ordinal)
        && handoffAuditRecord.Detail.Contains("customer.ops@example.test", StringComparison.Ordinal),
        "Bootstrap package handoff audit should include non-secret channel and recipient evidence.");
    Require(
        !handoffAuditRecord.Detail.Contains(generatedPackage.SetupToken, StringComparison.Ordinal),
        "Bootstrap package handoff audit should not expose setup-token plaintext.");

    var unsupportedModeResult = await bootstrapHandler.HandleAsync(
        new CreateLocalServerBootstrapPackageCommand(
            clientId,
            installationId,
            ExpiresInHours: 24,
            CreatedBy: "Smoke",
            DeploymentMode: SafarSuiteClientDeploymentModes.OfflineLocal,
            LocalServerVersion: "local-server-smoke",
            CloudBaseUrl: "https://control-cloud.local",
            InstallScriptUrl: "https://control-cloud.local/install/safarsuite-local-server/install.sh",
            SafarSuiteAppVersion: "safarsuite-app-smoke"));

    Require(!unsupportedModeResult.IsSuccess, "Bootstrap package should reject client deployment modes in the bootstrap-mode field.");
    Require(unsupportedModeResult.FailureCode == "BootstrapModeUnsupported", "Unsupported bootstrap mode failure code should be stable.");

    return generatedPackage;
}

async Task<LocalServerFeatureAccessDecision> EvaluateAsync(
    string moduleCode,
    DateOnly asOfDate)
{
    return await evaluateHandler.HandleAsync(
        new EvaluateFeatureAccessQuery(
            installationId,
            moduleCode,
            asOfDate));
}

static string CreateSupportCommandPayload(
    Guid clientId,
    string installationId,
    string commandType,
    string requestedBy,
    string reason,
    DateTimeOffset requestedAtUtc)
{
    return JsonSerializer.Serialize(
        new
        {
            FormatVersion = "safarsuite-control-desk-support-command-v1",
            CommandType = commandType,
            ClientId = clientId,
            InstallationId = installationId,
            RequestedBy = requestedBy,
            Reason = reason,
            RequestedAtUtc = requestedAtUtc
        },
        new JsonSerializerOptions(JsonSerializerDefaults.Web));
}

static string CreateAppActivationRevocationCommandPayload(
    Guid clientId,
    string installationId,
    Guid appServerInstallationId,
    Guid activationIssueId,
    Guid activationRequestId,
    DateTimeOffset revokedAtUtc,
    string revokedBy,
    string reason)
{
    return JsonSerializer.Serialize(
        new
        {
            PayloadFormatVersion = "safarsuite-app-activation-revocation-command-v1",
            CommandType = LocalServerInstallationCommandTypes.RevokeAppActivation,
            ClientId = clientId,
            InstallationId = installationId,
            AppServerInstallationId = appServerInstallationId,
            ActivationIssueId = activationIssueId,
            ActivationRequestId = activationRequestId,
            FingerprintHash = "fingerprint-smoke",
            ServerPublicKeySha256 = new string('a', 64),
            SigningKeyId = "app-activation-smoke",
            RevokedAtUtc = revokedAtUtc,
            RevokedBy = revokedBy,
            Reason = reason
        },
        new JsonSerializerOptions(JsonSerializerDefaults.Web));
}

static InstallationCommandResponse CreateCommandResponse(
    HmacControlCloudInstallationCommandSigner signer,
    Guid clientId,
    string installationId,
    long commandVersion,
    string commandType,
    string payloadJson,
    DateTimeOffset queuedAtUtc,
    string? transportPayloadJson = null)
{
    var commandId = Guid.NewGuid();
    var expiresAtUtc = queuedAtUtc.AddHours(2);
    var transportQueuedAtUtc = TruncateToMicrosecond(queuedAtUtc);
    var transportExpiresAtUtc = TruncateToMicrosecond(expiresAtUtc);
    var signature = signer.Sign(
        new ControlCloudInstallationCommandSigningPayload(
            commandId,
            clientId,
            installationId,
            commandVersion,
            commandType,
            payloadJson,
            queuedAtUtc,
            NotBeforeUtc: null,
            expiresAtUtc));

    using var payloadDocument = JsonDocument.Parse(transportPayloadJson ?? payloadJson);

    return new InstallationCommandResponse(
        commandId,
        clientId,
        installationId,
        commandVersion,
        commandType,
        "Pending",
        $"smoke:{commandType}:{commandVersion}",
        payloadDocument.RootElement.Clone(),
        new InstallationCommandSignatureResponse(
            signature.Algorithm,
            signature.KeyId,
            signature.PayloadSha256,
            signature.Value),
        transportQueuedAtUtc,
        NotBeforeUtc: null,
        transportExpiresAtUtc,
        AcknowledgedAtUtc: null,
        AcknowledgementStatus: null,
        AcknowledgementDetail: null);
}

static DateTimeOffset TruncateToMicrosecond(DateTimeOffset value)
{
    var utc = value.ToUniversalTime();
    var ticks = utc.Ticks - (utc.Ticks % 10);

    return new DateTimeOffset(ticks, TimeSpan.Zero);
}

static string ReorderSupportCommandPayload(string payloadJson)
{
    using var document = JsonDocument.Parse(payloadJson);
    var payload = document.RootElement;

    return string.Concat(
        "{",
        "\"reason\":", payload.GetProperty("reason").GetRawText(), ",",
        "\"clientId\":", payload.GetProperty("clientId").GetRawText(), ",",
        "\"commandType\":", payload.GetProperty("commandType").GetRawText(), ",",
        "\"requestedBy\":", payload.GetProperty("requestedBy").GetRawText(), ",",
        "\"formatVersion\":", payload.GetProperty("formatVersion").GetRawText(), ",",
        "\"installationId\":", payload.GetProperty("installationId").GetRawText(), ",",
        "\"requestedAtUtc\":", payload.GetProperty("requestedAtUtc").GetRawText(),
        "}");
}

static ClientPortalSignedEntitlementBundleResponse CreateSignedBundle(
    Guid clientId,
    string installationId,
    long entitlementVersion,
    DateOnly? paidUntil = null,
    DateOnly? warningStartsAt = null,
    DateOnly? graceUntil = null,
    DateOnly? offlineValidUntil = null)
{
    var resolvedPaidUntil = paidUntil ?? new DateOnly(2026, 8, 31);
    var resolvedWarningStartsAt = warningStartsAt ?? new DateOnly(2026, 8, 24);
    var resolvedGraceUntil = graceUntil ?? new DateOnly(2026, 9, 7);
    var resolvedOfflineValidUntil = offlineValidUntil ?? new DateOnly(2026, 9, 14);
    var payload = new ClientPortalEntitlementBundlePayloadResponse(
        "1",
        "SafarSuite.ControlCloud",
        "SafarSuite.ClientPortal",
        clientId,
        installationId,
        entitlementVersion,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "INV-SMOKE-001",
        "Active",
        new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
        new DateOnly(2026, 8, 1),
        resolvedPaidUntil,
        resolvedWarningStartsAt,
        resolvedGraceUntil,
        resolvedOfflineValidUntil,
        5,
        1,
        [
            new ClientPortalEntitlementBundleModuleResponse("Accounting", "Active", true),
            new ClientPortalEntitlementBundleModuleResponse("Billing", "Active", true),
            new ClientPortalEntitlementBundleModuleResponse("Reports", "Disabled", false)
        ]);
    var payloadJson = JsonSerializer.Serialize(
        payload,
        new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var payloadSha256 = ComputeSha256(payloadJson);
    var signature = Sign(
        "local-entitlement-signing-secret-change-before-cloud",
        payloadJson);

    return new ClientPortalSignedEntitlementBundleResponse(
        payloadJson,
        payload,
        new ClientPortalEntitlementBundleSignatureResponse(
            "HMAC-SHA256",
            "local-entitlement-dev",
            payloadSha256,
            signature));
}

static LocalServerSignedFirstManagerSetupTokenResponse CreateSignedFirstManagerSetupToken(
    Guid clientId,
    string installationId,
    Guid pendingDeviceRequestId,
    DateTimeOffset issuedAtUtc,
    string signingKeyId,
    string signingSecret,
    DateTimeOffset? expiresAtUtc = null)
{
    var payload = new LocalServerFirstManagerSetupTokenPayloadResponse(
        LocalServerPairingFormats.FirstManagerSetupTokenVersion,
        Guid.NewGuid(),
        clientId,
        installationId,
        pendingDeviceRequestId,
        [
            LocalServerFirstManagerSetupTokenActions.CreateFirstManager,
            LocalServerFirstManagerSetupTokenActions.ApproveFirstDevice
        ],
        "Owner",
        "owner@safarsuite.local",
        "entitlement-smoke",
        issuedAtUtc,
        expiresAtUtc ?? issuedAtUtc.AddHours(1));
    var payloadJson = JsonSerializer.Serialize(
        payload,
        new JsonSerializerOptions(JsonSerializerDefaults.Web));
    var payloadSha256 = ComputeSha256(payloadJson);
    var signature = Sign(signingSecret, payloadJson);

    return new LocalServerSignedFirstManagerSetupTokenResponse(
        payloadJson,
        payload,
        new LocalServerBootstrapPackageSignatureResponse(
            "HMAC-SHA256",
            signingKeyId,
            payloadSha256,
            signature));
}

static string ComputeSha256(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var hash = SHA256.HashData(bytes);

    return Convert.ToHexString(hash).ToLowerInvariant();
}

static string Sign(string signingSecret, string payloadJson)
{
    var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
    var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

    return Convert.ToBase64String(signatureBytes);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static LocalServerBootstrapPackageArtifactResponse RequireArtifact(
    LocalServerBootstrapPackageResponse bootstrapPackage,
    string artifactType)
{
    return bootstrapPackage.Artifacts.SingleOrDefault(artifact => artifact.ArtifactType == artifactType)
        ?? throw new InvalidOperationException($"Bootstrap package should include the {artifactType} artifact.");
}

static void RequireLocalServerRuntimeImageContract()
{
    var repoRoot = FindRepositoryRoot();
    var dockerfile = Path.Combine(repoRoot, "src", "SafarSuite.LocalServer.Api", "Dockerfile");
    var apiCommand = Path.Combine(repoRoot, "src", "SafarSuite.LocalServer.Api", "docker", "safarsuite-local-api");
    var workerCommand = Path.Combine(repoRoot, "src", "SafarSuite.LocalServer.Api", "docker", "safarsuite-local-worker");
    var agentCommand = Path.Combine(repoRoot, "src", "SafarSuite.LocalServer.Api", "docker", "safarsuite-local-agent");

    Require(File.Exists(dockerfile), "Local-server Dockerfile should exist.");
    Require(File.Exists(apiCommand), "Local-server API command shim should exist.");
    Require(File.Exists(workerCommand), "Local-server worker command shim should exist.");
    Require(File.Exists(agentCommand), "Local-server agent command shim should exist.");

    var dockerfileContent = File.ReadAllText(dockerfile);
    var apiCommandContent = File.ReadAllText(apiCommand);
    var workerCommandContent = File.ReadAllText(workerCommand);
    var agentCommandContent = File.ReadAllText(agentCommand);

    Require(dockerfileContent.Contains("safarsuite-local-api", StringComparison.Ordinal), "Local-server Dockerfile should install the API command.");
    Require(dockerfileContent.Contains("safarsuite-local-worker", StringComparison.Ordinal), "Local-server Dockerfile should install the worker command.");
    Require(dockerfileContent.Contains("safarsuite-local-agent", StringComparison.Ordinal), "Local-server Dockerfile should install the agent command.");
    Require(dockerfileContent.Contains("SAFARSUITE_LOCAL_SERVER_RUNTIME_BASE_IMAGE", StringComparison.Ordinal), "Local-server Dockerfile should allow the runtime base image to be overridden for local proof builds.");
    Require(dockerfileContent.Contains("SAFARSUITE_LOCAL_API_ASPNETCORE_URLS=http://0.0.0.0:8080", StringComparison.Ordinal), "Local-server Dockerfile should keep the API bind URL explicit for HTTPS-ready deployments.");
    Require(dockerfileContent.Contains("/etc/safarsuite/local-server/certs", StringComparison.Ordinal), "Local-server Dockerfile should create the local API certificate directory.");
    Require(dockerfileContent.Contains("runtime-from-publish", StringComparison.Ordinal), "Local-server Dockerfile should expose a pre-published runtime build target.");
    Require(dockerfileContent.Contains("ENTRYPOINT []", StringComparison.Ordinal), "Local-server Dockerfile should clear inherited base-image entrypoints.");
    Require(dockerfileContent.Contains("HEALTHCHECK NONE", StringComparison.Ordinal), "Local-server Dockerfile should clear inherited base-image healthchecks.");
    Require(dockerfileContent.Contains("USER app", StringComparison.Ordinal), "Local-server runtime image should run as the non-root app user.");
    Require(apiCommandContent.Contains("LocalServer__Runtime__EnableBackgroundWorker", StringComparison.Ordinal)
        && apiCommandContent.Contains("false", StringComparison.Ordinal), "Local-server API command should keep background automation disabled.");
    Require(apiCommandContent.Contains("SAFARSUITE_LOCAL_API_ASPNETCORE_URLS", StringComparison.Ordinal)
        && apiCommandContent.Contains("SAFARSUITE_LOCAL_API_CERTIFICATE_PATH", StringComparison.Ordinal), "Local-server API command should map HTTPS-ready Local API settings to Kestrel.");
    Require(workerCommandContent.Contains("LocalServer__Runtime__EnableEntitlementPull", StringComparison.Ordinal)
        && workerCommandContent.Contains("LocalServer__Runtime__EnableHeartbeat", StringComparison.Ordinal)
        && workerCommandContent.Contains("LocalServer__Runtime__EnableCommandPolling", StringComparison.Ordinal)
        && workerCommandContent.Contains("false", StringComparison.Ordinal), "Local-server worker command should run entitlement and heartbeat without command polling.");
    Require(agentCommandContent.Contains("LocalServer__Runtime__EnableCommandPolling", StringComparison.Ordinal)
        && agentCommandContent.Contains("LocalServer__Runtime__EnableEntitlementPull", StringComparison.Ordinal)
        && agentCommandContent.Contains("false", StringComparison.Ordinal), "Local-server agent command should run command polling without entitlement pull.");
}

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "SafarSuite.ControlDesk.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Repository root could not be found.");
}

internal sealed class FixedLocalServerClock : ILocalServerClock
{
    public FixedLocalServerClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}

internal sealed class StaticBundleHttpMessageHandler : HttpMessageHandler
{
    private readonly ClientPortalSignedEntitlementBundleResponse _bundle;

    public StaticBundleHttpMessageHandler(
        ClientPortalSignedEntitlementBundleResponse bundle)
    {
        _bundle = bundle;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get
            || request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith(
                "/entitlement-bundle",
                StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(_bundle)
        };

        return Task.FromResult(response);
    }
}

internal sealed class StaticInstallationCommandClient : IControlCloudInstallationCommandClient
{
    private readonly string _installationId;
    private readonly Dictionary<Guid, InstallationCommandResponse> _commands;

    public StaticInstallationCommandClient(
        string installationId,
        IReadOnlyCollection<InstallationCommandResponse> commands)
    {
        _installationId = installationId;
        _commands = commands.ToDictionary(command => command.CommandId);
    }

    public Dictionary<Guid, AcknowledgeInstallationCommandRequest> Acknowledgements { get; } = [];

    public Task<ControlCloudPendingInstallationCommandsResult> GetPendingAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(installationId, _installationId, StringComparison.Ordinal))
        {
            return Task.FromResult(ControlCloudPendingInstallationCommandsResult.Failure(
                "InstallationNotFound",
                "Smoke installation was not found."));
        }

        var response = new PendingInstallationCommandsResponse(
            _installationId,
            _commands.Values
                .Where(command => !Acknowledgements.ContainsKey(command.CommandId))
                .OrderBy(command => command.CommandVersion)
                .ToArray());

        return Task.FromResult(ControlCloudPendingInstallationCommandsResult.Success(response));
    }

    public Task<ControlCloudInstallationCommandAcknowledgementResult> AcknowledgeAsync(
        string installationId,
        Guid commandId,
        AcknowledgeInstallationCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(installationId, _installationId, StringComparison.Ordinal)
            || !_commands.TryGetValue(commandId, out var command))
        {
            return Task.FromResult(ControlCloudInstallationCommandAcknowledgementResult.Failure(
                "CommandNotFound",
                "Smoke command was not found."));
        }

        Acknowledgements[commandId] = request;

        var acknowledgedCommand = command with
        {
            Status = request.ResultStatus == LocalServerInstallationCommandAcknowledgementStatuses.Applied
                ? "Acknowledged"
                : "Failed",
            AcknowledgedAtUtc = DateTimeOffset.UtcNow,
            AcknowledgementStatus = request.ResultStatus,
            AcknowledgementDetail = request.Detail
        };
        _commands[commandId] = acknowledgedCommand;

        return Task.FromResult(ControlCloudInstallationCommandAcknowledgementResult.Success(acknowledgedCommand));
    }
}

internal sealed class StaticRegistrationHttpMessageHandler : HttpMessageHandler
{
    public RegisterLocalServerInstallationRequest? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post
            || request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith(
                "/registration",
                StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var registrationRequest =
            await request.Content!.ReadFromJsonAsync<RegisterLocalServerInstallationRequest>(
                cancellationToken);

        LastRequest = registrationRequest;

        var response = new LocalServerInstallationRegistrationResponse(
            registrationRequest!.ClientId,
            request.RequestUri.Segments[^2].TrimEnd('/'),
            "Active",
            new DateTimeOffset(2026, 8, 1, 10, 0, 1, TimeSpan.Zero),
            registrationRequest.LocalServerVersion,
            new LocalServerDeploymentProfileResponse(
                ControlCloudBootstrapModes.OnlineBootstrap,
                SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
                "hq-main",
                SafarSuiteDeploymentSiteRoles.Hq,
                ParentSiteId: null,
                BranchCode: "HQ",
                SyncTopologyId: "sync-main"));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
}

internal sealed class StaticHeartbeatHttpMessageHandler : HttpMessageHandler
{
    public ReportLocalServerHeartbeatRequest? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post
            || request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith(
                "/heartbeat",
                StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var heartbeatRequest =
            await request.Content!.ReadFromJsonAsync<ReportLocalServerHeartbeatRequest>(
                cancellationToken);

        LastRequest = heartbeatRequest;

        var response = new LocalServerHeartbeatResponse(
            Guid.NewGuid(),
            request.RequestUri.Segments[^2].TrimEnd('/'),
            heartbeatRequest!.ClientId,
            "Received",
            heartbeatRequest.ReportedAtUtc.AddSeconds(1),
            heartbeatRequest.ReportedAtUtc,
            heartbeatRequest.LicenseStatus,
            heartbeatRequest.EntitlementVersion,
            heartbeatRequest.PaidUntil,
            heartbeatRequest.WarningStartsAt,
            heartbeatRequest.GraceUntil,
            heartbeatRequest.OfflineValidUntil,
            heartbeatRequest.LocalServerVersion,
            heartbeatRequest.Detail,
            PairingStatus: heartbeatRequest.PairingStatus);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
}

internal sealed class StaticDiagnosticsHttpMessageHandler : HttpMessageHandler
{
    public UploadLocalServerDiagnosticsRequest? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post
            || request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith(
                "/diagnostics",
                StringComparison.Ordinal))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var diagnosticsRequest =
            await request.Content!.ReadFromJsonAsync<UploadLocalServerDiagnosticsRequest>(
                cancellationToken);

        LastRequest = diagnosticsRequest;

        var response = new LocalServerDiagnosticsUploadResponse(
            Guid.NewGuid(),
            diagnosticsRequest!.ClientId,
            request.RequestUri.Segments[^2].TrimEnd('/'),
            "Received",
            diagnosticsRequest.Bundle.GeneratedAtUtc.AddSeconds(1));

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
}

internal sealed class StaticRuntimeCommandRunner : ILocalServerRuntimeCommandRunner
{
    public Task<LocalServerRuntimeCommandResult> RunAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var joinedArguments = string.Join(" ", arguments);

        if (fileName == "docker"
            && joinedArguments.Equals("--version", StringComparison.Ordinal))
        {
            return Task.FromResult(Success("Docker version 27.0.0-smoke, build local"));
        }

        if (fileName == "docker"
            && joinedArguments.Equals("compose version", StringComparison.Ordinal))
        {
            return Task.FromResult(Success("Docker Compose version v2.27.0-smoke"));
        }

        if (fileName == "docker"
            && arguments.Contains("ps", StringComparer.Ordinal)
            && arguments.Contains("json", StringComparer.Ordinal))
        {
            return Task.FromResult(Success(
                """
                [
                  {
                    "Name": "safarsuite-local-server-local-api-1",
                    "Service": "local-api",
                    "State": "running",
                    "Health": "healthy",
                    "Status": "Up 2 minutes",
                    "CreatedAt": "2026-08-01T10:01:00+00:00"
                  },
                  {
                    "Name": "safarsuite-local-server-local-worker-1",
                    "Service": "local-worker",
                    "State": "running",
                    "Health": "healthy",
                    "Status": "Up 2 minutes",
                    "CreatedAt": "2026-08-01T10:01:00+00:00"
                  },
                  {
                    "Name": "safarsuite-local-server-local-agent-1",
                    "Service": "local-agent",
                    "State": "running",
                    "Health": "healthy",
                    "Status": "Up 2 minutes",
                    "CreatedAt": "2026-08-01T10:01:00+00:00"
                  }
                ]
                """));
        }

        if (fileName == "docker"
            && arguments.Contains("logs", StringComparer.Ordinal)
            && arguments.Contains("--tail", StringComparer.Ordinal))
        {
            return Task.FromResult(Success(
                """
                safarsuite-local-server-local-api-1     | 2026-08-01T10:03:00+00:00 info: module gateway checked Accounting.
                safarsuite-local-server-local-api-1     | 2026-08-01T10:03:01+00:00 error: module gateway retry failed for Reports.
                safarsuite-local-server-local-worker-1  | 2026-08-01T10:03:02+00:00 warn: heartbeat delayed by transient cloud timeout.
                safarsuite-local-server-local-agent-1   | 2026-08-01T10:03:03+00:00 info: diagnostics completed.
                """));
        }

        return Task.FromResult(new LocalServerRuntimeCommandResult(
            ExitCode: 1,
            StandardOutput: "",
            StandardError: $"Unexpected smoke command: {fileName} {joinedArguments}",
            TimedOut: false));
    }

    private static LocalServerRuntimeCommandResult Success(string output)
    {
        return new LocalServerRuntimeCommandResult(
            ExitCode: 0,
            output,
            StandardError: "",
            TimedOut: false);
    }
}

internal sealed class FixedControlCloudClock : IControlCloudClock
{
    public FixedControlCloudClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}

internal sealed class StaticCommercialProjectionRepository
    : IControlCloudClientCommercialProjectionRepository
{
    private readonly ControlCloudClientCommercialProjection _projection;

    public StaticCommercialProjectionRepository(Guid clientId)
    {
        _projection = ControlCloudClientCommercialProjection.Create(clientId, "PKR");
    }

    public Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _projection.ClientId == clientId
                ? _projection
                : null);
    }

    public Task SaveAsync(
        ControlCloudClientCommercialProjection projection,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryClientInstallationRepository
    : IControlCloudClientInstallationRepository
{
    private readonly Dictionary<string, ControlCloudClientInstallation> _installations = new(StringComparer.Ordinal);

    public Task<ControlCloudClientInstallation?> GetByInstallationIdAsync(
        string installationId,
        CancellationToken cancellationToken = default)
    {
        _installations.TryGetValue(installationId.Trim(), out var installation);

        return Task.FromResult(installation);
    }

    public Task AddAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        _installations[installation.InstallationId] = installation;

        return Task.CompletedTask;
    }

    public Task SaveAsync(
        ControlCloudClientInstallation installation,
        CancellationToken cancellationToken = default)
    {
        _installations[installation.InstallationId] = installation;

        return Task.CompletedTask;
    }
}

internal sealed class InMemorySetupTokenRepository
    : IControlCloudInstallationSetupTokenRepository
{
    private readonly Dictionary<string, ControlCloudInstallationSetupToken> _setupTokensByHash = new(StringComparer.Ordinal);

    public Task<ControlCloudInstallationSetupToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        _setupTokensByHash.TryGetValue(tokenHash.Trim(), out var setupToken);

        return Task.FromResult(setupToken);
    }

    public Task AddAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        _setupTokensByHash[setupToken.TokenHash] = setupToken;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ControlCloudInstallationSetupToken>> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var setupTokens = _setupTokensByHash.Values
            .Where(setupToken => setupToken.ClientId == clientId
                && string.Equals(setupToken.InstallationId, installationId.Trim(), StringComparison.Ordinal)
                && setupToken.HasBootstrapPackage)
            .OrderByDescending(setupToken => setupToken.BootstrapPackageGeneratedAtUtc ?? setupToken.CreatedAtUtc)
            .Take(take)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ControlCloudInstallationSetupToken>>(setupTokens);
    }

    public Task<ControlCloudInstallationSetupToken?> GetBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        Guid bootstrapPackageId,
        CancellationToken cancellationToken = default)
    {
        var setupToken = _setupTokensByHash.Values.FirstOrDefault(candidate =>
            candidate.ClientId == clientId
            && string.Equals(candidate.InstallationId, installationId.Trim(), StringComparison.Ordinal)
            && candidate.BootstrapPackageId == bootstrapPackageId);

        return Task.FromResult(setupToken);
    }

    public Task SaveAsync(
        ControlCloudInstallationSetupToken setupToken,
        CancellationToken cancellationToken = default)
    {
        _setupTokensByHash[setupToken.TokenHash] = setupToken;

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryFirstManagerSetupTokenIssueRepository
    : IControlCloudFirstManagerSetupTokenIssueRepository
{
    private readonly Dictionary<Guid, ControlCloudFirstManagerSetupTokenIssue> _issues = [];

    public Task AddAsync(
        ControlCloudFirstManagerSetupTokenIssue issue,
        CancellationToken cancellationToken = default)
    {
        _issues[issue.TokenId] = issue;

        return Task.CompletedTask;
    }

    public Task<ControlCloudFirstManagerSetupTokenIssue?> GetByTokenIdAsync(
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        _issues.TryGetValue(tokenId, out var issue);

        return Task.FromResult(issue);
    }
}

internal sealed class StaticSetupTokenService : IControlCloudInstallationSetupTokenService
{
    public string CreateSetupToken()
    {
        return "setup-token-bootstrap-smoke";
    }

    public string HashSecret(string secret)
    {
        return $"hash:{secret.Trim()}";
    }
}

internal sealed class InMemoryClientPortalAuditRecorder : IClientPortalAuditRecorder
{
    private readonly List<ClientPortalAuditRecord> _records = [];

    public IReadOnlyList<ClientPortalAuditRecord> Records => _records;

    public Task RecordAsync(
        ClientPortalAuditRecord audit,
        CancellationToken cancellationToken = default)
    {
        _records.Add(audit);

        return Task.CompletedTask;
    }
}

internal sealed class PassthroughControlCloudUnitOfWork : IControlCloudUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        return operation(cancellationToken);
    }

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        return operation(cancellationToken);
    }
}
