using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

var bundleJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};
// Stub mode fallback. Real Control Cloud mode reads this verifier key from the signed bootstrap package.
const string AppProofActivationPublicKeyPem =
    "-----BEGIN PUBLIC KEY-----\n"
    + "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEplJxLk27PAx9Jh5gFKq/cL5e9V67\n"
    + "GFv6MGWoiHl8PPfJtIpnA2gFoxQtAR4/QvnjJ4JvzcxIqkuW23fHR9pQUg==\n"
    + "-----END PUBLIC KEY-----";
const string AppProofActivationPublicKeyPemEscaped =
    "\"-----BEGIN PUBLIC KEY-----\\nMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEplJxLk27PAx9Jh5gFKq/cL5e9V67\\nGFv6MGWoiHl8PPfJtIpnA2gFoxQtAR4/QvnjJ4JvzcxIqkuW23fHR9pQUg==\\n-----END PUBLIC KEY-----\\n\"";
const string AppProofActivationSigningKeyId = "compose-proof-ecdsa-p256-2026-07";
var options = ProofOptions.Parse(args);

if (options.Mode.Equals("serve-stub", StringComparison.OrdinalIgnoreCase))
{
    await ServeStubAsync(options);
    return;
}

if (options.Mode.Equals("generate-real-cloud", StringComparison.OrdinalIgnoreCase))
{
    await GenerateRealCloudProofFilesAsync(options);
    return;
}

if (options.Mode.Equals("activate-app-runtime", StringComparison.OrdinalIgnoreCase))
{
    await ActivateAppRuntimeAsync(options);
    return;
}

if (options.Mode.Equals("run-compose", StringComparison.OrdinalIgnoreCase))
{
    await RunComposeProofAsync(options);
    return;
}

if (options.Mode.Equals("verify-running-runtime", StringComparison.OrdinalIgnoreCase))
{
    await VerifyRunningRuntimeAsync(options);
    return;
}

if (!options.Mode.Equals("generate", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Mode must be 'generate', 'generate-real-cloud', 'activate-app-runtime', 'run-compose', 'verify-running-runtime', or 'serve-stub'.");
}

GenerateProofFiles(options);

void GenerateProofFiles(ProofOptions options)
{
    Directory.CreateDirectory(options.OutputDirectory);
    Directory.CreateDirectory(Path.Combine(options.OutputDirectory, "certs", "local-api"));
    Directory.CreateDirectory(Path.Combine(options.OutputDirectory, "certs", "trust"));
    PrepareLocalApiCertificateArtifacts(options);

    var bundle = CreateBootstrapBundle(options);
    var composeSource = Path.Combine(
        options.RepositoryRoot,
        "src",
        "SafarSuite.ControlCloud.Api",
        "wwwroot",
        "install",
        "safarsuite-local-server",
        "docker-compose.yml");

    File.Copy(
        composeSource,
        Path.Combine(options.OutputDirectory, "docker-compose.yml"),
        overwrite: true);
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "local-server.env"),
        BuildEnvironmentFile(options));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "runtime-services.manifest.json"),
        JsonSerializer.Serialize(CreateRuntimePlan(options), jsonOptions));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "bootstrap-bundle.json"),
        JsonSerializer.Serialize(bundle, jsonOptions));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "proof-notes.txt"),
        BuildProofNotes(options));
}

async Task GenerateRealCloudProofFilesAsync(ProofOptions options)
{
    Directory.CreateDirectory(options.OutputDirectory);

    using var http = new HttpClient
    {
        BaseAddress = new Uri($"{options.ControlCloudApiBaseUrl.TrimEnd('/')}/")
    };

    await RequireHealthyControlCloudAsync(http);

    var envelope = CreateEntitlementSnapshotEnvelope(options);
    var seedResponse = await http.PostAsJsonAsync(
        "api/v1/control-desk/messages",
        envelope,
        bundleJsonOptions);
    var seed = await ReadJsonResponseAsync<ControlCloudReceiveEnvelopeResponse>(
        seedResponse,
        "seed Control Cloud entitlement projection");

    if (!seed.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase)
        && !seed.Status.Equals("Duplicate", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Control Cloud entitlement projection was not accepted. Status: {seed.Status}. Detail: {seed.Detail}");
    }

    var bootstrapRequest = new CreateLocalServerBootstrapPackageRequest(
        ExpiresInHours: 24,
        CreatedBy: "codex-real-cloud-proof",
        DeploymentMode: ControlCloudBootstrapModes.OnlineBootstrap,
        LocalServerVersion: options.LocalServerVersion,
        SafarSuiteAppVersion: options.AppVersion,
        ClientDeploymentMode: SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
        SiteId: options.SiteId,
        SiteRole: SafarSuiteDeploymentSiteRoles.Hq,
        ParentSiteId: null,
        BranchCode: "HQ",
        SyncTopologyId: "compose-proof");
    using var bootstrapMessage = new HttpRequestMessage(
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{options.ClientId:D}/installations/{Uri.EscapeDataString(options.InstallationId)}/bootstrap-package");
    bootstrapMessage.Headers.TryAddWithoutValidation(
        "X-SafarSuite-Provider-Key",
        options.ProviderAccessSecret);
    bootstrapMessage.Content = JsonContent.Create(
        bootstrapRequest,
        options: bundleJsonOptions);
    var bootstrapResponse = await http.SendAsync(bootstrapMessage);
    var package = await ReadJsonResponseAsync<LocalServerBootstrapPackageResponse>(
        bootstrapResponse,
        "create Control Cloud bootstrap package");

    WriteRealCloudProofFiles(options, envelope, seed, package);
}

async Task ActivateAppRuntimeAsync(ProofOptions options)
{
    var appBaseUrl = GetAppRuntimeBaseUrl(options);
    using var http = new HttpClient
    {
        BaseAddress = new Uri($"{appBaseUrl}/")
    };

    await RequireHealthyAppRuntimeAsync(http);

    var stateResponse = await http.GetAsync("api/local-server/activation-state");
    var state = await ReadJsonResponseAsync<AppActivationStateResponse>(
        stateResponse,
        "read app activation state");
    using var cloud = new HttpClient
    {
        BaseAddress = new Uri($"{options.ControlCloudApiBaseUrl.TrimEnd('/')}/")
    };
    using var issueRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{options.ClientId:D}/installations/{Uri.EscapeDataString(options.InstallationId)}/app-activation-token");
    var providerAccessMode = await AddProviderAccessAsync(
        cloud,
        issueRequest,
        options);
    issueRequest.Content = JsonContent.Create<IssueSafarSuiteAppActivationTokenRequest>(
        new IssueSafarSuiteAppActivationTokenRequest(
            state.ActivationRequestId,
            state.ServerInstallationId,
            state.FingerprintHash,
            state.PublicKey,
            RequestedBy: "compose-bootstrap-proof"),
        options: bundleJsonOptions);
    var issueResponse = await cloud.SendAsync(issueRequest);
    var issued = await ReadJsonResponseAsync<IssueSafarSuiteAppActivationTokenResponse>(
        issueResponse,
        "issue app activation token from Control Cloud");
    var importResponse = await http.PostAsJsonAsync(
        "api/local-server/activation-token",
        issued.Import,
        bundleJsonOptions);
    var activated = await ReadJsonResponseAsync<AppActivationStateResponse>(
        importResponse,
        "import app activation token");
    var moduleAccessResponse = await http.GetAsync(
        $"api/v1/local-server/modules/{Uri.EscapeDataString(options.RequiredModule)}/access?requestedBy=safarsuite-app&asOfDate={ResolveAsOfDate(options):yyyy-MM-dd}");
    var moduleAccess = await ReadJsonResponseAsync<AppModuleAccessResponse>(
        moduleAccessResponse,
        "read app module access");

    if (!activated.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"App activation import completed but status is '{activated.Status}', not Active.");
    }

    if (!moduleAccess.IsAllowed)
    {
        throw new InvalidOperationException(
            $"App module '{moduleAccess.ModuleCode}' is not allowed after activation. State: {moduleAccess.AccessState}. Reason: {moduleAccess.Reason}");
    }

    Console.WriteLine(JsonSerializer.Serialize(
        new
        {
            proofStatus = "Passed",
            appBaseUrl,
            providerAccessMode,
            activationIssueId = issued.ActivationIssueId,
            entitlementVersion = issued.EntitlementVersion,
            signingKeyId = issued.SigningKeyId,
            activatedServerInstallationId = activated.ServerInstallationId,
            activationStatus = activated.Status,
            activationRequestId = activated.ActivationRequestId,
            moduleCode = moduleAccess.ModuleCode,
            isAllowed = moduleAccess.IsAllowed,
            accessState = moduleAccess.AccessState,
            paidUntil = moduleAccess.PaidUntil,
            offlineValidUntil = moduleAccess.OfflineValidUntil
        },
        jsonOptions));
}

async Task<string> AddProviderAccessAsync(
    HttpClient cloud,
    HttpRequestMessage request,
    ProofOptions options)
{
    if (!options.UseProviderSessionToken)
    {
        request.Headers.TryAddWithoutValidation(
            "X-SafarSuite-Provider-Key",
            options.ProviderAccessSecret);

        return "SharedSecret";
    }

    var sessionResponse = options.UseProviderOperatorSession
        ? await cloud.PostAsJsonAsync(
            "api/v1/provider-access/operator-sessions",
            new CreateProviderOperatorSessionRequest(
                options.ProviderOperatorEmail,
                options.ProviderOperatorPassword,
                [
                    "app-activation:read",
                    "app-activation:write"
                ],
                15),
            bundleJsonOptions)
        : await cloud.PostAsJsonAsync(
            "api/v1/provider-access/sessions",
            new CreateProviderAccessSessionRequest(
                options.ProviderAccessSecret,
                "compose-bootstrap-proof",
                [
                    "app-activation:read",
                    "app-activation:write"
                ],
                15),
            bundleJsonOptions);
    var session = await ReadJsonResponseAsync<CreateProviderAccessSessionResponse>(
        sessionResponse,
        "create provider access session");

    if (!session.TokenType.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(session.AccessToken))
    {
        throw new InvalidOperationException(
            $"Control Cloud returned an invalid provider access session token type '{session.TokenType}'.");
    }

    request.Headers.TryAddWithoutValidation(
        "Authorization",
        $"Bearer {session.AccessToken}");

    return options.UseProviderOperatorSession
        ? "ProviderOperatorBearerSession"
        : "BearerSession";
}

async Task RunComposeProofAsync(ProofOptions options)
{
    GenerateProofFiles(options);

    await using var stub = CreateStubApplication(options);
    await stub.StartAsync();
    var composeStarted = false;

    try
    {
        var upResult = await RunProcessAsync(
            "docker",
            BuildDockerComposeArguments(options, "up", "-d"),
            options.OutputDirectory,
            TimeSpan.FromSeconds(options.CommandTimeoutSeconds),
            CancellationToken.None);

        if (upResult.ExitCode != 0)
        {
            Console.Error.WriteLine(
                $"docker compose up failed with exit code {upResult.ExitCode}.{Environment.NewLine}{upResult.StdErr}{Environment.NewLine}{upResult.StdOut}");
            Environment.ExitCode = upResult.ExitCode;
            return;
        }

        composeStarted = true;

        try
        {
            await VerifyRunningRuntimeAsync(options);

            if (options.VerifyAppRuntime)
            {
                await VerifyAppRuntimeHealthAsync(options);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Compose runtime proof failed: {exception.Message}");
            Environment.ExitCode = 1;
        }
    }
    finally
    {
        if (options.CleanupCompose && composeStarted)
        {
            var downResult = await RunProcessAsync(
                "docker",
                BuildDockerComposeArguments(options, "down"),
                options.OutputDirectory,
                TimeSpan.FromSeconds(options.CommandTimeoutSeconds),
                CancellationToken.None);

            if (downResult.ExitCode != 0)
            {
                Console.Error.WriteLine(
                    $"docker compose down failed with exit code {downResult.ExitCode}.{Environment.NewLine}{downResult.StdErr}{Environment.NewLine}{downResult.StdOut}");
            }
        }

        await stub.StopAsync();
    }
}

async Task<LocalRuntimeProofResult> VerifyRunningRuntimeAsync(ProofOptions options)
{
    using var http = CreateLocalApiHttpClient(options);
    var localApiBaseUrl = GetLocalApiHostBaseUrl(options);

    await WaitForHealthyLocalApiAsync(http, options);

    var bundleJson = await File.ReadAllTextAsync(
        Path.Combine(options.OutputDirectory, "bootstrap-bundle.json"));
    using var importResponse = await http.PostAsync(
        "api/v1/local-server/bootstrap-package/import",
        new StringContent(bundleJson, Encoding.UTF8, "application/json"));
    var imported = await ReadJsonResponseAsync<LocalServerBootstrapImportProofResponse>(
        importResponse,
        "import generated bootstrap bundle into LocalServer");

    using var pullResponse = await http.PostAsync("api/v1/local-server/entitlement/pull", content: null);
    var pulled = await ReadJsonResponseAsync<LocalServerEntitlementPullProofResponse>(
        pullResponse,
        "pull entitlement through LocalServer");

    using var heartbeatResponse = await http.PostAsync("api/v1/local-server/heartbeat", content: null);
    var heartbeat = await ReadJsonResponseAsync<LocalServerHeartbeatProofResponse>(
        heartbeatResponse,
        "post LocalServer heartbeat");

    using var commandResponse = await http.PostAsync("api/v1/local-server/commands/process", content: null);
    var commands = await ReadJsonResponseAsync<LocalServerCommandsProofResponse>(
        commandResponse,
        "process pending LocalServer commands");

    using var statusResponse = await http.GetAsync("api/v1/local-server/bootstrap");
    var status = await ReadJsonResponseAsync<LocalServerRuntimeStatusProofResponse>(
        statusResponse,
        "read LocalServer bootstrap status");

    using var discoveryResponse = await http.GetAsync(".well-known/safarsuite-local-server");
    var discovery = await ReadJsonResponseAsync<LocalServerPairingDiscoveryResponse>(
        discoveryResponse,
        "read LocalServer pairing discovery");

    var helloRequest = new LocalServerPairingHelloRequest(
        LocalServerPairingFormats.HelloRequestVersion,
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
        options.AppVersion,
        "compose-proof");
    using var helloResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/hello",
        helloRequest,
        bundleJsonOptions);
    var hello = await ReadJsonResponseAsync<LocalServerPairingHelloResponse>(
        helloResponse,
        "read LocalServer pairing hello");

    var asOfDate = ResolveAsOfDate(options);
    using var accessResponse = await http.GetAsync(
        $"api/v1/local-server/modules/{Uri.EscapeDataString(options.RequiredModule)}/access?requestedBy=compose-proof&asOfDate={asOfDate:yyyy-MM-dd}");
    var moduleAccess = await ReadJsonResponseAsync<LocalServerModuleAccessResponse>(
        accessResponse,
        "verify module access through LocalServer module gateway");

    if (!imported.BootstrapRegistrationStatus.Equals("Registered", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"Bootstrap import did not finish as Registered. Status: {imported.BootstrapRegistrationStatus}");
    }

    if (!status.HasBootstrapConfiguration || !status.HasCachedEntitlement)
    {
        throw new InvalidOperationException(
            "LocalServer runtime status did not include both bootstrap configuration and cached entitlement.");
    }

    if (!discovery.HasBootstrapConfiguration
        || discovery.ClientId != imported.ClientId
        || !string.Equals(discovery.InstallationIdHint, imported.InstallationId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "LocalServer pairing discovery did not report the imported bootstrap identity.");
    }

    if (hello.ClientId != imported.ClientId
        || !string.Equals(hello.InstallationId, imported.InstallationId, StringComparison.Ordinal)
        || !string.Equals(hello.ClientNonce, helloRequest.ClientNonce, StringComparison.Ordinal)
        || !string.Equals(hello.PairingMode, LocalServerPairingModes.ManagerApproval, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(hello.ServerNonce)
        || !string.Equals(hello.FormatVersion, LocalServerPairingFormats.HelloResponseVersion, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(hello.BootstrapPayloadSha256))
    {
        throw new InvalidOperationException(
            "LocalServer pairing hello did not return a stable non-secret pairing identity.");
    }

    var firstManagerDevicePairingRequest = new LocalServerDevicePairingRequest(
        LocalServerPairingFormats.DevicePairingRequestVersion,
        imported.InstallationId,
        "Compose First Manager Device",
        $"compose-first-manager-public-key-{Guid.NewGuid():N}",
        DeviceFingerprintHash: $"compose-first-manager-fingerprint-{Guid.NewGuid():N}",
        WindowsUserHint: "compose-first-manager",
        AppVersion: options.AppVersion,
        HelloServerNonce: hello.ServerNonce,
        HelloClientNonce: hello.ClientNonce,
        RequestedAtUtc: DateTimeOffset.UtcNow);
    using var firstManagerPairingRequestResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/requests",
        firstManagerDevicePairingRequest,
        bundleJsonOptions);
    var firstManagerPairingRequest = await ReadJsonResponseAsync<LocalServerDevicePairingRequestResponse>(
        firstManagerPairingRequestResponse,
        "create first-manager LocalServer device pairing request");

    var firstManagerToken = CreateFirstManagerSetupToken(
        options,
        imported.ClientId,
        imported.InstallationId,
        firstManagerPairingRequest.PairingRequestId);
    var wrongClientFirstManagerToken = CreateFirstManagerSetupToken(
        options,
        Guid.NewGuid(),
        imported.InstallationId,
        firstManagerPairingRequest.PairingRequestId);
    using var wrongClientFirstManagerImportResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/first-manager-token/import",
        wrongClientFirstManagerToken,
        bundleJsonOptions);

    if (wrongClientFirstManagerImportResponse.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            "First-manager setup token for a wrong client was accepted.");
    }

    var wrongInstallationFirstManagerToken = CreateFirstManagerSetupToken(
        options,
        imported.ClientId,
        $"wrong-{imported.InstallationId}",
        firstManagerPairingRequest.PairingRequestId);
    using var wrongInstallationFirstManagerImportResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/first-manager-token/import",
        wrongInstallationFirstManagerToken,
        bundleJsonOptions);

    if (wrongInstallationFirstManagerImportResponse.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            "First-manager setup token for a wrong installation was accepted.");
    }

    using var firstManagerImportResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/first-manager-token/import",
        firstManagerToken,
        bundleJsonOptions);
    var firstManagerImport = await ReadJsonResponseAsync<ImportLocalServerFirstManagerSetupTokenResponse>(
        firstManagerImportResponse,
        "import first-manager setup token");

    using var firstManagerReplayResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/first-manager-token/import",
        firstManagerToken,
        bundleJsonOptions);

    if (firstManagerReplayResponse.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            "Replayed first-manager setup token was accepted.");
    }

    using var firstManagerCredentialVerifyResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/device-credentials/verify",
        new VerifyLocalServerDeviceCredentialRequest(firstManagerImport.DeviceCredential),
        bundleJsonOptions);
    var firstManagerCredentialVerify = await ReadJsonResponseAsync<VerifyLocalServerDeviceCredentialResponse>(
        firstManagerCredentialVerifyResponse,
        "verify first-manager LocalServer device credential");

    if (!firstManagerCredentialVerify.IsManagerCapable
        || firstManagerCredentialVerify.DeviceId != firstManagerImport.DeviceId
        || firstManagerCredentialVerify.ClientId != imported.ClientId
        || !string.Equals(firstManagerCredentialVerify.InstallationId, imported.InstallationId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "First-manager signed device credential did not verify as manager-capable.");
    }

    using var badManagerSessionResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/manager-sessions",
        new CreateLocalServerManagerSessionRequest(
            firstManagerImport.DeviceId,
            $"{firstManagerImport.DeviceCredential}-invalid",
            "compose-proof-manager"),
        bundleJsonOptions);

    if (badManagerSessionResponse.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            "Invalid first-manager device credential created a manager session.");
    }

    using var managerSessionResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/manager-sessions",
        new CreateLocalServerManagerSessionRequest(
            firstManagerImport.DeviceId,
            firstManagerImport.DeviceCredential,
            "compose-proof-manager"),
        bundleJsonOptions);
    var managerSession = await ReadJsonResponseAsync<LocalServerManagerSessionResponse>(
        managerSessionResponse,
        "create LocalServer manager session");

    if (!string.Equals(managerSession.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(managerSession.AccessToken)
        || managerSession.DeviceId != firstManagerImport.DeviceId
        || managerSession.ClientId != imported.ClientId
        || !string.Equals(managerSession.InstallationId, imported.InstallationId, StringComparison.Ordinal)
        || managerSession.ExpiresAtUtc <= DateTimeOffset.UtcNow)
    {
        throw new InvalidOperationException(
            "LocalServer manager session response did not bind the first-manager device and installation.");
    }

    var devicePairingRequest = new LocalServerDevicePairingRequest(
        LocalServerPairingFormats.DevicePairingRequestVersion,
        imported.InstallationId,
        "Compose Proof Device",
        $"compose-proof-public-key-{Guid.NewGuid():N}",
        DeviceFingerprintHash: $"compose-fingerprint-{Guid.NewGuid():N}",
        WindowsUserHint: "compose-proof",
        AppVersion: options.AppVersion,
        HelloServerNonce: hello.ServerNonce,
        HelloClientNonce: hello.ClientNonce,
        RequestedAtUtc: DateTimeOffset.UtcNow);
    using var pairingRequestResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/pairing/requests",
        devicePairingRequest,
        bundleJsonOptions);
    var pairingRequest = await ReadJsonResponseAsync<LocalServerDevicePairingRequestResponse>(
        pairingRequestResponse,
        "create LocalServer device pairing request");

    using var anonymousPendingDevicesResponse = await http.GetAsync("api/v1/local-server/devices/pending");

    if (anonymousPendingDevicesResponse.StatusCode != HttpStatusCode.Unauthorized)
    {
        throw new InvalidOperationException(
            $"Unauthenticated pending-device list returned {(int)anonymousPendingDevicesResponse.StatusCode}; expected 401.");
    }

    using var anonymousApproveResponse = await http.PostAsJsonAsync(
        $"api/v1/local-server/devices/{pairingRequest.DeviceId:D}/approve",
        new ApproveLocalServerDeviceRequest(
            "spoofed-manager",
            "ProofApprovedDevice"),
        bundleJsonOptions);

    if (anonymousApproveResponse.StatusCode != HttpStatusCode.Unauthorized)
    {
        throw new InvalidOperationException(
            $"Unauthenticated device approval returned {(int)anonymousApproveResponse.StatusCode}; expected 401.");
    }

    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Bearer",
        managerSession.AccessToken);

    using var pendingDevicesResponse = await http.GetAsync("api/v1/local-server/devices/pending");
    var pendingDevices = await ReadJsonResponseAsync<LocalServerDevicePairingRequestsResponse>(
        pendingDevicesResponse,
        "list pending LocalServer devices");

    using var approveResponse = await http.PostAsJsonAsync(
        $"api/v1/local-server/devices/{pairingRequest.DeviceId:D}/approve",
        new ApproveLocalServerDeviceRequest(
            "compose-proof-manager",
            "ProofApprovedDevice"),
        bundleJsonOptions);
    var approvedDevice = await ReadJsonResponseAsync<ApproveLocalServerDeviceResponse>(
        approveResponse,
        "approve LocalServer device pairing request");

    using var approvedCredentialVerifyResponse = await http.PostAsJsonAsync(
        "api/v1/local-server/device-credentials/verify",
        new VerifyLocalServerDeviceCredentialRequest(approvedDevice.DeviceCredential),
        bundleJsonOptions);
    var approvedCredentialVerify = await ReadJsonResponseAsync<VerifyLocalServerDeviceCredentialResponse>(
        approvedCredentialVerifyResponse,
        "verify approved LocalServer device credential");

    using var suspendResponse = await http.PostAsJsonAsync(
        $"api/v1/local-server/devices/{pairingRequest.DeviceId:D}/suspend",
        new ChangeLocalServerDeviceStatusRequest(
            "compose-proof-manager",
            "compose proof suspension"),
        bundleJsonOptions);
    var suspendedDevice = await ReadJsonResponseAsync<LocalServerDeviceResponse>(
        suspendResponse,
        "suspend LocalServer paired device");

    using var revokeResponse = await http.PostAsJsonAsync(
        $"api/v1/local-server/devices/{pairingRequest.DeviceId:D}/revoke",
        new ChangeLocalServerDeviceStatusRequest(
            "compose-proof-manager",
            "compose proof revocation"),
        bundleJsonOptions);
    var revokedDevice = await ReadJsonResponseAsync<LocalServerDeviceResponse>(
        revokeResponse,
        "revoke LocalServer paired device");

    if (!string.Equals(firstManagerImport.Device.DeviceStatus, LocalServerDevicePairingStatuses.Approved, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(firstManagerImport.DeviceCredential)
        || firstManagerImport.SignedDeviceCredential is null
        || firstManagerImport.TokenId != firstManagerToken.Payload.TokenId
        || firstManagerImport.PairingRequestId != firstManagerPairingRequest.PairingRequestId
        || !string.Equals(pairingRequest.PairingRequestStatus, LocalServerDevicePairingStatuses.Pending, StringComparison.Ordinal)
        || !pendingDevices.Devices.Any(device => device.DeviceId == pairingRequest.DeviceId)
        || !string.Equals(approvedDevice.Device.DeviceStatus, LocalServerDevicePairingStatuses.Approved, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(approvedDevice.DeviceCredential)
        || approvedDevice.SignedDeviceCredential is null
        || approvedCredentialVerify.DeviceId != pairingRequest.DeviceId
        || approvedCredentialVerify.IsManagerCapable
        || !string.Equals(approvedDevice.Device.ApprovedBy, managerSession.Actor, StringComparison.Ordinal)
        || !string.Equals(suspendedDevice.DeviceStatus, LocalServerDevicePairingStatuses.Suspended, StringComparison.Ordinal)
        || !string.Equals(suspendedDevice.SuspendedBy, managerSession.Actor, StringComparison.Ordinal)
        || !string.Equals(revokedDevice.DeviceStatus, LocalServerDevicePairingStatuses.Revoked, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "LocalServer device pairing lifecycle did not move through pending, approved, suspended, and revoked states.");
    }

    if (!string.Equals(revokedDevice.RevokedBy, managerSession.Actor, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            "LocalServer device management audit actor was not derived from the manager session.");
    }

    if (!moduleAccess.IsAllowed)
    {
        throw new InvalidOperationException(
            $"Module '{moduleAccess.ModuleCode}' is not allowed after compose proof. State: {moduleAccess.AccessState}. Reason: {moduleAccess.Reason}");
    }

    var result = new LocalRuntimeProofResult(
        "Passed",
        localApiBaseUrl,
        options.LocalApiTlsMode,
        imported.ClientId,
        imported.InstallationId,
        imported.BootstrapRegistrationStatus,
        imported.CloudRegistrationStatus,
        pulled.EntitlementVersion,
        heartbeat.HeartbeatStatus,
        heartbeat.LicenseStatus,
        commands.PendingCommandCount,
        commands.AppliedCount,
        discovery.HasBootstrapConfiguration,
        hello.PairingMode,
        firstManagerImport.Device.DeviceStatus,
        pairingRequest.PairingRequestStatus,
        approvedDevice.Device.DeviceStatus,
        revokedDevice.DeviceStatus,
        status.HasBootstrapConfiguration,
        status.HasCachedEntitlement,
        moduleAccess.ModuleCode,
        moduleAccess.IsAllowed,
        moduleAccess.AccessState,
        moduleAccess.EntitlementVersion);

    Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));

    return result;
}

async Task VerifyAppRuntimeHealthAsync(ProofOptions options)
{
    using var http = new HttpClient
    {
        BaseAddress = new Uri($"{GetAppRuntimeBaseUrl(options)}/")
    };

    await WaitForHealthyEndpointAsync(
        http,
        "health",
        "app runtime",
        TimeSpan.FromSeconds(options.RuntimeWaitSeconds),
        TimeSpan.FromSeconds(options.RuntimeWaitIntervalSeconds));

    using var response = await http.GetAsync("health");
    var body = await response.Content.ReadAsStringAsync();

    Console.WriteLine(JsonSerializer.Serialize(
        new AppRuntimeHealthProofResult(
            "Passed",
            GetAppRuntimeBaseUrl(options),
            (int)response.StatusCode,
            TryReadJsonElement(body)),
        jsonOptions));
}

async Task ServeStubAsync(ProofOptions options)
{
    await using var app = CreateStubApplication(options);
    await app.RunAsync();
}

WebApplication CreateStubApplication(ProofOptions options)
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls($"http://0.0.0.0:{options.StubPort}");
    builder.Logging.SetMinimumLevel(LogLevel.Warning);

    var app = builder.Build();

    app.MapGet("/health", () => Results.Ok(new
    {
        service = "SafarSuite Control Cloud registration stub",
        status = "Healthy"
    }));

    app.MapPost(
        "/api/v1/local-server/installations/{installationId}/registration",
        (string installationId, RegisterLocalServerInstallationRequest request) =>
            Results.Ok(new LocalServerInstallationRegistrationResponse(
                request.ClientId,
                installationId,
                "Active",
                DateTimeOffset.UtcNow,
                request.LocalServerVersion,
                request.DeploymentProfile ?? CreateDeploymentProfile(options))));

    app.MapGet(
        "/api/v1/local-server/installations/{installationId}/commands/pending",
        (string installationId) => Results.Ok(new PendingInstallationCommandsResponse(
            installationId,
            Array.Empty<InstallationCommandResponse>())));

    app.MapPost(
        "/api/v1/local-server/installations/{installationId}/heartbeat",
        (string installationId, ReportLocalServerHeartbeatRequest request) =>
            Results.Ok(new LocalServerHeartbeatResponse(
                Guid.NewGuid(),
                installationId,
                request.ClientId,
                "Received",
                DateTimeOffset.UtcNow,
                request.ReportedAtUtc,
                request.LicenseStatus,
                request.EntitlementVersion,
                request.PaidUntil,
                request.WarningStartsAt,
                request.GraceUntil,
                request.OfflineValidUntil,
                request.LocalServerVersion,
                request.Detail,
                request.DeploymentProfile ?? CreateDeploymentProfile(options),
                request.PairingStatus)));

    app.MapGet(
        "/api/v1/local-server/installations/{installationId}/entitlement-bundle",
        (string installationId, Guid clientId) =>
            Results.Ok(CreateEntitlementBundle(options with
            {
                ClientId = clientId,
                InstallationId = installationId
            })));

    app.MapPost(
        "/api/v1/local-server/installations/{installationId}/diagnostics",
        (string installationId) => Results.Ok(new
        {
            installationId,
            status = "Received",
            receivedAtUtc = DateTimeOffset.UtcNow
        }));

    return app;
}

async Task RequireHealthyControlCloudAsync(HttpClient http)
{
    using var response = await http.GetAsync("health");
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Control Cloud health check failed with {(int)response.StatusCode}: {body}");
    }
}

async Task RequireHealthyAppRuntimeAsync(HttpClient http)
{
    using var response = await http.GetAsync("health");
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"App runtime health check failed with {(int)response.StatusCode}: {body}");
    }
}

async Task<T> ReadJsonResponseAsync<T>(
    HttpResponseMessage response,
    string action)
{
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Could not {action}. HTTP {(int)response.StatusCode}: {body}");
    }

    return JsonSerializer.Deserialize<T>(body, jsonOptions)
        ?? throw new InvalidOperationException($"Could not parse response while trying to {action}.");
}

HttpClient CreateLocalApiHttpClient(ProofOptions options)
{
    if (!UseGeneratedLocalApiTls(options))
    {
        return new HttpClient
        {
            BaseAddress = new Uri($"{GetLocalApiHostBaseUrl(options)}/")
        };
    }

    var caCertificatePath = Path.Combine(
        options.OutputDirectory,
        "certs",
        "trust",
        "local-api-ca.pem");
    var trustedCa = LoadLocalApiCertificate(caCertificatePath);
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) =>
            ValidateLocalApiServerCertificate(request, certificate, errors, trustedCa)
    };

    return new HttpClient(handler)
    {
        BaseAddress = new Uri($"{GetLocalApiHostBaseUrl(options)}/")
    };
}

X509Certificate2 LoadLocalApiCertificate(string certificatePath)
{
    return certificatePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase)
        ? X509Certificate2.CreateFromPem(File.ReadAllText(certificatePath))
        : X509CertificateLoader.LoadCertificateFromFile(certificatePath);
}

bool ValidateLocalApiServerCertificate(
    HttpRequestMessage request,
    X509Certificate2? certificate,
    SslPolicyErrors sslPolicyErrors,
    X509Certificate2 trustedCa)
{
    if (certificate is null)
    {
        return false;
    }

    if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)
        || sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
    {
        return false;
    }

    return IsValidWithCustomTrust(certificate, trustedCa)
        || IsValidWithPinnedRoot(certificate, trustedCa);
}

bool IsValidWithCustomTrust(
    X509Certificate2 certificate,
    X509Certificate2 trustedCa)
{
    using var chain = new X509Chain();
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    chain.ChainPolicy.CustomTrustStore.Add(trustedCa);

    return chain.Build(certificate);
}

bool IsValidWithPinnedRoot(
    X509Certificate2 certificate,
    X509Certificate2 trustedCa)
{
    using var chain = new X509Chain();
    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
    chain.ChainPolicy.ExtraStore.Add(trustedCa);
    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

    if (!chain.Build(certificate))
    {
        var hasOnlyExpectedRootErrors = chain.ChainStatus.All(status =>
            status.Status is X509ChainStatusFlags.UntrustedRoot
                or X509ChainStatusFlags.PartialChain
                or X509ChainStatusFlags.NoError);

        if (!hasOnlyExpectedRootErrors)
        {
            return false;
        }
    }

    var root = chain.ChainElements.Count > 0
        ? chain.ChainElements[^1].Certificate
        : certificate;
    var rootHash = root.GetCertHash(HashAlgorithmName.SHA256);
    var trustedHash = trustedCa.GetCertHash(HashAlgorithmName.SHA256);

    return CryptographicOperations.FixedTimeEquals(rootHash, trustedHash);
}

async Task WaitForHealthyLocalApiAsync(
    HttpClient http,
    ProofOptions options)
{
    await WaitForHealthyEndpointAsync(
        http,
        "health",
        "Local API",
        TimeSpan.FromSeconds(options.RuntimeWaitSeconds),
        TimeSpan.FromSeconds(options.RuntimeWaitIntervalSeconds));
}

async Task WaitForHealthyEndpointAsync(
    HttpClient http,
    string relativeUrl,
    string serviceName,
    TimeSpan timeout,
    TimeSpan interval)
{
    var startedAt = DateTimeOffset.UtcNow;
    Exception? lastException = null;
    HttpResponseMessage? lastResponse = null;

    while (DateTimeOffset.UtcNow - startedAt < timeout)
    {
        try
        {
            lastResponse?.Dispose();
            lastResponse = await http.GetAsync(relativeUrl);

            if (lastResponse.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (Exception exception)
        {
            lastException = exception;
        }

        await Task.Delay(interval);
    }

    var lastStatus = lastResponse is null
        ? "no HTTP response"
        : $"HTTP {(int)lastResponse.StatusCode}";
    var detail = lastException is null
        ? lastStatus
        : $"{lastStatus}; {lastException}";

    throw new TimeoutException(
        $"{serviceName} did not become healthy within {timeout.TotalSeconds:0} seconds ({detail}).");
}

IReadOnlyList<string> BuildDockerComposeArguments(
    ProofOptions options,
    params string[] commandArguments)
{
    var arguments = new List<string>
    {
        "compose"
    };

    if (options.IncludeAppRuntime)
    {
        arguments.Add("--profile");
        arguments.Add("app-runtime");
    }

    arguments.Add("--env-file");
    arguments.Add("local-server.env");
    arguments.Add("-f");
    arguments.Add("docker-compose.yml");
    arguments.AddRange(commandArguments);

    return arguments;
}

async Task<CommandResult> RunProcessAsync(
    string fileName,
    IReadOnlyList<string> arguments,
    string workingDirectory,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    Directory.CreateDirectory(Path.Combine(workingDirectory, ".docker-config"));

    using var process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.WorkingDirectory = workingDirectory;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.Environment["DOCKER_CONFIG"] = Path.Combine(workingDirectory, ".docker-config");

    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    var stdOut = new StringBuilder();
    var stdErr = new StringBuilder();
    process.OutputDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            stdOut.AppendLine(eventArgs.Data);
        }
    };
    process.ErrorDataReceived += (_, eventArgs) =>
    {
        if (eventArgs.Data is not null)
        {
            stdErr.AppendLine(eventArgs.Data);
        }
    };

    if (!process.Start())
    {
        throw new InvalidOperationException($"Could not start process '{fileName}'.");
    }

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var waitTask = process.WaitForExitAsync(timeoutCancellation.Token);
    var timeoutTask = Task.Delay(timeout, cancellationToken);

    if (await Task.WhenAny(waitTask, timeoutTask) != waitTask)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }

        throw new TimeoutException(
            $"Process '{fileName} {string.Join(" ", arguments)}' exceeded {timeout.TotalSeconds:0} seconds.");
    }

    timeoutCancellation.Cancel();
    await waitTask;

    return new CommandResult(
        process.ExitCode,
        stdOut.ToString(),
        stdErr.ToString());
}

void WriteRealCloudProofFiles(
    ProofOptions options,
    ControlCloudEnvelope seedEnvelope,
    ControlCloudReceiveEnvelopeResponse seed,
    LocalServerBootstrapPackageResponse package)
{
    var bootstrapBundleJson = JsonSerializer.Serialize(package.SignedBundle, bundleJsonOptions);
    var bootstrapBundleSha256 = ComputeSha256(bootstrapBundleJson);

    Directory.CreateDirectory(Path.Combine(options.OutputDirectory, "certs", "local-api"));
    Directory.CreateDirectory(Path.Combine(options.OutputDirectory, "certs", "trust"));
    PrepareLocalApiCertificateArtifacts(options);

    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "docker-compose.yml"),
        GetArtifactContent(package, "docker-compose.yml"));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "local-server.env"),
        BuildEnvironmentFileFromPackage(options, package));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "runtime-services.manifest.json"),
        GetArtifactContent(package, "runtime-services.manifest.json"));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "bootstrap-bundle.json"),
        bootstrapBundleJson);
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "control-cloud-bootstrap-package.json"),
        JsonSerializer.Serialize(package, jsonOptions));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "control-desk-entitlement-envelope.json"),
        JsonSerializer.Serialize(seedEnvelope, jsonOptions));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "control-desk-entitlement-seed-response.json"),
        JsonSerializer.Serialize(seed, jsonOptions));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "proof-notes.txt"),
        BuildRealCloudProofNotes(options, package, seed));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "clean-machine-install.sh"),
        BuildCleanMachineInstallScript(options, package, bootstrapBundleSha256));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "clean-machine-verify.sh"),
        BuildCleanMachineVerifyScript(options));
    File.WriteAllText(
        Path.Combine(options.OutputDirectory, "clean-machine-rerun-secret-proof.sh"),
        BuildCleanMachineRerunSecretProofScript());
}

string BuildCleanMachineInstallScript(
    ProofOptions options,
    LocalServerBootstrapPackageResponse package,
    string bootstrapBundleSha256)
{
    var profile = package.DeploymentProfile;
    var installScriptUrl = $"{options.ControlCloudApiBaseUrl.TrimEnd('/')}/install/safarsuite-local-server/install.sh";
    var appActivationSigningKey = ReadAppActivationSigningKey(package);
    var lines = new List<string>
    {
        "#!/usr/bin/env bash",
        "set -euo pipefail",
        "",
        "package_dir=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")\" && pwd)\"",
        "target_root=\"${SAFARSUITE_CLEAN_TARGET_ROOT:-$package_dir/../target}\"",
        "",
        "set_default() {",
        "  name=\"$1\"",
        "  value=\"$2\"",
        "  if [ -z \"${!name:-}\" ]; then",
        "    printf -v \"$name\" '%s' \"$value\"",
        "    export \"$name\"",
        "  fi",
        "}",
        ""
    };

    AddSetDefaultLines(
        lines,
        [
            ("COMPOSE_PROFILES", "app-runtime"),
            ("SAFARSUITE_INSTALL_SCRIPT_URL", installScriptUrl),
            ("SAFARSUITE_CONTROL_CLOUD_URL", package.CloudBaseUrl.TrimEnd('/')),
            ("SAFARSUITE_CLIENT_ID", package.ClientId.ToString("D")),
            ("SAFARSUITE_INSTALLATION_ID", package.InstallationId),
            ("SAFARSUITE_SETUP_TOKEN", package.SetupToken),
            ("SAFARSUITE_BOOTSTRAP_MODE", profile.BootstrapMode),
            ("SAFARSUITE_CLIENT_DEPLOYMENT_MODE", profile.ClientDeploymentMode),
            ("SAFARSUITE_SITE_ID", profile.SiteId),
            ("SAFARSUITE_SITE_ROLE", profile.SiteRole),
            ("SAFARSUITE_PARENT_SITE_ID", profile.ParentSiteId ?? ""),
            ("SAFARSUITE_BRANCH_CODE", profile.BranchCode ?? ""),
            ("SAFARSUITE_SYNC_TOPOLOGY_ID", profile.SyncTopologyId ?? ""),
            ("SAFARSUITE_LOCAL_SERVER_VERSION", package.LocalServerVersion),
            ("SAFARSUITE_APP_VERSION", package.RuntimePlan?.SafarSuiteAppVersion ?? options.AppVersion),
            ("SAFARSUITE_LOCAL_SERVER_IMAGE", options.LocalServerImage),
            ("SAFARSUITE_APP_IMAGE", options.AppImage),
            ("SAFARSUITE_LOCAL_SERVER_HTTP_PORT", options.LocalServerHttpPort.ToString()),
            ("SAFARSUITE_APP_HTTP_PORT", options.AppHttpPort.ToString()),
            ("SAFARSUITE_LOCAL_API_PUBLIC_URL", GetLocalApiHostBaseUrl(options)),
            ("SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID", "compose-proof-manager-session"),
            ("SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET", "compose-proof-manager-session-secret-change-before-production"),
            ("SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES", "60"),
            ("SAFARSUITE_LOCAL_API_TLS_MODE", options.LocalApiTlsMode),
            ("SAFARSUITE_LOCAL_API_ASPNETCORE_URLS", GetLocalApiAspNetCoreUrls(options)),
            ("SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES", "local-api,localhost"),
            ("SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES", "127.0.0.1"),
            ("SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS", options.LocalApiCertificateDays.ToString()),
            ("SAFARSUITE_ENTITLEMENT_SIGNING_KEY_ID", package.SignedBundle.Signature.KeyId),
            ("SAFARSUITE_ENTITLEMENT_SIGNING_SECRET", options.SigningSecret),
            ("SAFARSUITE_APP_ACTIVATION_SIGNING_KEY_ID", appActivationSigningKey.SigningKeyId),
            ("SAFARSUITE_APP_ACTIVATION_PUBLIC_KEY_PEM", EscapeDockerEnvValue(appActivationSigningKey.PublicKeyPem)),
            ("SAFARSUITE_BOOTSTRAP_BUNDLE_SHA256", bootstrapBundleSha256),
            ("SAFARSUITE_START_COMPOSE", "true"),
            ("SAFARSUITE_IMPORT_BOOTSTRAP_BUNDLE_AFTER_START", "true")
        ]);

    lines.AddRange(
    [
        "set_default SAFARSUITE_LOCAL_SERVER_CONFIG_DIR \"$target_root/etc\"",
        "set_default SAFARSUITE_LOCAL_SERVER_STATE_DIR \"$target_root/var\"",
        "set_default SAFARSUITE_BOOTSTRAP_BUNDLE_FILE \"$package_dir/bootstrap-bundle.json\"",
        "",
        "mkdir -p \"$target_root/etc\" \"$target_root/var\"",
        "curl -fsSL \"$SAFARSUITE_INSTALL_SCRIPT_URL\" -o \"$package_dir/safarsuite-install.sh\"",
        "chmod +x \"$package_dir/safarsuite-install.sh\"",
        "bash \"$package_dir/safarsuite-install.sh\""
    ]);

    return JoinScriptLines(lines);
}

string BuildCleanMachineVerifyScript(ProofOptions options)
{
    var modulePath = $"api/v1/local-server/modules/{Uri.EscapeDataString(options.RequiredModule)}/access?requestedBy=clean-machine-proof";
    if (!string.IsNullOrWhiteSpace(options.AsOfDate))
    {
        modulePath += $"&asOfDate={Uri.EscapeDataString(options.AsOfDate)}";
    }

    return JoinScriptLines(
    [
        "#!/usr/bin/env bash",
        "set -euo pipefail",
        "",
        "package_dir=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")\" && pwd)\"",
        "target_root=\"${SAFARSUITE_CLEAN_TARGET_ROOT:-$package_dir/../target}\"",
        $"local_api_base_url=\"${{SAFARSUITE_LOCAL_API_PUBLIC_URL:-{GetLocalApiHostBaseUrl(options)}}}\"",
        "curl_args=()",
        "",
        "if [[ \"$local_api_base_url\" == https://* ]]; then",
        "  ca_file=\"${SAFARSUITE_LOCAL_API_CA_FILE:-$target_root/etc/certs/trust/local-api-ca.pem}\"",
        "  if [ ! -f \"$ca_file\" ]; then",
        "    echo \"Missing Local API CA certificate: $ca_file\" >&2",
        "    exit 12",
        "  fi",
        "  curl_args=(--cacert \"$ca_file\")",
        "  if curl --version 2>/dev/null | head -n 1 | grep -qi 'Schannel' &&",
        "     curl --help all 2>/dev/null | grep -q -- '--ssl-no-revoke'; then",
        "    curl_args+=(--ssl-no-revoke)",
        "  fi",
        "fi",
        "",
        "curl -fsS \"${curl_args[@]}\" \"$local_api_base_url/health\"",
        "curl -fsS \"${curl_args[@]}\" -X POST \"$local_api_base_url/api/v1/local-server/entitlement/pull\"",
        "curl -fsS \"${curl_args[@]}\" -X POST \"$local_api_base_url/api/v1/local-server/heartbeat\"",
        "curl -fsS \"${curl_args[@]}\" -X POST \"$local_api_base_url/api/v1/local-server/commands/process\"",
        $"curl -fsS \"${{curl_args[@]}}\" \"$local_api_base_url/{modulePath}\"",
        "echo \"Clean-machine runtime verification passed against $local_api_base_url.\""
    ]);
}

string BuildCleanMachineRerunSecretProofScript()
{
    return JoinScriptLines(
    [
        "#!/usr/bin/env bash",
        "set -euo pipefail",
        "",
        "package_dir=\"$(cd \"$(dirname \"${BASH_SOURCE[0]}\")\" && pwd)\"",
        "target_root=\"${SAFARSUITE_CLEAN_TARGET_ROOT:-$package_dir/../target}\"",
        "",
        "write_secret_hashes() {",
        "  output_file=\"$1\"",
        "  secret_dir=\"$target_root/var/secrets\"",
        "  cert_private_dir=\"$target_root/var/certs-private/local-api\"",
        "",
        "  if [ ! -d \"$secret_dir\" ] || [ ! -d \"$cert_private_dir\" ]; then",
        "    echo \"Expected generated secret directories are missing under $target_root/var.\" >&2",
        "    exit 13",
        "  fi",
        "",
        "  find \"$secret_dir\" \"$cert_private_dir\" -type f -print0 |",
        "    sort -z |",
        "    xargs -0 sha256sum > \"$output_file\"",
        "}",
        "",
        "\"$package_dir/clean-machine-install.sh\"",
        "write_secret_hashes \"$target_root/first-secret-sha256.txt\"",
        "\"$package_dir/clean-machine-install.sh\"",
        "write_secret_hashes \"$target_root/second-secret-sha256.txt\"",
        "diff -u \"$target_root/first-secret-sha256.txt\" \"$target_root/second-secret-sha256.txt\"",
        "echo \"Clean-machine rerun secret hash proof passed; generated secret files were reused.\""
    ]);
}

void AddSetDefaultLines(
    List<string> lines,
    IReadOnlyCollection<(string Name, string Value)> values)
{
    foreach (var (name, value) in values)
    {
        lines.Add($"set_default {name} {QuoteForShell(value)}");
    }
}

string JoinScriptLines(IEnumerable<string> lines)
{
    return string.Join("\n", lines) + "\n";
}

LocalServerSignedBootstrapBundleResponse CreateBootstrapBundle(ProofOptions options)
{
    var payload = new LocalServerBootstrapPackagePayloadResponse(
        ControlCloudLocalServerBootstrapPackageFormat.Version,
        Guid.NewGuid(),
        Guid.NewGuid(),
        options.ClientId,
        options.InstallationId,
        ControlCloudBootstrapModes.OnlineBootstrap,
        CreateDeploymentProfile(options),
        options.CloudBaseUrl.TrimEnd('/'),
        options.LocalServerVersion,
        "compose-proof-token",
        DateTimeOffset.UtcNow.AddHours(24),
        DateTimeOffset.UtcNow,
        $"{options.CloudBaseUrl.TrimEnd('/')}/install/safarsuite-local-server/install.sh",
        "",
        Array.Empty<LocalServerBootstrapPackageArtifactResponse>(),
        CreateEndpoints(options),
        CreateRuntimePlan(options),
        new SafarSuiteAppActivationSigningKeyResponse(
            AppProofActivationSigningKeyId,
            AppProofActivationPublicKeyPem));
    var payloadJson = JsonSerializer.Serialize(payload, bundleJsonOptions);
    var signature = new LocalServerBootstrapPackageSignatureResponse(
        "HMAC-SHA256",
        options.SigningKeyId,
        ComputeSha256(payloadJson),
        Sign(options.SigningSecret, payloadJson));

    return new LocalServerSignedBootstrapBundleResponse(
        payloadJson,
        payload,
        signature);
}

ClientPortalSignedEntitlementBundleResponse CreateEntitlementBundle(ProofOptions options)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var paidUntil = today.AddDays(30);
    var payload = new ClientPortalEntitlementBundlePayloadResponse(
        "safarsuite-entitlement-v1",
        "SafarSuite.ControlCloud",
        "SafarSuite.ClientPortal",
        options.ClientId,
        options.InstallationId,
        EntitlementVersion: 1,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "COMPOSE-PROOF",
        "Active",
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        today,
        paidUntil,
        paidUntil.AddDays(-7),
        paidUntil.AddDays(7),
        paidUntil.AddDays(14),
        AllowedDevices: 10,
        AllowedBranches: 1,
        Modules:
        [
            new ClientPortalEntitlementBundleModuleResponse("Accounting", "Active", true),
            new ClientPortalEntitlementBundleModuleResponse("Reports", "Active", true)
        ]);
    var payloadJson = JsonSerializer.Serialize(payload, bundleJsonOptions);
    var signature = new ClientPortalEntitlementBundleSignatureResponse(
        "HMAC-SHA256",
        options.SigningKeyId,
        ComputeSha256(payloadJson),
        Sign(options.SigningSecret, payloadJson));

    return new ClientPortalSignedEntitlementBundleResponse(
        payloadJson,
        payload,
        signature);
}

ControlCloudEnvelope CreateEntitlementSnapshotEnvelope(ProofOptions options)
{
    var now = DateTimeOffset.UtcNow;
    var today = DateOnly.FromDateTime(now.UtcDateTime);
    var paidUntil = today.AddDays(30);
    var messageId = Guid.NewGuid();
    var entitlementSnapshotId = Guid.NewGuid();
    var payload = new EntitlementSnapshotIssuedProofPayload(
        EventVersion: "1",
        EntitlementSnapshotId: entitlementSnapshotId,
        ClientId: options.ClientId,
        ContractId: Guid.NewGuid(),
        SourceInvoiceId: Guid.NewGuid(),
        SourceInvoiceNumber: $"COMPOSE-PROOF-{now:yyyyMMddHHmmss}",
        Status: "Active",
        PaidUntil: paidUntil,
        GraceUntil: paidUntil.AddDays(7),
        OfflineValidUntil: paidUntil.AddDays(14),
        AllowedDevices: 10,
        AllowedBranches: 1,
        IssuedAtUtc: now,
        Modules:
        [
            new EntitlementSnapshotIssuedProofModule("Accounting", IsEnabled: true),
            new EntitlementSnapshotIssuedProofModule("Reports", IsEnabled: true)
        ]);
    var payloadJson = JsonSerializer.Serialize(payload, bundleJsonOptions);
    var payloadSha256 = ComputeSha256(payloadJson);
    var occurredAtUtc = now;
    var preparedAtUtc = now;
    var idempotencyKey = $"{options.ControlDeskSourceSystem}:{messageId:N}";
    var signature = SignControlDeskEnvelope(
        options.ControlDeskSigningSecret,
        messageId,
        "EntitlementSnapshotIssued",
        "EntitlementSnapshot",
        entitlementSnapshotId.ToString("D"),
        options.ControlDeskSourceSystem,
        options.ControlDeskSourceEnvironment,
        occurredAtUtc,
        preparedAtUtc,
        idempotencyKey,
        payloadSha256);

    using var document = JsonDocument.Parse(payloadJson);
    var envelope = new ControlCloudEnvelope(
        EnvelopeVersion: "1",
        MessageId: messageId,
        MessageType: "EntitlementSnapshotIssued",
        SubjectType: "EntitlementSnapshot",
        SubjectId: entitlementSnapshotId.ToString("D"),
        SourceSystem: options.ControlDeskSourceSystem,
        SourceEnvironment: options.ControlDeskSourceEnvironment,
        OccurredAtUtc: occurredAtUtc,
        PreparedAtUtc: preparedAtUtc,
        IdempotencyKey: idempotencyKey,
        Payload: document.RootElement.Clone(),
        Signature: new ControlCloudEnvelopeSignature(
            "HMAC-SHA256",
            options.ControlDeskSigningKeyId,
            payloadSha256,
            signature));

    return CanonicalizeControlDeskEnvelopeForHttp(options, envelope);
}

ControlCloudEnvelope CanonicalizeControlDeskEnvelopeForHttp(
    ProofOptions options,
    ControlCloudEnvelope envelope)
{
    var envelopeJson = JsonSerializer.Serialize(envelope, bundleJsonOptions);
    using var document = JsonDocument.Parse(envelopeJson);
    var payload = document.RootElement.GetProperty("payload").Clone();
    var payloadSha256 = ComputeSha256(payload.GetRawText());
    var signature = SignControlDeskEnvelope(
        options.ControlDeskSigningSecret,
        envelope.MessageId,
        envelope.MessageType,
        envelope.SubjectType,
        envelope.SubjectId,
        envelope.SourceSystem,
        envelope.SourceEnvironment,
        envelope.OccurredAtUtc,
        envelope.PreparedAtUtc,
        envelope.IdempotencyKey,
        payloadSha256);

    return envelope with
    {
        Payload = payload,
        Signature = envelope.Signature with
        {
            PayloadSha256 = payloadSha256,
            Value = signature
        }
    };
}

LocalServerDeploymentProfileResponse CreateDeploymentProfile(ProofOptions options)
{
    return new LocalServerDeploymentProfileResponse(
        ControlCloudBootstrapModes.OnlineBootstrap,
        SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
        options.SiteId,
        SafarSuiteDeploymentSiteRoles.Hq,
        ParentSiteId: null,
        BranchCode: "HQ",
        SyncTopologyId: "compose-proof");
}

LocalServerBootstrapPackageEndpointsResponse CreateEndpoints(ProofOptions options)
{
    var cloudBaseUrl = options.CloudBaseUrl.TrimEnd('/');
    var installationId = Uri.EscapeDataString(options.InstallationId);

    return new LocalServerBootstrapPackageEndpointsResponse(
        $"{cloudBaseUrl}/api/v1/local-server/installations/{installationId}/registration",
        $"{cloudBaseUrl}/api/v1/local-server/installations/{installationId}/entitlement-bundle?clientId={options.ClientId:D}",
        $"{cloudBaseUrl}/api/v1/local-server/installations/{installationId}/heartbeat",
        $"{cloudBaseUrl}/api/v1/local-server/installations/{installationId}/commands/pending",
        $"{cloudBaseUrl}/api/v1/local-server/installations/{installationId}/diagnostics");
}

LocalServerBootstrapRuntimePlanResponse CreateRuntimePlan(ProofOptions options)
{
    var localApiBaseUrl = GetLocalApiContainerBaseUrl(options);

    return new LocalServerBootstrapRuntimePlanResponse(
        "DockerCompose",
        "safarsuite-local-server",
        "/etc/safarsuite/local-server",
        "/var/lib/safarsuite/local-server",
        options.LocalServerVersion,
        options.AppVersion,
        [
            new LocalServerBootstrapRuntimeServiceResponse(
                ServiceName: "local-api",
                ServiceRole: "Local entitlement, diagnostics, and module-gateway API",
                StartsByDefault: true,
                ComposeProfile: null,
                ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                PublishedPortEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_HTTP_PORT",
                InternalBaseUrl: localApiBaseUrl,
                HealthUrl: $"{localApiBaseUrl}/health",
                DependsOn: ["local-db"]),
            new LocalServerBootstrapRuntimeServiceResponse(
                ServiceName: "local-worker",
                ServiceRole: "Background entitlement pull and heartbeat reporting",
                StartsByDefault: true,
                ComposeProfile: null,
                ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                PublishedPortEnvironmentVariable: null,
                InternalBaseUrl: "n/a",
                HealthUrl: "n/a",
                DependsOn: ["local-db"]),
            new LocalServerBootstrapRuntimeServiceResponse(
                ServiceName: "local-agent",
                ServiceRole: "Support command polling, diagnostics, and acknowledgement bridge",
                StartsByDefault: true,
                ComposeProfile: null,
                ImageEnvironmentVariable: "SAFARSUITE_LOCAL_SERVER_IMAGE",
                PublishedPortEnvironmentVariable: null,
                InternalBaseUrl: "n/a",
                HealthUrl: "n/a",
                DependsOn: ["local-db"]),
            new LocalServerBootstrapRuntimeServiceResponse(
                ServiceName: "safarsuite-app",
                ServiceRole: "Customer-facing SafarSuite application runtime",
                StartsByDefault: false,
                ComposeProfile: "app-runtime",
                ImageEnvironmentVariable: "SAFARSUITE_APP_IMAGE",
                PublishedPortEnvironmentVariable: "SAFARSUITE_APP_HTTP_PORT",
                InternalBaseUrl: "http://safarsuite-app:5280",
                HealthUrl: "http://safarsuite-app:5280/health",
                DependsOn: ["local-api", "local-db"])
        ]);
}

string BuildEnvironmentFile(ProofOptions options)
{
    var localApiBaseUrl = GetLocalApiContainerBaseUrl(options);

    return string.Join(
        Environment.NewLine,
        $"SAFARSUITE_CONTROL_CLOUD_URL={options.CloudBaseUrl.TrimEnd('/')}",
        $"SAFARSUITE_CLIENT_ID={options.ClientId:D}",
        $"SAFARSUITE_INSTALLATION_ID={options.InstallationId}",
        $"SAFARSUITE_BOOTSTRAP_MODE={ControlCloudBootstrapModes.OnlineBootstrap}",
        $"SAFARSUITE_CLIENT_DEPLOYMENT_MODE={SafarSuiteClientDeploymentModes.CloudSyncMultiBranch}",
        $"SAFARSUITE_SITE_ID={options.SiteId}",
        $"SAFARSUITE_SITE_ROLE={SafarSuiteDeploymentSiteRoles.Hq}",
        "SAFARSUITE_PARENT_SITE_ID=",
        "SAFARSUITE_BRANCH_CODE=HQ",
        "SAFARSUITE_SYNC_TOPOLOGY_ID=compose-proof",
        $"SAFARSUITE_LOCAL_SERVER_VERSION={options.LocalServerVersion}",
        $"SAFARSUITE_LOCAL_SERVER_IMAGE={options.LocalServerImage}",
        $"SAFARSUITE_LOCAL_SERVER_HTTP_PORT={options.LocalServerHttpPort}",
        "SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=/etc/safarsuite/local-server",
        "SAFARSUITE_LOCAL_SERVER_STATE_DIR=/var/lib/safarsuite/local-server",
        $"SAFARSUITE_APP_VERSION={options.AppVersion}",
        $"SAFARSUITE_APP_IMAGE={options.AppImage}",
        $"SAFARSUITE_APP_HTTP_PORT={options.AppHttpPort}",
        $"SAFARSUITE_LOCAL_API_BASE_URL={localApiBaseUrl}",
        "SAFARSUITE_LOCAL_API_ACCESS_KEY=compose-proof-local-api-access-key",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID=compose-proof-manager-session",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET=compose-proof-manager-session-secret-change-before-production",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES=60",
        $"SAFARSUITE_LOCAL_API_TLS_MODE={options.LocalApiTlsMode}",
        $"SAFARSUITE_LOCAL_API_ASPNETCORE_URLS={GetLocalApiAspNetCoreUrls(options)}",
        $"SAFARSUITE_LOCAL_API_CERTIFICATE_PATH={GetLocalApiCertificatePath(options)}",
        $"SAFARSUITE_LOCAL_API_CERTIFICATE_PASSWORD={GetLocalApiCertificatePassword(options)}",
        $"SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH={GetLocalApiCaCertificatePath(options)}",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES=local-api,localhost",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES=127.0.0.1",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS=825",
        "SAFARSUITE_LOCAL_PAIRING_DISPLAY_NAME=SafarSuite - Compose Proof",
        $"SAFARSUITE_LOCAL_PAIRING_HTTPS_URL={GetLocalApiHostBaseUrl(options)}",
        "SAFARSUITE_LOCAL_PAIRING_MODE=ManagerApproval",
        $"SAFARSUITE_MODULE_GATEWAY_URL={localApiBaseUrl}",
        "SAFARSUITE_RUNTIME_MANIFEST_PATH=/etc/safarsuite/local-server/runtime-services.manifest.json",
        $"SAFARSUITE_LOCAL_DB_IMAGE={options.LocalDbImage}",
        "SAFARSUITE_LOCAL_DB_NAME=safarsuite_local",
        "SAFARSUITE_LOCAL_DB_USER=safarsuite",
        "SAFARSUITE_LOCAL_DB_PASSWORD=safarsuite_compose_proof_password",
        $"SAFARSUITE_REGISTRATION_URL={CreateEndpoints(options).RegistrationUrl}",
        $"SAFARSUITE_ENTITLEMENT_BUNDLE_URL={CreateEndpoints(options).EntitlementBundleUrl}",
        $"SAFARSUITE_HEARTBEAT_URL={CreateEndpoints(options).HeartbeatUrl}",
        $"SAFARSUITE_PENDING_COMMANDS_URL={CreateEndpoints(options).PendingCommandsUrl}",
        $"SAFARSUITE_DIAGNOSTICS_URL={CreateEndpoints(options).DiagnosticsUrl}",
        $"LocalServer__BootstrapTrust__SigningKeys__0__KeyId={options.SigningKeyId}",
        $"LocalServer__BootstrapTrust__SigningKeys__0__Secret={options.SigningSecret}",
        $"LocalServer__EntitlementTrust__SigningKeys__0__KeyId={options.SigningKeyId}",
        $"LocalServer__EntitlementTrust__SigningKeys__0__Secret={options.SigningSecret}",
        "DeploymentSecrets__Provider=Environment",
        $"ActivationSigning__SigningKeyId={AppProofActivationSigningKeyId}",
        $"ActivationSigning__PublicKeyPem={AppProofActivationPublicKeyPemEscaped}",
        "DeviceCredentials__SigningKeyId=app-profile-proof-device",
        "DeviceCredentials__SigningSecret=app-profile-proof-device-secret-change-before-production",
        "DeviceCredentials__ExpiresInDays=3650",
        "UserSessions__SigningKeyId=app-profile-proof-session",
        "UserSessions__SigningSecret=app-profile-proof-session-secret-change-before-production",
        "FirstManagerBootstrap__AllowSetupCodeFallback=false",
        "");
}

string BuildEnvironmentFileFromPackage(
    ProofOptions options,
    LocalServerBootstrapPackageResponse package)
{
    var profile = package.DeploymentProfile;
    var endpoints = package.Endpoints;
    var localApiBaseUrl = GetLocalApiContainerBaseUrl(options);
    var appActivationSigningKey = ReadAppActivationSigningKey(package);

    return string.Join(
        Environment.NewLine,
        $"SAFARSUITE_CONTROL_CLOUD_URL={package.CloudBaseUrl.TrimEnd('/')}",
        $"SAFARSUITE_CLIENT_ID={package.ClientId:D}",
        $"SAFARSUITE_INSTALLATION_ID={package.InstallationId}",
        $"SAFARSUITE_BOOTSTRAP_MODE={profile.BootstrapMode}",
        $"SAFARSUITE_CLIENT_DEPLOYMENT_MODE={profile.ClientDeploymentMode}",
        $"SAFARSUITE_SITE_ID={profile.SiteId}",
        $"SAFARSUITE_SITE_ROLE={profile.SiteRole}",
        $"SAFARSUITE_PARENT_SITE_ID={profile.ParentSiteId ?? ""}",
        $"SAFARSUITE_BRANCH_CODE={profile.BranchCode ?? ""}",
        $"SAFARSUITE_SYNC_TOPOLOGY_ID={profile.SyncTopologyId ?? ""}",
        $"SAFARSUITE_LOCAL_SERVER_VERSION={package.LocalServerVersion}",
        $"SAFARSUITE_LOCAL_SERVER_IMAGE={options.LocalServerImage}",
        $"SAFARSUITE_LOCAL_SERVER_HTTP_PORT={options.LocalServerHttpPort}",
        "SAFARSUITE_LOCAL_SERVER_CONFIG_DIR=/etc/safarsuite/local-server",
        "SAFARSUITE_LOCAL_SERVER_STATE_DIR=/var/lib/safarsuite/local-server",
        $"SAFARSUITE_APP_VERSION={package.RuntimePlan?.SafarSuiteAppVersion ?? options.AppVersion}",
        $"SAFARSUITE_APP_IMAGE={options.AppImage}",
        $"SAFARSUITE_APP_HTTP_PORT={options.AppHttpPort}",
        $"SAFARSUITE_LOCAL_API_BASE_URL={localApiBaseUrl}",
        "SAFARSUITE_LOCAL_API_ACCESS_KEY=compose-proof-local-api-access-key",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_KEY_ID=compose-proof-manager-session",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_SIGNING_SECRET=compose-proof-manager-session-secret-change-before-production",
        "SAFARSUITE_LOCAL_MANAGER_SESSION_MINUTES=60",
        $"SAFARSUITE_LOCAL_API_TLS_MODE={options.LocalApiTlsMode}",
        $"SAFARSUITE_LOCAL_API_ASPNETCORE_URLS={GetLocalApiAspNetCoreUrls(options)}",
        $"SAFARSUITE_LOCAL_API_CERTIFICATE_PATH={GetLocalApiCertificatePath(options)}",
        $"SAFARSUITE_LOCAL_API_CERTIFICATE_PASSWORD={GetLocalApiCertificatePassword(options)}",
        $"SAFARSUITE_LOCAL_API_CA_CERTIFICATE_PATH={GetLocalApiCaCertificatePath(options)}",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_DNS_NAMES=local-api,localhost",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_IP_ADDRESSES=127.0.0.1",
        "SAFARSUITE_LOCAL_API_CERTIFICATE_DAYS=825",
        "SAFARSUITE_LOCAL_PAIRING_DISPLAY_NAME=SafarSuite - Compose Proof",
        $"SAFARSUITE_LOCAL_PAIRING_HTTPS_URL={GetLocalApiHostBaseUrl(options)}",
        "SAFARSUITE_LOCAL_PAIRING_MODE=ManagerApproval",
        $"SAFARSUITE_MODULE_GATEWAY_URL={localApiBaseUrl}",
        "SAFARSUITE_RUNTIME_MANIFEST_PATH=/etc/safarsuite/local-server/runtime-services.manifest.json",
        $"SAFARSUITE_LOCAL_DB_IMAGE={options.LocalDbImage}",
        "SAFARSUITE_LOCAL_DB_NAME=safarsuite_local",
        "SAFARSUITE_LOCAL_DB_USER=safarsuite",
        "SAFARSUITE_LOCAL_DB_PASSWORD=safarsuite_compose_proof_password",
        $"SAFARSUITE_REGISTRATION_URL={endpoints.RegistrationUrl}",
        $"SAFARSUITE_ENTITLEMENT_BUNDLE_URL={endpoints.EntitlementBundleUrl}",
        $"SAFARSUITE_HEARTBEAT_URL={endpoints.HeartbeatUrl}",
        $"SAFARSUITE_PENDING_COMMANDS_URL={endpoints.PendingCommandsUrl}",
        $"SAFARSUITE_DIAGNOSTICS_URL={endpoints.DiagnosticsUrl}",
        $"LocalServer__BootstrapTrust__SigningKeys__0__KeyId={package.SignedBundle.Signature.KeyId}",
        $"LocalServer__BootstrapTrust__SigningKeys__0__Secret={options.SigningSecret}",
        $"LocalServer__EntitlementTrust__SigningKeys__0__KeyId={package.SignedBundle.Signature.KeyId}",
        $"LocalServer__EntitlementTrust__SigningKeys__0__Secret={options.SigningSecret}",
        "DeploymentSecrets__Provider=Environment",
        $"ActivationSigning__SigningKeyId={appActivationSigningKey.SigningKeyId}",
        $"ActivationSigning__PublicKeyPem={EscapeDockerEnvValue(appActivationSigningKey.PublicKeyPem)}",
        "DeviceCredentials__SigningKeyId=app-profile-proof-device",
        "DeviceCredentials__SigningSecret=app-profile-proof-device-secret-change-before-production",
        "DeviceCredentials__ExpiresInDays=3650",
        "UserSessions__SigningKeyId=app-profile-proof-session",
        "UserSessions__SigningSecret=app-profile-proof-session-secret-change-before-production",
        "FirstManagerBootstrap__AllowSetupCodeFallback=false",
        "");
}

LocalServerSignedFirstManagerSetupTokenResponse CreateFirstManagerSetupToken(
    ProofOptions options,
    Guid clientId,
    string installationId,
    Guid pendingDeviceRequestId)
{
    var issuedAtUtc = DateTimeOffset.UtcNow;
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
        "Compose Proof Manager",
        "compose.manager@safarsuite.local",
        "compose-bootstrap-proof",
        issuedAtUtc,
        issuedAtUtc.AddHours(1));
    var payloadJson = JsonSerializer.Serialize(payload, bundleJsonOptions);
    var signature = new LocalServerBootstrapPackageSignatureResponse(
        "HMAC-SHA256",
        options.SigningKeyId,
        ComputeSha256(payloadJson),
        Sign(options.SigningSecret, payloadJson));

    return new LocalServerSignedFirstManagerSetupTokenResponse(
        payloadJson,
        payload,
        signature);
}

SafarSuiteAppActivationSigningKeyResponse ReadAppActivationSigningKey(
    LocalServerBootstrapPackageResponse package)
{
    return package.AppActivationSigningKey
        ?? package.SignedBundle.Payload.AppActivationSigningKey
        ?? throw new InvalidOperationException(
            "The Control Cloud bootstrap package did not include app activation signing-key metadata. Regenerate the package with a current Control Cloud build before generating app-runtime proof files.");
}

void PrepareLocalApiCertificateArtifacts(ProofOptions options)
{
    if (!UseGeneratedLocalApiTls(options))
    {
        return;
    }

    var certDirectory = Path.Combine(options.OutputDirectory, "certs", "local-api");
    var trustDirectory = Path.Combine(options.OutputDirectory, "certs", "trust");
    Directory.CreateDirectory(certDirectory);
    Directory.CreateDirectory(trustDirectory);

    var caCertificatePath = Path.Combine(trustDirectory, "local-api-ca.pem");
    var serverCertificatePath = Path.Combine(certDirectory, "local-api-server.crt");
    var serverPfxPath = Path.Combine(certDirectory, "local-api-server.pfx");

    if (File.Exists(caCertificatePath) && File.Exists(serverPfxPath))
    {
        return;
    }

    var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
    var notAfter = DateTimeOffset.UtcNow.AddDays(options.LocalApiCertificateDays);

    using var caKey = RSA.Create(4096);
    var caRequest = new CertificateRequest(
        "CN=SafarSuite Local API Compose Proof CA",
        caKey,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
        X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
        true));
    caRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(caRequest.PublicKey, false));

    using var caCertificate = caRequest.CreateSelfSigned(notBefore, notAfter);
    File.WriteAllText(caCertificatePath, caCertificate.ExportCertificatePem());

    using var serverKey = RSA.Create(2048);
    var serverRequest = new CertificateRequest(
        "CN=local-api",
        serverKey,
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);
    serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
    serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
        true));
    serverRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        new OidCollection { new("1.3.6.1.5.5.7.3.1") },
        false));
    var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
    subjectAlternativeNames.AddDnsName("local-api");
    subjectAlternativeNames.AddDnsName("localhost");
    subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
    serverRequest.CertificateExtensions.Add(subjectAlternativeNames.Build());
    serverRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(serverRequest.PublicKey, false));

    var serialNumber = RandomNumberGenerator.GetBytes(16);
    using var serverCertificateWithoutKey = serverRequest.Create(
        caCertificate,
        notBefore,
        notAfter,
        serialNumber);
    using var serverCertificate = serverCertificateWithoutKey.CopyWithPrivateKey(serverKey);
    File.WriteAllText(serverCertificatePath, serverCertificate.ExportCertificatePem());
    File.WriteAllBytes(
        serverPfxPath,
        serverCertificate.Export(X509ContentType.Pkcs12, GetLocalApiCertificatePassword(options)));
}

bool UseGeneratedLocalApiTls(ProofOptions options)
{
    return options.LocalApiTlsMode.Equals("GeneratedLocalCa", StringComparison.OrdinalIgnoreCase);
}

bool UseHttpOnlyLocalApi(ProofOptions options)
{
    return options.LocalApiTlsMode.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase);
}

string GetLocalApiContainerBaseUrl(ProofOptions options)
{
    return UseHttpOnlyLocalApi(options)
        ? "http://local-api:8080"
        : "https://local-api:8080";
}

string GetLocalApiHostBaseUrl(ProofOptions options)
{
    return UseHttpOnlyLocalApi(options)
        ? $"http://127.0.0.1:{options.LocalServerHttpPort}"
        : $"https://127.0.0.1:{options.LocalServerHttpPort}";
}

string GetLocalApiAspNetCoreUrls(ProofOptions options)
{
    return UseHttpOnlyLocalApi(options)
        ? "http://0.0.0.0:8080"
        : "https://0.0.0.0:8080";
}

string GetLocalApiCertificatePath(ProofOptions options)
{
    return UseGeneratedLocalApiTls(options)
        ? "/etc/safarsuite/local-server/certs/local-api/local-api-server.pfx"
        : "";
}

string GetLocalApiCaCertificatePath(ProofOptions options)
{
    return UseGeneratedLocalApiTls(options)
        ? "/etc/safarsuite/local-server/certs/trust/local-api-ca.pem"
        : "";
}

string GetLocalApiCertificatePassword(ProofOptions options)
{
    return UseGeneratedLocalApiTls(options)
        ? options.LocalApiCertificatePassword
        : "";
}

string GetLocalApiHostCurlTrustArgument(ProofOptions options)
{
    return UseGeneratedLocalApiTls(options)
        ? "--cacert certs/trust/local-api-ca.pem "
        : "";
}

string BuildProofNotes(ProofOptions options)
{
    var localApiHostBaseUrl = GetLocalApiHostBaseUrl(options);
    var curlTrustArgument = GetLocalApiHostCurlTrustArgument(options);

    return string.Join(
        Environment.NewLine,
        "SafarSuite LocalServer Compose bootstrap proof",
        "",
        $"Stub URL: {options.CloudBaseUrl}",
        $"Local API URL: {localApiHostBaseUrl}",
        "",
        "Commands:",
        $"dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- run-compose --output {options.OutputDirectory} --port {options.StubPort} --local-port {options.LocalServerHttpPort} --cleanup-compose true",
        "",
        "Manual commands:",
        $"dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- serve-stub --port {options.StubPort}",
        "docker compose --env-file local-server.env -f docker-compose.yml up -d",
        $"dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- verify-running-runtime --output {options.OutputDirectory} --local-port {options.LocalServerHttpPort}",
        $"curl -fsS {curlTrustArgument}{localApiHostBaseUrl}/health",
        $"curl -fsS {curlTrustArgument}-X POST -H \"Content-Type: application/json\" --data-binary @bootstrap-bundle.json {localApiHostBaseUrl}/api/v1/local-server/bootstrap-package/import",
        "");
}

string BuildRealCloudProofNotes(
    ProofOptions options,
    LocalServerBootstrapPackageResponse package,
    ControlCloudReceiveEnvelopeResponse seed)
{
    var localApiHostBaseUrl = GetLocalApiHostBaseUrl(options);
    var curlTrustArgument = GetLocalApiHostCurlTrustArgument(options);

    return string.Join(
        Environment.NewLine,
        "SafarSuite LocalServer real Control Cloud Compose bootstrap proof",
        "",
        $"Control Cloud API URL from host: {options.ControlCloudApiBaseUrl}",
        $"Control Cloud URL from containers: {package.CloudBaseUrl}",
        $"Local API URL: {localApiHostBaseUrl}",
        $"Client ID: {package.ClientId:D}",
        $"Installation ID: {package.InstallationId}",
        $"Seed status: {seed.Status}",
        $"Bootstrap package ID: {package.BootstrapPackageId:D}",
        "",
        "Clean target scripts:",
        "chmod +x clean-machine-*.sh",
        "SAFARSUITE_CLEAN_TARGET_ROOT=../target ./clean-machine-install.sh",
        "./clean-machine-verify.sh",
        "./clean-machine-rerun-secret-proof.sh",
        "",
        "Manual compose commands:",
        "docker compose --env-file local-server.env -f docker-compose.yml up -d",
        $"dotnet run --project tools/SafarSuite.LocalServer.ComposeBootstrapProof/SafarSuite.LocalServer.ComposeBootstrapProof.csproj -- verify-running-runtime --output {options.OutputDirectory} --local-port {options.LocalServerHttpPort}",
        $"curl -fsS {curlTrustArgument}{localApiHostBaseUrl}/health",
        $"curl -fsS {curlTrustArgument}-X POST -H \"Content-Type: application/json\" --data-binary @bootstrap-bundle.json {localApiHostBaseUrl}/api/v1/local-server/bootstrap-package/import",
        "");
}

string GetArtifactContent(
    LocalServerBootstrapPackageResponse package,
    string fileName)
{
    return package.Artifacts
        .FirstOrDefault(artifact => artifact.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
        ?.Content
        ?? throw new InvalidOperationException($"Bootstrap package did not include artifact '{fileName}'.");
}

string ComputeSha256(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var hash = SHA256.HashData(bytes);

    return Convert.ToHexString(hash).ToLowerInvariant();
}

string EscapeDockerEnvValue(string value)
{
    return "\"" + value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal) + "\"";
}

string QuoteForShell(string value)
{
    return $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
}

JsonElement? TryReadJsonElement(string value)
{
    try
    {
        using var document = JsonDocument.Parse(value);

        return document.RootElement.Clone();
    }
    catch (JsonException)
    {
        return null;
    }
}

string GetAppRuntimeBaseUrl(ProofOptions options)
{
    return string.IsNullOrWhiteSpace(options.AppBaseUrl)
        ? $"http://127.0.0.1:{options.AppHttpPort}"
        : options.AppBaseUrl.TrimEnd('/');
}

DateOnly ResolveAsOfDate(ProofOptions options)
{
    return DateOnly.TryParse(options.AsOfDate, out var parsed)
        ? parsed
        : DateOnly.FromDateTime(DateTime.UtcNow);
}

string Sign(string signingSecret, string payloadJson)
{
    var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
    var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

    return Convert.ToBase64String(signatureBytes);
}

string SignControlDeskEnvelope(
    string signingSecret,
    Guid messageId,
    string messageType,
    string subjectType,
    string subjectId,
    string sourceSystem,
    string sourceEnvironment,
    DateTimeOffset occurredAtUtc,
    DateTimeOffset preparedAtUtc,
    string idempotencyKey,
    string payloadSha256)
{
    var signatureInput = string.Join(
        "\n",
        "1",
        messageId.ToString("D"),
        messageType,
        subjectType,
        subjectId,
        sourceSystem,
        sourceEnvironment,
        occurredAtUtc.ToString("O"),
        preparedAtUtc.ToString("O"),
        idempotencyKey,
        payloadSha256);

    return Sign(signingSecret, signatureInput);
}

internal sealed record EntitlementSnapshotIssuedProofPayload(
    string EventVersion,
    Guid EntitlementSnapshotId,
    Guid ClientId,
    Guid ContractId,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<EntitlementSnapshotIssuedProofModule> Modules);

internal sealed record EntitlementSnapshotIssuedProofModule(
    string ModuleCode,
    bool IsEnabled);

internal sealed record AppActivationStateResponse(
    Guid ServerInstallationId,
    string Status,
    string FingerprintHash,
    string PublicKey,
    Guid? ActivationRequestId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RequestedAt,
    DateTimeOffset? ActivatedAt,
    bool BootstrapMode,
    bool LoginAllowed,
    bool WritesAllowed,
    bool DevicePairingAllowed,
    IReadOnlyList<string> Reasons);

internal sealed record AppModuleAccessResponse(
    string FormatVersion,
    Guid InstallationId,
    string ModuleCode,
    bool IsAllowed,
    string AccessState,
    string Reason,
    JsonElement? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? WarningStartsAt,
    DateOnly? GraceUntil,
    DateOnly? OfflineValidUntil,
    DateTimeOffset CheckedAtUtc);

internal sealed record CreateProviderAccessSessionRequest(
    string SharedSecret,
    string? Actor,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null);

internal sealed record CreateProviderOperatorSessionRequest(
    string Email,
    string Password,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null);

internal sealed record CreateProviderAccessSessionResponse(
    string AccessToken,
    string TokenType,
    string Actor,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);

internal sealed record LocalServerBootstrapImportProofResponse(
    Guid ClientId,
    string InstallationId,
    string BootstrapRegistrationStatus,
    JsonElement DeploymentProfile,
    string CloudRegistrationStatus,
    DateTimeOffset RegisteredAtUtc,
    string SignatureKeyId,
    string PayloadSha256);

internal sealed record LocalServerEntitlementPullProofResponse(
    DateTimeOffset PulledAtUtc,
    Guid ClientId,
    string InstallationId,
    long EntitlementVersion,
    DateOnly PaidUntil,
    DateOnly OfflineValidUntil);

internal sealed record LocalServerHeartbeatProofResponse(
    Guid ClientId,
    string InstallationId,
    string HeartbeatStatus,
    string LicenseStatus,
    long? EntitlementVersion,
    DateTimeOffset ReceivedAtUtc,
    JsonElement EntitlementState);

internal sealed record LocalServerCommandsProofResponse(
    int PendingCommandCount,
    int AppliedCount,
    int FailedCount,
    int RejectedCount,
    JsonElement Commands);

internal sealed record LocalServerRuntimeStatusProofResponse(
    bool HasBootstrapConfiguration,
    Guid? ClientId,
    string? InstallationId,
    string? RegistrationStatus,
    JsonElement? DeploymentProfile,
    string? CloudBaseUrl,
    string? LocalServerVersion,
    DateTimeOffset? SetupTokenExpiresAtUtc,
    DateTimeOffset? LastRegistrationAttemptUtc,
    DateTimeOffset? LastRegistrationSucceededAtUtc,
    string? LastRegistrationFailureCode,
    bool HasCachedEntitlement,
    long? EntitlementVersion,
    DateOnly? PaidUntil,
    DateOnly? OfflineValidUntil,
    DateTimeOffset? LastSuccessfulCloudTimeUtc,
    DateTimeOffset? LastLocalCheckAtUtc,
    bool ClockMovedBackwards);

internal sealed record LocalRuntimeProofResult(
    string ProofStatus,
    string LocalApiBaseUrl,
    string LocalApiTlsMode,
    Guid ClientId,
    string InstallationId,
    string BootstrapRegistrationStatus,
    string CloudRegistrationStatus,
    long PulledEntitlementVersion,
    string HeartbeatStatus,
    string LicenseStatus,
    int PendingCommandCount,
    int AppliedCommandCount,
    bool PairingDiscoveryHasBootstrap,
    string PairingMode,
    string FirstManagerDeviceStatus,
    string DevicePairingRequestStatus,
    string ApprovedDeviceStatus,
    string RevokedDeviceStatus,
    bool HasBootstrapConfiguration,
    bool HasCachedEntitlement,
    string ModuleCode,
    bool ModuleAllowed,
    string ModuleAccessState,
    long? ModuleEntitlementVersion);

internal sealed record AppRuntimeHealthProofResult(
    string ProofStatus,
    string AppBaseUrl,
    int HealthStatusCode,
    JsonElement? Health);

internal sealed record CommandResult(
    int ExitCode,
    string StdOut,
    string StdErr);

internal sealed record ProofOptions
{
    public string Mode { get; init; } = "generate";

    public string RepositoryRoot { get; init; } = Directory.GetCurrentDirectory();

    public string OutputDirectory { get; init; } = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".codex-run",
        "localserver-compose-proof");

    public Guid ClientId { get; init; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string InstallationId { get; init; } = "compose-main";

    public string SiteId { get; init; } = "hq-main";

    public string LocalServerVersion { get; init; } = "codex-smoke";

    public string AppVersion { get; init; } = "0.1.0";

    public string CloudBaseUrl { get; init; } = "http://host.docker.internal:51877";

    public string ControlCloudApiBaseUrl { get; init; } = "http://127.0.0.1:5127";

    public int StubPort { get; init; } = 51877;

    public int LocalServerHttpPort { get; init; } = 18080;

    public int AppHttpPort { get; init; } = 18081;

    public string AppBaseUrl { get; init; } = "";

    public bool IncludeAppRuntime { get; init; }

    public bool VerifyAppRuntime { get; init; }

    public bool CleanupCompose { get; init; } = true;

    public int RuntimeWaitSeconds { get; init; } = 120;

    public int RuntimeWaitIntervalSeconds { get; init; } = 2;

    public int CommandTimeoutSeconds { get; init; } = 180;

    public string LocalApiTlsMode { get; init; } = "GeneratedLocalCa";

    public string LocalApiCertificatePassword { get; init; } =
        "compose-proof-local-api-pfx-password-change-before-production";

    public int LocalApiCertificateDays { get; init; } = 825;

    public string RequiredModule { get; init; } = "Accounting";

    public string AsOfDate { get; init; } = "";

    public string LocalServerImage { get; init; } = "safarsuite-local-server:codex-smoke";

    public string LocalDbImage { get; init; } = "postgres:17-alpine";

    public string AppImage { get; init; } = "ghcr.io/danionwheels/localserver:0.1.0";

    public string SigningKeyId { get; init; } = "local-entitlement-dev";

    public string SigningSecret { get; init; } = "local-entitlement-signing-secret-change-before-cloud";

    public string ControlDeskSigningKeyId { get; init; } = "local-dev";

    public string ControlDeskSigningSecret { get; init; } =
        "local-development-signing-secret-change-before-cloud";

    public string ProviderAccessSecret { get; init; } =
        "local-development-provider-access-secret-change-before-cloud";

    public bool UseProviderSessionToken { get; init; } = true;

    public bool UseProviderOperatorSession { get; init; } = true;

    public string ProviderOperatorEmail { get; init; } = "provider.admin@safarsuite.local";

    public string ProviderOperatorPassword { get; init; } =
        "provider-dev-password-change-before-cloud";

    public string ControlDeskSourceSystem { get; init; } = "SafarSuite.ControlDesk";

    public string ControlDeskSourceEnvironment { get; init; } = "Local";

    public static ProofOptions Parse(string[] args)
    {
        var mode = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
            ? args[0]
            : "generate";
        var options = new ProofOptions { Mode = mode };
        var startIndex = mode == "generate" && (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
            ? 0
            : 1;
        var cloudBaseUrlProvided = false;

        for (var index = startIndex; index < args.Length; index++)
        {
            var arg = args[index];

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            var value = index + 1 < args.Length ? args[++index] : "";
            if (key.Equals("cloud-base-url", StringComparison.OrdinalIgnoreCase))
            {
                cloudBaseUrlProvided = true;
            }

            options = key switch
            {
                "output" => options with { OutputDirectory = Path.GetFullPath(value) },
                "repo-root" => options with { RepositoryRoot = Path.GetFullPath(value) },
                "cloud-base-url" => options with { CloudBaseUrl = value.TrimEnd('/') },
                "control-cloud-api-url" => options with { ControlCloudApiBaseUrl = value.TrimEnd('/') },
                "port" => options with { StubPort = int.Parse(value) },
                "local-port" => options with { LocalServerHttpPort = int.Parse(value) },
                "app-port" => options with { AppHttpPort = int.Parse(value) },
                "app-base-url" => options with { AppBaseUrl = value.TrimEnd('/') },
                "include-app-runtime" => options with { IncludeAppRuntime = bool.Parse(value) },
                "verify-app-runtime" => options with { VerifyAppRuntime = bool.Parse(value) },
                "cleanup-compose" => options with { CleanupCompose = bool.Parse(value) },
                "runtime-wait-seconds" => options with { RuntimeWaitSeconds = int.Parse(value) },
                "runtime-wait-interval-seconds" => options with { RuntimeWaitIntervalSeconds = int.Parse(value) },
                "command-timeout-seconds" => options with { CommandTimeoutSeconds = int.Parse(value) },
                "local-api-tls-mode" => options with { LocalApiTlsMode = NormalizeLocalApiTlsMode(value) },
                "local-api-certificate-password" => options with { LocalApiCertificatePassword = value },
                "local-api-certificate-days" => options with { LocalApiCertificateDays = int.Parse(value) },
                "required-module" => options with { RequiredModule = value },
                "as-of-date" => options with { AsOfDate = value },
                "client-id" => options with { ClientId = Guid.Parse(value) },
                "installation-id" => options with { InstallationId = value },
                "site-id" => options with { SiteId = value },
                "local-server-version" => options with { LocalServerVersion = value },
                "app-version" => options with { AppVersion = value },
                "local-server-image" => options with { LocalServerImage = value },
                "local-db-image" => options with { LocalDbImage = value },
                "app-image" => options with { AppImage = value },
                "signing-key-id" => options with { SigningKeyId = value },
                "signing-secret" => options with { SigningSecret = value },
                "signing-secret-file" => options with { SigningSecret = ReadRequiredSecretFile(value, key) },
                "control-desk-signing-key-id" => options with { ControlDeskSigningKeyId = value },
                "control-desk-signing-secret" => options with { ControlDeskSigningSecret = value },
                "control-desk-signing-secret-file" => options with { ControlDeskSigningSecret = ReadRequiredSecretFile(value, key) },
                "provider-access-secret" => options with { ProviderAccessSecret = value },
                "provider-access-secret-file" => options with { ProviderAccessSecret = ReadRequiredSecretFile(value, key) },
                "use-provider-session-token" => options with { UseProviderSessionToken = bool.Parse(value) },
                "use-provider-operator-session" => options with { UseProviderOperatorSession = bool.Parse(value) },
                "provider-operator-email" => options with { ProviderOperatorEmail = value },
                "provider-operator-password" => options with { ProviderOperatorPassword = value },
                "provider-operator-password-file" => options with { ProviderOperatorPassword = ReadRequiredSecretFile(value, key) },
                _ => throw new InvalidOperationException($"Unknown option '--{key}'.")
            };
        }

        if (!cloudBaseUrlProvided && options.StubPort != 51877)
        {
            options = options with
            {
                CloudBaseUrl = $"http://host.docker.internal:{options.StubPort}"
            };
        }

        return options;
    }

    private static string ReadRequiredSecretFile(
        string path,
        string optionName)
    {
        var resolvedPath = Path.GetFullPath(path.Trim());

        if (!File.Exists(resolvedPath))
        {
            throw new InvalidOperationException(
                $"--{optionName} points to a secret file that does not exist: {resolvedPath}");
        }

        var secret = File.ReadAllText(resolvedPath).Trim();

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                $"--{optionName} points to an empty secret file: {resolvedPath}");
        }

        return secret;
    }

    private static string NormalizeLocalApiTlsMode(string value)
    {
        return value switch
        {
            "" or "GeneratedLocalCa" or "generated-local-ca" or "generated_local_ca" or "generatedlocalca" => "GeneratedLocalCa",
            "HttpOnly" or "http-only" or "http_only" or "httponly" => "HttpOnly",
            _ => throw new InvalidOperationException($"Unsupported local API TLS mode '{value}'.")
        };
    }
}
