using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

var options = ProofOptions.Parse(args);

if (options.ShowHelp)
{
    Console.WriteLine(ProofOptions.HelpText);
    return 0;
}

Directory.CreateDirectory(options.OutputDirectory);

var checks = new List<string>();
ControlCloudProcess? cloudProcess = null;

try
{
    if (!options.SkipMigrations)
    {
        await using var migrationContext = CreateDbContext(options.ConnectionString);
        await migrationContext.Database.MigrateAsync();
        checks.Add("applied Control Cloud PostgreSQL migrations");
    }

    if (!options.UseExistingCloud)
    {
        cloudProcess = await ControlCloudProcess.StartAsync(options);
        checks.Add("started Control Cloud API with PostgreSQL persistence");
    }

    using var http = new HttpClient
    {
        BaseAddress = new Uri($"{options.ControlCloudBaseUrl}/")
    };

    await RequireHealthyAsync(http);
    checks.Add("verified Control Cloud health");

    var runId = Guid.NewGuid();
    var clientId = Guid.NewGuid();
    var installationId = $"postgres-proof-{runId:N}";
    var operatorEmail = $"postgres.proof.{runId:N}@safarsuite.local";
    var operatorPassword = $"PostgresProofPassword-{runId:N}!";
    var portalEmail = $"postgres.portal.{runId:N}@safarsuite.local";
    var portalPassword = $"PostgresPortalPassword-{runId:N}!";
    var localServerVersion = "postgres-proof-localserver-0.1";
    var deploymentProfile = new LocalServerDeploymentProfileResponse(
        ControlCloudBootstrapModes.OnlineBootstrap,
        SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
        SiteId: "postgres-proof-hq",
        SiteRole: SafarSuiteDeploymentSiteRoles.Hq,
        ParentSiteId: null,
        BranchCode: "HQ",
        SyncTopologyId: $"postgres-proof-{runId:N}");

    var adminSession = await SendJsonAsync<ProviderAccessSessionResponse>(
        http,
        HttpMethod.Post,
        "api/v1/provider-access/sessions",
        new CreateProviderAccessSessionRequest(
            options.ProviderAccessSecret,
            "postgres-proof-bootstrap",
            [
                ProviderAccessScopes.ClientPortalManage,
                ProviderAccessScopes.DeploymentPackagesRead,
                ProviderAccessScopes.DeploymentPackagesWrite,
                ProviderAccessScopes.ProviderOperatorsManage
            ],
            15));

    RequireBearerSession(adminSession, ProviderAccessScopes.ProviderOperatorsManage);
    Require(adminSession.Scopes.Contains(ProviderAccessScopes.ClientPortalManage), "admin session should include client portal manage scope.");
    Require(adminSession.Scopes.Contains(ProviderAccessScopes.DeploymentPackagesWrite), "admin session should include deployment package write scope.");
    checks.Add("minted bootstrap provider bearer session");

    var providerOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        http,
        HttpMethod.Post,
        "api/v1/provider-access/operators",
        new CreateProviderOperatorRequest(
            operatorEmail,
            "Postgres Proof Operator",
            operatorPassword,
            [
                ProviderAccessScopes.AppActivationRead,
                ProviderAccessScopes.AppActivationWrite,
                ProviderAccessScopes.DeploymentPackagesRead,
                ProviderAccessScopes.DeploymentPackagesWrite,
                ProviderAccessScopes.ProviderOperatorsManage
            ],
            "postgres-proof-bootstrap"),
        adminSession.AccessToken);

    Require(!string.IsNullOrWhiteSpace(providerOperator.UserId), "provider operator creation should return a user id.");
    checks.Add("created provider operator through EF-backed store");

    var operatorSession = await SendJsonAsync<ProviderAccessSessionResponse>(
        http,
        HttpMethod.Post,
        "api/v1/provider-access/operator-sessions",
        new CreateProviderOperatorSessionRequest(
            operatorEmail,
            operatorPassword,
            [ProviderAccessScopes.AppActivationRead, ProviderAccessScopes.AppActivationWrite],
            15));

    RequireBearerSession(operatorSession, ProviderAccessScopes.AppActivationRead);
    Require(operatorSession.Scopes.Contains(ProviderAccessScopes.AppActivationWrite), "operator session should include app activation write scope.");
    checks.Add("minted provider-operator bearer session from PostgreSQL");

    _ = await SendJsonAsync<JsonElement>(
        http,
        HttpMethod.Get,
        $"api/v1/control-cloud/clients/{clientId:D}/app-activation-issues?take=1",
        body: null,
        operatorSession.AccessToken);
    checks.Add("authorized provider-gated read with operator session");

    var envelope = CreateEntitlementSnapshotEnvelope(
        options,
        clientId,
        runId);
    var seed = await SendJsonAsync<ControlCloudReceiveEnvelopeResponse>(
        http,
        HttpMethod.Post,
        "api/v1/control-desk/messages",
        envelope);

    Require(
        seed.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase)
        || seed.Status.Equals("Duplicate", StringComparison.OrdinalIgnoreCase),
        $"entitlement seed should be accepted or duplicate, got '{seed.Status}'.");
    checks.Add("seeded entitlement projection through signed Control Desk envelope");

    var bootstrapPackage = await SendJsonAsync<LocalServerBootstrapPackageResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-package",
        new CreateLocalServerBootstrapPackageRequest(
            ExpiresInHours: 4,
            CreatedBy: "postgres-proof",
            DeploymentMode: ControlCloudBootstrapModes.OnlineBootstrap,
            LocalServerVersion: localServerVersion,
            SafarSuiteAppVersion: "postgres-proof-app-0.1",
            ClientDeploymentMode: deploymentProfile.ClientDeploymentMode,
            SiteId: deploymentProfile.SiteId,
            SiteRole: deploymentProfile.SiteRole,
            ParentSiteId: deploymentProfile.ParentSiteId,
            BranchCode: deploymentProfile.BranchCode,
            SyncTopologyId: deploymentProfile.SyncTopologyId),
        adminSession.AccessToken);

    Require(bootstrapPackage.ClientId == clientId, "bootstrap package should target proof client.");
    Require(bootstrapPackage.InstallationId == installationId, "bootstrap package should target proof installation.");
    RequireBootstrapSecretReadiness(bootstrapPackage, "PostgreSQL-backed bootstrap package");
    checks.Add("created PostgreSQL-backed bootstrap package and setup token with non-secret signing readiness");

    var packageRegister = await SendJsonAsync<LocalServerBootstrapPackageRegisterResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-packages?take=10",
        body: null,
        adminSession.AccessToken);

    Require(
        packageRegister.Packages.Any(package =>
            package.BootstrapPackageId == bootstrapPackage.BootstrapPackageId
            && package.BundleSha256 == bootstrapPackage.BundleSha256
            && package.PackageStatus == "Pending"),
        "bootstrap package register should include the generated pending package.");
    checks.Add("listed PostgreSQL-backed bootstrap package register through provider gate");

    var handoff = await SendJsonAsync<LocalServerBootstrapPackageHandoffResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-packages/{bootstrapPackage.BootstrapPackageId:D}/handoff",
        new MarkLocalServerBootstrapPackageHandoffRequest(
            Channel: "Secure email",
            Recipient: "postgres.proof.customer@safarsuite.local",
            MarkedBy: "postgres-proof",
            Note: "PostgreSQL proof handoff marker"),
        adminSession.AccessToken);

    Require(handoff.BootstrapPackageId == bootstrapPackage.BootstrapPackageId, "handoff should target generated bootstrap package.");
    Require(handoff.SetupTokenId == bootstrapPackage.SetupTokenId, "handoff should target generated setup token.");
    Require(handoff.ClientId == clientId, "handoff should target proof client.");
    Require(handoff.InstallationId == installationId, "handoff should target proof installation.");
    Require(handoff.HandoffStatus == "HandedOff", "handoff should be marked handed off.");

    var handoffAudit = await SendJsonAsync<ControlCloudAuditEventsResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/control-cloud/audit-events?clientId={clientId:D}&eventType=BootstrapPackageHandedOff&take=20");

    Require(
        handoffAudit.Events.Any(auditEvent =>
            auditEvent.EventType == "BootstrapPackageHandedOff"
            && auditEvent.ClientId == clientId
            && auditEvent.Detail.Contains(bootstrapPackage.BootstrapPackageId.ToString("D"), StringComparison.Ordinal)
            && auditEvent.Detail.Contains(installationId, StringComparison.Ordinal)
            && !auditEvent.Detail.Contains(bootstrapPackage.SetupToken, StringComparison.Ordinal)),
        "handoff audit trail should identify the package and installation without leaking the setup token secret.");
    checks.Add("marked bootstrap package handoff and verified non-secret audit trail");

    var appServerInstallationId = Guid.NewGuid();
    var pairingDescriptor = await SendJsonAsync<LocalServerPairingDescriptorResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/pairing-descriptor",
        new IssueLocalServerPairingDescriptorRequest(
            bootstrapPackage.BootstrapPackageId,
            bootstrapPackage.SetupTokenId,
            ClientCode: "POSTGRES",
            CustomerName: "Postgres Proof Customer",
            AppServerInstallationId: appServerInstallationId.ToString("D"),
            FingerprintHash: "postgres-proof-app-fingerprint",
            UrlCandidates: ["http://127.0.0.1:5280"],
            RequestedBy: "postgres-proof"),
        adminSession.AccessToken);

    Require(pairingDescriptor.FormatVersion == LocalServerPairingFormats.PairingDescriptorVersion, "pairing descriptor should use the v1 descriptor format.");
    Require(pairingDescriptor.ClientId == clientId, "pairing descriptor should target proof client.");
    Require(pairingDescriptor.ProviderInstallationId == installationId, "pairing descriptor should target proof installation.");
    Require(pairingDescriptor.BootstrapPackageId == bootstrapPackage.BootstrapPackageId, "pairing descriptor should bind the bootstrap package id.");
    Require(pairingDescriptor.SetupTokenId == bootstrapPackage.SetupTokenId, "pairing descriptor should bind the setup token id.");
    Require(pairingDescriptor.FingerprintHash == "postgres-proof-app-fingerprint", "pairing descriptor should carry the requested app fingerprint when no activation issue exists yet.");
    Require(pairingDescriptor.SignatureAlgorithm == "HMAC-SHA256", "pairing descriptor should use HMAC signature.");
    Require(!string.IsNullOrWhiteSpace(pairingDescriptor.SignatureKeyId), "pairing descriptor should expose the signing key id.");
    Require(!string.IsNullOrWhiteSpace(pairingDescriptor.PayloadSha256), "pairing descriptor should expose payload hash.");
    Require(!string.IsNullOrWhiteSpace(pairingDescriptor.Signature), "pairing descriptor should expose signature value.");
    checks.Add("issued provider-gated signed pairing descriptor");

    var registration = await SendJsonAsync<LocalServerInstallationRegistrationResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/registration",
        new RegisterLocalServerInstallationRequest(
            clientId,
            bootstrapPackage.SetupToken,
            localServerVersion,
            deploymentProfile));

    Require(registration.InstallationStatus.Equals("Active", StringComparison.OrdinalIgnoreCase), "registration should activate installation.");
    checks.Add("registered local-server installation");

    var firstManagerPendingDeviceRequestId = Guid.NewGuid();
    var firstManagerSetupToken = await SendJsonAsync<IssueLocalServerFirstManagerSetupTokenResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/first-manager-setup-token",
        new IssueLocalServerFirstManagerSetupTokenRequest(
            firstManagerPendingDeviceRequestId,
            "Postgres Proof Manager",
            "postgres.proof.manager@safarsuite.local",
            "postgres-proof",
            ExpiresInHours: 24),
        adminSession.AccessToken);

    Require(firstManagerSetupToken.ClientId == clientId, "first-manager setup token should target proof client.");
    Require(firstManagerSetupToken.InstallationId == installationId, "first-manager setup token should target proof installation.");
    Require(firstManagerSetupToken.PendingDeviceRequestId == firstManagerPendingDeviceRequestId, "first-manager setup token should bind the pending device request.");
    Require(firstManagerSetupToken.Purpose == LocalServerFirstManagerSetupTokenPurposes.FirstManagerBootstrap, "first-manager setup token should default to first-manager bootstrap purpose.");
    Require(firstManagerSetupToken.SignedToken.Signature.Algorithm == "HMAC-SHA256", "first-manager setup token should use HMAC signature.");
    Require(firstManagerSetupToken.SignedToken.Signature.PayloadSha256 == firstManagerSetupToken.PayloadSha256, "first-manager setup token response should expose the signed payload hash.");
    checks.Add("issued provider-gated first-manager setup token");

    var managerRecoveryPendingDeviceRequestId = Guid.NewGuid();
    var managerRecoverySetupToken = await SendJsonAsync<IssueLocalServerFirstManagerSetupTokenResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/first-manager-setup-token",
        new IssueLocalServerFirstManagerSetupTokenRequest(
            managerRecoveryPendingDeviceRequestId,
            "Recovered Manager",
            "recovered.manager@safarsuite.local",
            "postgres-proof",
            ExpiresInHours: 24,
            Purpose: LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery,
            RecoveryReason: "PostgreSQL proof manager recovery"),
        adminSession.AccessToken);

    Require(managerRecoverySetupToken.ClientId == clientId, "manager recovery setup token should target proof client.");
    Require(managerRecoverySetupToken.InstallationId == installationId, "manager recovery setup token should target proof installation.");
    Require(managerRecoverySetupToken.PendingDeviceRequestId == managerRecoveryPendingDeviceRequestId, "manager recovery setup token should bind the pending device request.");
    Require(managerRecoverySetupToken.Purpose == LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery, "manager recovery setup token should carry recovery purpose.");
    Require(managerRecoverySetupToken.RecoveryReason == "PostgreSQL proof manager recovery", "manager recovery setup token should carry recovery reason.");
    Require(managerRecoverySetupToken.AllowedActions?.Contains(LocalServerFirstManagerSetupTokenActions.RecoverManagerAccess) == true, "manager recovery setup token should allow manager access recovery.");
    Require(managerRecoverySetupToken.AllowedActions?.Contains(LocalServerFirstManagerSetupTokenActions.ApproveManagerDevice) == true, "manager recovery setup token should allow manager device approval.");
    Require(managerRecoverySetupToken.SignedToken.Payload.Purpose == LocalServerFirstManagerSetupTokenPurposes.ManagerRecovery, "signed manager recovery payload should carry recovery purpose.");
    Require(managerRecoverySetupToken.SignedToken.Signature.PayloadSha256 == managerRecoverySetupToken.PayloadSha256, "manager recovery setup token response should expose the signed payload hash.");
    checks.Add("issued provider-gated manager recovery setup token");

    var signedBundle = await SendJsonAsync<ClientPortalSignedEntitlementBundleResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/entitlement-bundle?clientId={clientId:D}");

    Require(signedBundle.Payload.ClientId == clientId, "entitlement bundle should target proof client.");
    Require(signedBundle.Payload.InstallationId == installationId, "entitlement bundle should target proof installation.");
    Require(signedBundle.Payload.Modules.Any(module =>
        module.ModuleCode.Equals("Accounting", StringComparison.OrdinalIgnoreCase)
        && module.IsEnabled), "entitlement bundle should include enabled Accounting module.");
    checks.Add("issued signed entitlement bundle through PostgreSQL-backed cloud");

    var heartbeatPairingStatus = new LocalServerPairingStatusResponse(
        PairingMode: LocalServerPairingModes.ManagerApproval,
        TotalDeviceCount: 2,
        PendingDeviceCount: 1,
        ApprovedDeviceCount: 1,
        SuspendedDeviceCount: 0,
        RevokedDeviceCount: 0,
        FirstManagerDeviceApproved: true,
        FirstManagerDeviceApprovedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
        LastDeviceUpdatedAtUtc: DateTimeOffset.UtcNow);
    var heartbeat = await SendJsonAsync<LocalServerHeartbeatResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/heartbeat",
        new ReportLocalServerHeartbeatRequest(
            clientId,
            localServerVersion,
            DateTimeOffset.UtcNow,
            LicenseStatus: "Active",
            EntitlementVersion: signedBundle.Payload.EntitlementVersion,
            PaidUntil: signedBundle.Payload.PaidUntil,
            WarningStartsAt: signedBundle.Payload.WarningStartsAt,
            GraceUntil: signedBundle.Payload.GraceUntil,
            OfflineValidUntil: signedBundle.Payload.OfflineValidUntil,
            Detail: "postgres proof heartbeat",
            DeploymentProfile: deploymentProfile,
            PairingStatus: heartbeatPairingStatus));

    Require(heartbeat.HeartbeatStatus.Equals("Received", StringComparison.OrdinalIgnoreCase), "heartbeat should be received.");
    Require(heartbeat.PairingStatus?.FirstManagerDeviceApproved == true, "heartbeat should echo pairing first-manager approval.");
    checks.Add("reported local-server heartbeat");

    var commandPayload = JsonSerializer.SerializeToElement(
        new
        {
            proofRunId = runId,
            requestedBy = "postgres-proof"
        },
        ProofJson.Options);
    var queuedCommand = await SendJsonAsync<InstallationCommandResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/commands",
        new QueueInstallationCommandRequest(
            "collect_diagnostics",
            commandPayload,
            NotBeforeUtc: null,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            IdempotencyKey: $"postgres-proof:{runId:N}:diagnostics"));

    Require(queuedCommand.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase), "queued command should be pending.");
    checks.Add("queued signed installation command");

    var pendingCommands = await SendJsonAsync<PendingInstallationCommandsResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/commands/pending");

    Require(pendingCommands.Commands.Any(command => command.CommandId == queuedCommand.CommandId), "pending command list should include queued command.");
    checks.Add("read pending installation command");

    var acknowledgementPayload = JsonSerializer.SerializeToElement(
        new
        {
            proofRunId = runId,
            applied = true
        },
        ProofJson.Options);
    var acknowledgedCommand = await SendJsonAsync<InstallationCommandResponse>(
        http,
        HttpMethod.Post,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/commands/{queuedCommand.CommandId:D}/acknowledgement",
        new AcknowledgeInstallationCommandRequest(
            "Applied",
            "postgres proof applied command",
            acknowledgementPayload));

    Require(acknowledgedCommand.AcknowledgementStatus == "Applied", "acknowledged command should record Applied status.");
    checks.Add("acknowledged installation command");

    var status = await SendJsonAsync<ControlCloudInstallationStatusResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/status");

    Require(status.InstallationStatus.Equals("Active", StringComparison.OrdinalIgnoreCase), "installation status should be active.");
    Require(status.LatestHeartbeat?.HeartbeatId == heartbeat.HeartbeatId, "installation status should include latest heartbeat.");
    Require(status.LatestHeartbeat?.PairingStatus?.ApprovedDeviceCount == 1, "installation status should include pairing device counts.");
    Require(status.LatestHeartbeat?.PairingStatus?.FirstManagerDeviceApproved == true, "installation status should include first-manager approval state.");
    Require(status.LatestEntitlement?.BundleIssueId == signedBundle.Payload.BundleIssueId, "installation status should include latest entitlement issue.");
    Require(status.CommandStatus.LatestAcknowledgementStatus == "Applied", "installation status should include command acknowledgement.");
    checks.Add("read cloud installation status summary");

    var portalInvitation = await SendJsonAsync<ClientPortalInvitationResponse>(
        http,
        HttpMethod.Post,
        "api/v1/client-portal/invitations",
        new CreateClientPortalInvitationRequest(
            clientId,
            portalEmail,
            "Postgres Proof Portal",
            "ClientViewer",
            ExpiresInDays: 7,
            CreatedBy: "postgres-proof-bootstrap"),
        adminSession.AccessToken);

    Require(portalInvitation.ClientId == clientId, "portal invitation should target proof client.");
    Require(!string.IsNullOrWhiteSpace(portalInvitation.InvitationToken), "portal invitation should include an invitation token.");
    checks.Add("created client portal invitation through provider gate");

    var acceptedPortal = await SendJsonAsync<AcceptClientPortalInvitationResponse>(
        http,
        HttpMethod.Post,
        "api/v1/client-portal/invitations/accept",
        new AcceptClientPortalInvitationRequest(
            portalInvitation.InvitationToken!,
            portalPassword,
            "Postgres Proof Portal"));

    Require(acceptedPortal.ClientId == clientId, "accepted portal session should target proof client.");
    Require(!string.IsNullOrWhiteSpace(acceptedPortal.AccessToken), "accepted portal session should include access token.");
    checks.Add("accepted client portal invitation");

    var portalStatus = await SendJsonAsync<ControlCloudInstallationStatusResponse>(
        http,
        HttpMethod.Get,
        $"api/v1/client-portal/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/status",
        body: null,
        acceptedPortal.AccessToken);

    Require(portalStatus.InstallationStatus.Equals("Active", StringComparison.OrdinalIgnoreCase), "portal installation status should be active.");
    Require(portalStatus.LatestHeartbeat?.HeartbeatId == heartbeat.HeartbeatId, "portal installation status should include latest heartbeat.");
    Require(portalStatus.LatestHeartbeat?.PairingStatus?.ApprovedDeviceCount == 1, "portal installation status should include approved pairing device count.");
    Require(portalStatus.LatestHeartbeat?.PairingStatus?.PendingDeviceCount == 1, "portal installation status should include pending pairing device count.");
    Require(portalStatus.LatestHeartbeat?.PairingStatus?.FirstManagerDeviceApproved == true, "portal installation status should include first-manager approval state.");
    checks.Add("read client portal installation pairing status");

    await VerifyPersistedRowsAsync(
        options.ConnectionString,
        envelope.MessageId,
        clientId,
        installationId,
        operatorEmail,
        bootstrapPackage.SetupTokenId,
        bootstrapPackage.BootstrapPackageId,
        signedBundle.Payload.BundleIssueId,
        heartbeat.HeartbeatId,
        queuedCommand.CommandId);
    checks.Add("verified PostgreSQL rows for every proof boundary");

    Console.WriteLine($"Control Cloud PostgreSQL proof passed {checks.Count} checks:");
    foreach (var check in checks)
    {
        Console.WriteLine($"- {check}");
    }

    Console.WriteLine(JsonSerializer.Serialize(
        new
        {
            proofStatus = "Passed",
            cloudUrl = options.ControlCloudBaseUrl,
            clientId,
            installationId,
            operatorEmail,
            entitlementVersion = signedBundle.Payload.EntitlementVersion,
            bundleIssueId = signedBundle.Payload.BundleIssueId,
            heartbeatId = heartbeat.HeartbeatId,
            commandId = queuedCommand.CommandId
        },
        ProofJson.Options));

    return 0;
}
finally
{
    if (cloudProcess is not null && !options.KeepCloudRunning)
    {
        await cloudProcess.DisposeAsync();
    }
}

static ControlCloudDbContext CreateDbContext(string connectionString)
{
    var dbOptions = new DbContextOptionsBuilder<ControlCloudDbContext>()
        .UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "cloud"))
        .Options;

    return new ControlCloudDbContext(dbOptions);
}

static async Task RequireHealthyAsync(HttpClient http)
{
    using var response = await http.GetAsync("health");
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"Control Cloud health check failed with HTTP {(int)response.StatusCode}: {body}");
    }
}

static async Task<T> SendJsonAsync<T>(
    HttpClient http,
    HttpMethod method,
    string path,
    object? body = null,
    string? bearerToken = null)
{
    using var request = new HttpRequestMessage(method, path);

    if (!string.IsNullOrWhiteSpace(bearerToken))
    {
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerToken}");
    }

    if (body is not null)
    {
        request.Content = JsonContent.Create(body, options: ProofJson.Options);
    }

    using var response = await http.SendAsync(request);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"HTTP {method} {path} failed with {(int)response.StatusCode}: {responseBody}");
    }

    return JsonSerializer.Deserialize<T>(responseBody, ProofJson.Options)
        ?? throw new InvalidOperationException($"HTTP {method} {path} returned an empty or invalid JSON response.");
}

static void RequireBearerSession(
    ProviderAccessSessionResponse session,
    string requiredScope)
{
    Require(session.TokenType.Equals("Bearer", StringComparison.OrdinalIgnoreCase), "provider session token type should be Bearer.");
    Require(!string.IsNullOrWhiteSpace(session.AccessToken), "provider session should include access token.");
    Require(session.Scopes.Contains(requiredScope), $"provider session should include {requiredScope}.");
}

static async Task VerifyPersistedRowsAsync(
    string connectionString,
    Guid messageId,
    Guid clientId,
    string installationId,
    string operatorEmail,
    Guid setupTokenId,
    Guid bootstrapPackageId,
    Guid bundleIssueId,
    Guid heartbeatId,
    Guid commandId)
{
    await using var dbContext = CreateDbContext(connectionString);
    var normalizedEmail = NormalizeEmail(operatorEmail);

    Require(
        await dbContext.ControlDeskEnvelopeReceipts.AnyAsync(receipt =>
            receipt.MessageId == messageId
            && receipt.Status == "Accepted"),
        "accepted envelope receipt should be persisted.");
    Require(
        await dbContext.ClientCommercialProjections.AnyAsync(projection =>
            projection.ClientId == clientId),
        "client commercial projection should be persisted.");
    Require(
        await dbContext.ProviderAccessOperators.AnyAsync(providerOperator =>
            providerOperator.NormalizedEmail == normalizedEmail),
        "provider access operator should be persisted.");
    Require(
        await dbContext.InstallationSetupTokens.AnyAsync(setupToken =>
            setupToken.SetupTokenId == setupTokenId
            && setupToken.BootstrapPackageId == bootstrapPackageId
            && setupToken.PackageBundleSha256 != null
            && setupToken.ConsumedAtUtc != null),
        "consumed installation setup token and package metadata should be persisted.");
    Require(
        await dbContext.ClientInstallations.AnyAsync(installation =>
            installation.ClientId == clientId
            && installation.InstallationId == installationId
            && installation.Status == "Active"),
        "client installation should be persisted.");
    Require(
        await dbContext.EntitlementBundleIssues.AnyAsync(bundle =>
            bundle.BundleIssueId == bundleIssueId
            && bundle.ClientId == clientId
            && bundle.InstallationId == installationId),
        "entitlement bundle issue should be persisted.");
    Require(
        await dbContext.InstallationHeartbeats.AnyAsync(heartbeat =>
            heartbeat.HeartbeatId == heartbeatId
            && heartbeat.InstallationId == installationId
            && heartbeat.PairingFirstManagerDeviceApproved == true
            && heartbeat.PairingApprovedDeviceCount == 1),
        "installation heartbeat with pairing status should be persisted.");
    Require(
        await dbContext.InstallationCommands.AnyAsync(command =>
            command.CommandId == commandId
            && command.AcknowledgementStatus == "Applied"),
        "installation command acknowledgement status should be persisted.");
    Require(
        await dbContext.InstallationCommandAcknowledgements.AnyAsync(acknowledgement =>
            acknowledgement.CommandId == commandId
            && acknowledgement.ResultStatus == "Applied"),
        "installation command acknowledgement row should be persisted.");
}

static ControlCloudEnvelope CreateEntitlementSnapshotEnvelope(
    ProofOptions options,
    Guid clientId,
    Guid runId)
{
    var now = DateTimeOffset.UtcNow;
    var today = DateOnly.FromDateTime(now.UtcDateTime);
    var paidUntil = today.AddDays(30);
    var messageId = Guid.NewGuid();
    var entitlementSnapshotId = Guid.NewGuid();
    var payload = new EntitlementSnapshotIssuedProofPayload(
        EventVersion: "1",
        EntitlementSnapshotId: entitlementSnapshotId,
        ClientId: clientId,
        ContractId: Guid.NewGuid(),
        SourceInvoiceId: Guid.NewGuid(),
        SourceInvoiceNumber: $"POSTGRES-PROOF-{now:yyyyMMddHHmmss}",
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
    var payloadJson = JsonSerializer.Serialize(payload, ProofJson.Options);
    var occurredAtUtc = now;
    var preparedAtUtc = now;
    var idempotencyKey = $"{options.ControlDeskSourceSystem}:postgres-proof:{runId:N}";

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
            "",
            ""));

    return CanonicalizeControlDeskEnvelopeForHttp(options, envelope);
}

static ControlCloudEnvelope CanonicalizeControlDeskEnvelopeForHttp(
    ProofOptions options,
    ControlCloudEnvelope envelope)
{
    var envelopeJson = JsonSerializer.Serialize(envelope, ProofJson.Options);
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

static string SignControlDeskEnvelope(
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

static string Sign(string signingSecret, string value)
{
    var secretBytes = Encoding.UTF8.GetBytes(signingSecret);
    var valueBytes = Encoding.UTF8.GetBytes(value);
    var signatureBytes = HMACSHA256.HashData(secretBytes, valueBytes);

    return Convert.ToBase64String(signatureBytes);
}

static string ComputeSha256(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var hash = SHA256.HashData(bytes);

    return Convert.ToHexString(hash).ToLowerInvariant();
}

static string NormalizeEmail(string? email)
{
    return string.IsNullOrWhiteSpace(email)
        ? ""
        : email.Trim().ToLowerInvariant();
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void RequireBootstrapSecretReadiness(
    LocalServerBootstrapPackageResponse bootstrapPackage,
    string label)
{
    var readiness = bootstrapPackage.SecretReadiness
        ?? throw new InvalidOperationException($"{label} should include secret readiness.");
    var readinessJson = JsonSerializer.Serialize(readiness, ProofJson.Options);

    Require(readiness.HasActiveSecret, $"{label} should report an active signing secret is configured.");
    Require(readiness.ActiveKeyId == bootstrapPackage.SignedBundle.Signature.KeyId, $"{label} readiness key should match the bundle signature key.");
    Require(
        readiness.RequiredEnvironmentVariables.Contains("SAFARSUITE_ENTITLEMENT_SIGNING_SECRET"),
        $"{label} should tell operators to control the install-time entitlement signing secret.");
    Require(
        bootstrapPackage.InstallCommand.Contains("SAFARSUITE_ENTITLEMENT_SIGNING_KEY_ID=", StringComparison.Ordinal),
        $"{label} install command should include the non-secret active signing key id.");
    Require(
        !readinessJson.Contains("local-entitlement-signing-secret-change-before-cloud", StringComparison.Ordinal),
        $"{label} readiness should not leak the active signing secret value.");
}

internal sealed class ControlCloudProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output;

    private ControlCloudProcess(
        Process process,
        ConcurrentQueue<string> output)
    {
        _process = process;
        _output = output;
    }

    public static async Task<ControlCloudProcess> StartAsync(ProofOptions options)
    {
        var apiProject = Path.Combine(
            options.RepositoryRoot,
            "src",
            "SafarSuite.ControlCloud.Api",
            "SafarSuite.ControlCloud.Api.csproj");

        if (!File.Exists(apiProject))
        {
            throw new InvalidOperationException($"Control Cloud API project was not found at '{apiProject}'.");
        }

        var apiOutputDirectory = EnsureTrailingSeparator(Path.Combine(
            options.OutputDirectory,
            "controlcloud-api-build"));
        Directory.CreateDirectory(apiOutputDirectory);

        var processStart = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = options.RepositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        processStart.ArgumentList.Add("run");
        processStart.ArgumentList.Add("--no-restore");
        processStart.ArgumentList.Add("--no-launch-profile");
        processStart.ArgumentList.Add("--project");
        processStart.ArgumentList.Add(apiProject);
        processStart.ArgumentList.Add($"-p:OutDir={apiOutputDirectory}");
        processStart.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        processStart.Environment["ASPNETCORE_URLS"] = options.ControlCloudBaseUrl;
        processStart.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        processStart.Environment["DOTNET_NOLOGO"] = "1";
        processStart.Environment["Persistence__Provider"] = "Postgres";
        processStart.Environment["ConnectionStrings__ControlCloud"] = options.ConnectionString;
        processStart.Environment["ConnectionStrings__ControlDesk"] = options.ConnectionString;
        processStart.Environment["ControlCloud__BootstrapPackages__CloudBaseUrl"] = options.ControlCloudBaseUrl;

        var output = new ConcurrentQueue<string>();
        var process = Process.Start(processStart)
            ?? throw new InvalidOperationException("Could not start Control Cloud API process.");

        process.OutputDataReceived += (_, eventArgs) => TrackOutput(output, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => TrackOutput(output, eventArgs.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var cloud = new ControlCloudProcess(process, output);

        try
        {
            await WaitForCloudHealthAsync(
                options.ControlCloudBaseUrl,
                process,
                output,
                TimeSpan.FromSeconds(options.CloudStartTimeoutSeconds));
        }
        catch
        {
            await cloud.DisposeAsync();
            throw;
        }

        return cloud;
    }

    public async ValueTask DisposeAsync()
    {
        if (_process.HasExited)
        {
            _process.Dispose();
            return;
        }

        _process.Kill(entireProcessTree: true);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await _process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _process.Dispose();
        }
    }

    private static async Task WaitForCloudHealthAsync(
        string baseUrl,
        Process process,
        ConcurrentQueue<string> output,
        TimeSpan timeout)
    {
        using var http = new HttpClient
        {
            BaseAddress = new Uri($"{baseUrl}/")
        };
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Control Cloud API exited before becoming healthy with code {process.ExitCode}.{Environment.NewLine}{FormatRecentOutput(output)}");
            }

            try
            {
                using var response = await http.GetAsync("health");

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException(
            $"Control Cloud API did not become healthy within {timeout.TotalSeconds:N0} seconds.{Environment.NewLine}{FormatRecentOutput(output)}");
    }

    private static void TrackOutput(
        ConcurrentQueue<string> output,
        string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        output.Enqueue(line);

        while (output.Count > 120 && output.TryDequeue(out _))
        {
        }
    }

    private static string FormatRecentOutput(ConcurrentQueue<string> output)
    {
        var lines = output.ToArray();

        return lines.Length == 0
            ? "No Control Cloud output was captured."
            : string.Join(Environment.NewLine, lines.TakeLast(40));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return Path.EndsInDirectorySeparator(path)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}

internal sealed record ProofOptions(
    string RepositoryRoot,
    string OutputDirectory,
    string ConnectionString,
    string ControlCloudBaseUrl,
    bool UseExistingCloud,
    bool SkipMigrations,
    bool KeepCloudRunning,
    int CloudStartTimeoutSeconds,
    string ProviderAccessSecret,
    string ControlDeskSigningKeyId,
    string ControlDeskSigningSecret,
    string ControlDeskSourceSystem,
    string ControlDeskSourceEnvironment,
    bool ShowHelp)
{
    private const string DefaultConnectionString =
        "Host=127.0.0.1;Port=54329;Database=safarsuite_control_desk;Username=safarsuite;Password=safarsuite_dev_password";

    public const string HelpText = """
        Usage:
          dotnet run --project tools/SafarSuite.ControlCloud.PostgresProof -- [options]

        Options:
          --connection-string <value>       PostgreSQL connection string. Default: local dev compose database.
          --cloud-url <url>                 Control Cloud URL. Default: http://127.0.0.1:5159.
          --use-existing-cloud              Do not start a Control Cloud API process.
          --skip-migrations                 Do not apply Control Cloud EF migrations before proof.
          --keep-cloud-running              Leave the started Control Cloud API process running.
          --cloud-start-timeout-seconds <n> Wait time for started API health. Default: 90.
          --output <path>                   Artifact/build directory. Default: artifacts/codex/controlcloud-postgres-proof.
          --repo-root <path>                Repository root. Default: current directory.
          --help                            Show help.
        """;

    public static ProofOptions Parse(string[] args)
    {
        var repositoryRoot = Path.GetFullPath(Environment.CurrentDirectory);
        var outputDirectory = Path.Combine(repositoryRoot, "artifacts", "codex", "controlcloud-postgres-proof");
        var connectionString = DefaultConnectionString;
        var controlCloudBaseUrl = "http://127.0.0.1:5159";
        var useExistingCloud = false;
        var skipMigrations = false;
        var keepCloudRunning = false;
        var cloudStartTimeoutSeconds = 90;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];

            switch (arg)
            {
                case "--connection-string":
                    connectionString = ReadRequired(args, ref index, arg);
                    break;

                case "--cloud-url":
                    controlCloudBaseUrl = ReadRequired(args, ref index, arg).TrimEnd('/');
                    break;

                case "--use-existing-cloud":
                    useExistingCloud = true;
                    break;

                case "--skip-migrations":
                    skipMigrations = true;
                    break;

                case "--keep-cloud-running":
                    keepCloudRunning = true;
                    break;

                case "--cloud-start-timeout-seconds":
                    cloudStartTimeoutSeconds = int.Parse(ReadRequired(args, ref index, arg));
                    break;

                case "--output":
                    outputDirectory = Path.GetFullPath(ReadRequired(args, ref index, arg));
                    break;

                case "--repo-root":
                    repositoryRoot = Path.GetFullPath(ReadRequired(args, ref index, arg));
                    break;

                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown option '{arg}'.{Environment.NewLine}{HelpText}");
            }
        }

        return new ProofOptions(
            repositoryRoot,
            outputDirectory,
            connectionString,
            controlCloudBaseUrl,
            useExistingCloud,
            skipMigrations,
            keepCloudRunning,
            cloudStartTimeoutSeconds,
            ProviderAccessSecret: "local-development-provider-access-secret-change-before-cloud",
            ControlDeskSigningKeyId: "local-dev",
            ControlDeskSigningSecret: "local-development-signing-secret-change-before-cloud",
            ControlDeskSourceSystem: "SafarSuite.ControlDesk",
            ControlDeskSourceEnvironment: "PostgresProof",
            showHelp);
    }

    private static string ReadRequired(
        string[] args,
        ref int index,
        string option)
    {
        if (++index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new InvalidOperationException($"{option} requires a value.");
        }

        return args[index].Trim();
    }
}

internal static class ProviderAccessScopes
{
    public const string AppActivationRead = "app-activation:read";
    public const string AppActivationWrite = "app-activation:write";
    public const string ClientPortalManage = "client-portal:manage";
    public const string DeploymentPackagesRead = "deployment-packages:read";
    public const string DeploymentPackagesWrite = "deployment-packages:write";
    public const string ProviderOperatorsManage = "provider-operators:manage";
}

internal static class ProofJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}

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

internal sealed record ProviderAccessSessionResponse(
    string AccessToken,
    string TokenType,
    string Actor,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);

internal sealed record CreateProviderOperatorRequest(
    string Email,
    string FullName,
    string Password,
    string[] Scopes,
    string? CreatedBy = null);

internal sealed class ProviderAccessOperatorResponse
{
    public string UserId { get; set; } = "";
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
