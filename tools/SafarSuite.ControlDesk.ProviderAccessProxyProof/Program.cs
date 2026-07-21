using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
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
ApiProcess? cloudProcess = null;
ApiProcess? controlDeskProcess = null;

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
        cloudProcess = await ApiProcess.StartAsync(
            "Control Cloud API",
            options.RepositoryRoot,
            Path.Combine("src", "SafarSuite.ControlCloud.Api", "SafarSuite.ControlCloud.Api.csproj"),
            options.ControlCloudBaseUrl,
            Path.Combine(options.OutputDirectory, "controlcloud-api-build"),
            environment =>
            {
                environment["Persistence__Provider"] = "Postgres";
                environment["ConnectionStrings__ControlCloud"] = options.ConnectionString;
                environment["ConnectionStrings__ControlDesk"] = options.ConnectionString;
                environment["ControlCloud__BootstrapPackages__CloudBaseUrl"] = options.ControlCloudBaseUrl;
                environment["ClientPortal__ProviderAccess__SharedSecret"] = options.ProviderAccessSecret;
                environment["ClientPortal__ProviderAccess__SessionSigningSecret"] = options.ProviderSessionSigningSecret;
                environment["ClientPortal__ProviderAccess__TotpProtectionSecret"] = options.ProviderTotpProtectionSecret;
            },
            TimeSpan.FromSeconds(options.ApiStartTimeoutSeconds));
        checks.Add("started Control Cloud API with PostgreSQL provider access");
    }

    if (!options.UseExistingControlDesk)
    {
        controlDeskProcess = await ApiProcess.StartAsync(
            "Control Desk API",
            options.RepositoryRoot,
            Path.Combine("src", "SafarSuite.ControlDesk.Api", "SafarSuite.ControlDesk.Api.csproj"),
            options.ControlDeskBaseUrl,
            Path.Combine(options.OutputDirectory, "controldesk-api-build"),
            environment =>
            {
                environment["Persistence__Provider"] = "InMemory";
                environment["ControlCloud__Status__BaseUrl"] = options.ControlCloudBaseUrl;
                environment["ControlCloud__Status__ProviderAccessSecret"] = options.ProviderAccessSecret;
                environment["ControlCloud__Status__ProviderAccessToken"] = "";
                environment["ControlCloud__PortalInvitations__BaseUrl"] = options.ControlCloudBaseUrl;
                environment["ControlCloud__PortalInvitations__ProviderAccessSecret"] = options.ProviderAccessSecret;
                environment["ControlCloud__PortalInvitations__ProviderAccessToken"] = "";
            },
            TimeSpan.FromSeconds(options.ApiStartTimeoutSeconds));
        checks.Add("started Control Desk API pointed at Control Cloud");
    }

    using var controlDeskHttp = new HttpClient
    {
        BaseAddress = new Uri($"{options.ControlDeskBaseUrl}/")
    };
    using var controlCloudHttp = new HttpClient
    {
        BaseAddress = new Uri($"{options.ControlCloudBaseUrl}/")
    };

    await RequireHealthyAsync(controlCloudHttp, "Control Cloud");
    await RequireHealthyAsync(controlDeskHttp, "Control Desk");
    checks.Add("verified both APIs are healthy");

    var runId = Guid.NewGuid();
    var operatorEmail = $"proxy.proof.{runId:N}@safarsuite.local";
    var initialPassword = $"ProxyProofPassword-{runId:N}!";
    var resetPassword = $"ProxyProofReset-{runId:N}!";
    var changedPassword = $"ProxyProofChanged-{runId:N}!";
    var clientId = Guid.NewGuid();
    var installationId = $"proxy-proof-{runId:N}";
    var localServerVersion = "proxy-proof-localserver-0.1";
    var pendingDeviceRequestId = Guid.NewGuid();

    var envelope = CreateEntitlementSnapshotEnvelope(
        options,
        clientId,
        runId);
    var seed = await SendJsonAsync<ControlCloudReceiveEnvelopeResponse>(
        controlCloudHttp,
        HttpMethod.Post,
        "api/v1/control-desk/messages",
        envelope);

    Require(
        seed.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase)
        || seed.Status.Equals("Duplicate", StringComparison.OrdinalIgnoreCase),
        $"client entitlement seed should be accepted or duplicate, got '{seed.Status}'.");
    checks.Add("seeded client projection into Control Cloud");

    var bootstrapPackage = await SendJsonAsync<LocalServerBootstrapPackageResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-package",
        new CreateLocalServerBootstrapPackageRequest(
            ExpiresInHours: 4,
            CreatedBy: "control-desk-proxy-proof",
            DeploymentMode: ControlCloudBootstrapModes.OnlineBootstrap,
            LocalServerVersion: localServerVersion,
            SafarSuiteAppVersion: "proxy-proof-app-0.1",
            ClientDeploymentMode: SafarSuiteClientDeploymentModes.CloudSyncMultiBranch,
            SiteId: "proxy-proof-hq",
            SiteRole: SafarSuiteDeploymentSiteRoles.Hq,
            ParentSiteId: null,
            BranchCode: "HQ",
            SyncTopologyId: $"proxy-proof-{runId:N}"));

    Require(bootstrapPackage.ClientId == clientId, "Control Desk bootstrap package should target proof client.");
    Require(bootstrapPackage.InstallationId == installationId, "Control Desk bootstrap package should target proof installation.");
    RequireBootstrapSecretReadiness(bootstrapPackage, "Control Desk bootstrap package");
    checks.Add("created bootstrap package through Control Desk proxy with non-secret signing readiness");

    var handoff = await SendJsonAsync<LocalServerBootstrapPackageHandoffResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/bootstrap-packages/{bootstrapPackage.BootstrapPackageId:D}/handoff",
        new MarkLocalServerBootstrapPackageHandoffRequest(
            Channel: "Secure email",
            Recipient: "proxy.proof.customer@safarsuite.local",
            MarkedBy: "control-desk-proxy-proof",
            PreflightAcknowledgements: LocalServerBootstrapPackageHandoffPreflight.RequiredKeys,
            Note: "Control Desk proxy proof handoff marker"));

    Require(handoff.BootstrapPackageId == bootstrapPackage.BootstrapPackageId, "Control Desk handoff should target generated bootstrap package.");
    Require(handoff.SetupTokenId == bootstrapPackage.SetupTokenId, "Control Desk handoff should target generated setup token.");
    Require(handoff.ClientId == clientId, "Control Desk handoff should target proof client.");
    Require(handoff.InstallationId == installationId, "Control Desk handoff should target proof installation.");
    Require(handoff.HandoffStatus == "HandedOff", "Control Desk handoff should be marked handed off.");
    Require(
        LocalServerBootstrapPackageHandoffPreflight.RequiredKeys.All(key =>
            handoff.PreflightAcknowledgements.Contains(key)),
        "Control Desk handoff should record all required setup preflight acknowledgements.");

    var handoffAudit = await SendJsonAsync<ControlCloudAuditEventsResponse>(
        controlDeskHttp,
        HttpMethod.Get,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/audit-events?take=20");

    Require(
        handoffAudit.Events.Any(auditEvent =>
            auditEvent.EventType == "BootstrapPackageHandedOff"
            && auditEvent.ClientId == clientId
            && auditEvent.Detail.Contains(bootstrapPackage.BootstrapPackageId.ToString("D"), StringComparison.Ordinal)
            && auditEvent.Detail.Contains(installationId, StringComparison.Ordinal)
            && auditEvent.Detail.Contains("Preflight acknowledged", StringComparison.Ordinal)
            && !auditEvent.Detail.Contains(bootstrapPackage.SetupToken, StringComparison.Ordinal)),
        "Control Desk handoff audit view should identify the package, installation, and preflight acknowledgements without leaking the setup token secret.");
    checks.Add("marked bootstrap package handoff through Control Desk proxy and verified non-secret audit trail");

    var registration = await SendJsonAsync<LocalServerInstallationRegistrationResponse>(
        controlCloudHttp,
        HttpMethod.Post,
        $"api/v1/local-server/installations/{Uri.EscapeDataString(installationId)}/registration",
        new RegisterLocalServerInstallationRequest(
            clientId,
            bootstrapPackage.SetupToken,
            localServerVersion,
            bootstrapPackage.DeploymentProfile));

    Require(registration.InstallationStatus.Equals("Active", StringComparison.OrdinalIgnoreCase), "proxied bootstrap package should register the installation.");
    checks.Add("registered Control Desk-created installation package");

    var firstManagerSetupToken = await SendJsonAsync<IssueLocalServerFirstManagerSetupTokenResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/clients/{clientId:D}/installations/{Uri.EscapeDataString(installationId)}/first-manager-setup-token",
        new IssueLocalServerFirstManagerSetupTokenRequest(
            pendingDeviceRequestId,
            "Control Desk Proxy Proof Manager",
            "proxy.manager@safarsuite.local",
            "control-desk-proxy-proof",
            ExpiresInHours: 4));

    Require(firstManagerSetupToken.ClientId == clientId, "first-manager setup token should target proof client.");
    Require(firstManagerSetupToken.InstallationId == installationId, "first-manager setup token should target proof installation.");
    Require(firstManagerSetupToken.PendingDeviceRequestId == pendingDeviceRequestId, "first-manager setup token should bind the pending device request.");
    Require(firstManagerSetupToken.SignedToken.Payload.TokenId == firstManagerSetupToken.TokenId, "signed token payload should expose the issued token id.");
    Require(firstManagerSetupToken.SignedToken.Payload.PendingDeviceRequestId == pendingDeviceRequestId, "signed token payload should preserve the pending device request.");
    Require(firstManagerSetupToken.SignedToken.Signature.PayloadSha256 == firstManagerSetupToken.PayloadSha256, "first-manager setup token response should expose the signed payload hash.");
    Require(firstManagerSetupToken.SignedToken.Signature.KeyId == firstManagerSetupToken.SigningKeyId, "first-manager setup token response should expose the signing key.");
    Require(!string.IsNullOrWhiteSpace(firstManagerSetupToken.SignedToken.Signature.Value), "first-manager setup token should include a signature.");
    checks.Add("issued first-manager setup token through Control Desk proxy");

    _ = await SendJsonAsync<ProviderAccessOperatorsResponse>(
        controlDeskHttp,
        HttpMethod.Get,
        "api/v1/control-cloud/provider-access/operators");
    checks.Add("listed provider operators through Control Desk proxy");

    var createdOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        "api/v1/control-cloud/provider-access/operators",
        new CreateProviderOperatorRequest(
            operatorEmail,
            "Control Desk Proxy Proof Operator",
            initialPassword,
            [ProviderAccessScopes.AppActivationRead, ProviderAccessScopes.ProviderOperatorsManage],
            "control-desk-proxy-proof"));

    Require(!string.IsNullOrWhiteSpace(createdOperator.UserId), "created provider operator should return a user id.");
    Require(createdOperator.Email.Equals(operatorEmail, StringComparison.OrdinalIgnoreCase), "created provider operator should preserve email.");
    Require(createdOperator.Status.Equals(ProviderAccessStatuses.Active, StringComparison.OrdinalIgnoreCase), "created provider operator should be active.");
    checks.Add("created provider operator through Control Desk proxy");

    var operatorsAfterCreate = await SendJsonAsync<ProviderAccessOperatorsResponse>(
        controlDeskHttp,
        HttpMethod.Get,
        "api/v1/control-cloud/provider-access/operators");
    Require(
        operatorsAfterCreate.Operators.Any(providerOperator =>
            providerOperator.UserId == createdOperator.UserId
            && providerOperator.Email.Equals(operatorEmail, StringComparison.OrdinalIgnoreCase)),
        "provider operator list should include the proxy-created operator.");
    checks.Add("read proxy-created operator through Control Desk proxy list");

    var scopedOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/scopes",
        new UpdateProviderOperatorScopesRequest(
            [
                ProviderAccessScopes.AppActivationRead,
                ProviderAccessScopes.AppActivationWrite,
                ProviderAccessScopes.ProviderOperatorsManage
            ],
            "control-desk-proxy-proof"));
    Require(
        scopedOperator.Scopes.Contains(ProviderAccessScopes.AppActivationWrite),
        "scope update should grant app-activation:write.");
    checks.Add("updated provider operator scopes through Control Desk proxy");

    var suspendedOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/status",
        new UpdateProviderOperatorStatusRequest(
            ProviderAccessStatuses.Suspended,
            "control-desk-proxy-proof"));
    Require(
        suspendedOperator.Status.Equals(ProviderAccessStatuses.Suspended, StringComparison.OrdinalIgnoreCase),
        "status update should suspend the operator.");
    checks.Add("suspended provider operator through Control Desk proxy");

    var passwordResetOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/password",
        new ResetProviderOperatorPasswordRequest(
            resetPassword,
            "control-desk-proxy-proof"));
    Require(
        passwordResetOperator.UpdatedBy == "control-desk-proxy-proof",
        "password reset should stamp the update actor.");
    checks.Add("reset provider operator password through Control Desk proxy");

    var reactivatedOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/status",
        new UpdateProviderOperatorStatusRequest(
            ProviderAccessStatuses.Active,
            "control-desk-proxy-proof"));
    Require(
        reactivatedOperator.Status.Equals(ProviderAccessStatuses.Active, StringComparison.OrdinalIgnoreCase),
        "status update should reactivate the operator.");
    checks.Add("reactivated provider operator through Control Desk proxy");

    var limitedOperatorSession = await SendJsonAsync<ProviderAccessSessionResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        "api/v1/control-cloud/provider-access/operator-sessions",
        new CreateProviderOperatorSessionRequest(
            operatorEmail,
            resetPassword,
            [ProviderAccessScopes.AppActivationRead, ProviderAccessScopes.AppActivationWrite],
            15));

    RequireBearerSession(limitedOperatorSession, ProviderAccessScopes.AppActivationRead);
    Require(limitedOperatorSession.Scopes.Contains(ProviderAccessScopes.AppActivationWrite), "operator session should include updated write scope.");
    checks.Add("minted provider bearer session through Control Desk proxy");

    using var limitedSessionControlDeskHttp = CreateControlDeskHttpClient(
        options.ControlDeskBaseUrl,
        limitedOperatorSession.AccessToken);
    await RequireFailureStatusAsync(
        limitedSessionControlDeskHttp,
        HttpMethod.Get,
        "api/v1/control-cloud/provider-access/operators",
        HttpStatusCode.ServiceUnavailable);
    checks.Add("proved under-scoped Control Desk session override is enforced");

    var managerOperatorSession = await SendJsonAsync<ProviderAccessSessionResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        "api/v1/control-cloud/provider-access/operator-sessions",
        new CreateProviderOperatorSessionRequest(
            operatorEmail,
            resetPassword,
            [ProviderAccessScopes.ProviderOperatorsManage],
            15));

    RequireBearerSession(managerOperatorSession, ProviderAccessScopes.ProviderOperatorsManage);
    using var managerSessionControlDeskHttp = CreateControlDeskHttpClient(
        options.ControlDeskBaseUrl,
        managerOperatorSession.AccessToken);
    var operatorsViaSession = await SendJsonAsync<ProviderAccessOperatorsResponse>(
        managerSessionControlDeskHttp,
        HttpMethod.Get,
        "api/v1/control-cloud/provider-access/operators");
    Require(
        operatorsViaSession.Operators.Any(providerOperator =>
            providerOperator.UserId == createdOperator.UserId),
        "provider operator list should work with a manager-scoped Control Desk session override.");
    checks.Add("listed provider operators with Control Desk session override");

    var recoveryCodes = await SendJsonAsync<ProviderOperatorRecoveryCodesResponse>(
        managerSessionControlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/recovery-codes",
        new ResetProviderOperatorRecoveryCodesRequest(
            3,
            "control-desk-proxy-proof"));
    Require(
        recoveryCodes.Operator.MfaEnabled
        && recoveryCodes.Operator.RecoveryCodeCount == 3
        && recoveryCodes.RecoveryCodes.Count == 3,
        "recovery-code reset should enable MFA and return one-time recovery codes.");
    checks.Add("reset provider recovery codes through Control Desk session override");

    var totpEnrollment = await SendJsonAsync<ProviderOperatorTotpEnrollmentResponse>(
        managerSessionControlDeskHttp,
        HttpMethod.Post,
        $"api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(createdOperator.UserId)}/totp",
        new ResetProviderOperatorTotpRequest("control-desk-proxy-proof"));
    Require(
        totpEnrollment.Operator.MfaEnabled
        && totpEnrollment.Operator.TotpEnabled
        && !string.IsNullOrWhiteSpace(totpEnrollment.Secret)
        && totpEnrollment.OtpAuthUri.StartsWith("otpauth://totp/", StringComparison.Ordinal),
        "TOTP reset should enable provider MFA and return one-time enrollment material.");
    checks.Add("reset provider TOTP through Control Desk session override");

    var selfChangedOperator = await SendJsonAsync<ProviderAccessOperatorResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        "api/v1/control-cloud/provider-access/operator-password",
        new ChangeProviderOperatorPasswordRequest(
            operatorEmail,
            resetPassword,
            changedPassword));
    Require(
        selfChangedOperator.UpdatedBy == "Control Desk Proxy Proof Operator",
        "self-service password change should stamp the provider operator actor.");
    checks.Add("changed provider operator password through Control Desk proxy");

    var changedPasswordSession = await SendJsonAsync<ProviderAccessSessionResponse>(
        controlDeskHttp,
        HttpMethod.Post,
        "api/v1/control-cloud/provider-access/operator-sessions",
        new CreateProviderOperatorSessionRequest(
            operatorEmail,
            changedPassword,
            [ProviderAccessScopes.ProviderOperatorsManage],
            15,
            RecoveryCode: null,
            TotpCode: ProofTotp.CreateCode(totpEnrollment.Secret, DateTimeOffset.UtcNow)));
    RequireBearerSession(changedPasswordSession, ProviderAccessScopes.ProviderOperatorsManage);
    checks.Add("minted provider bearer session with self-changed password and TOTP");

    await VerifyPersistedOperatorAsync(
        options.ConnectionString,
        createdOperator.UserId,
        operatorEmail,
        "Control Desk Proxy Proof Operator",
        totpEnrollment.Secret);
    checks.Add("verified final provider operator state and protected TOTP custody in PostgreSQL");

    Console.WriteLine($"Control Desk provider-access proxy proof passed {checks.Count} checks:");
    foreach (var check in checks)
    {
        Console.WriteLine($"- {check}");
    }

    Console.WriteLine(JsonSerializer.Serialize(
        new
        {
            proofStatus = "Passed",
            controlCloudUrl = options.ControlCloudBaseUrl,
            controlDeskUrl = options.ControlDeskBaseUrl,
            clientId,
            installationId,
            firstManagerSetupTokenId = firstManagerSetupToken.TokenId,
            operatorEmail,
            userId = createdOperator.UserId,
            limitedScopes = limitedOperatorSession.Scopes,
            managerScopes = managerOperatorSession.Scopes
        },
        ProofJson.Options));

    return 0;
}
finally
{
    if (!options.KeepApisRunning)
    {
        if (controlDeskProcess is not null)
        {
            await controlDeskProcess.DisposeAsync();
        }

        if (cloudProcess is not null)
        {
            await cloudProcess.DisposeAsync();
        }
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
        EventVersion: "2",
        EntitlementSnapshotId: entitlementSnapshotId,
        ClientAccessRevisionId: Guid.NewGuid(),
        ClientId: clientId,
        ContractId: Guid.NewGuid(),
        EntitlementVersion: 100,
        SourceInvoiceId: Guid.NewGuid(),
        SourceInvoiceNumber: $"PROXY-PROOF-{now:yyyyMMddHHmmss}",
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
    var idempotencyKey = $"{options.ControlDeskSourceSystem}:provider-access-proxy-proof:{runId:N}";

    using var document = JsonDocument.Parse(payloadJson);
    var envelope = new ControlCloudEnvelope(
        EnvelopeVersion: "1",
        MessageId: messageId,
        MessageType: "EntitlementSnapshotIssued",
        SubjectType: "EntitlementSnapshot",
        SubjectId: entitlementSnapshotId.ToString("D"),
        SourceSystem: options.ControlDeskSourceSystem,
        SourceEnvironment: options.ControlDeskSourceEnvironment,
        OccurredAtUtc: now,
        PreparedAtUtc: now,
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

static string Sign(
    string signingSecret,
    string value)
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

static async Task RequireHealthyAsync(
    HttpClient http,
    string name)
{
    using var response = await http.GetAsync("health");
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException(
            $"{name} health check failed with HTTP {(int)response.StatusCode}: {body}");
    }
}

static async Task<T> SendJsonAsync<T>(
    HttpClient http,
    HttpMethod method,
    string path,
    object? body = null)
{
    using var request = new HttpRequestMessage(method, path);

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

static HttpClient CreateControlDeskHttpClient(
    string controlDeskBaseUrl,
    string providerAccessToken)
{
    var http = new HttpClient
    {
        BaseAddress = new Uri($"{controlDeskBaseUrl}/")
    };
    http.DefaultRequestHeaders.TryAddWithoutValidation(
        "X-SafarSuite-Provider-Access-Token",
        providerAccessToken);

    return http;
}

static async Task RequireFailureStatusAsync(
    HttpClient http,
    HttpMethod method,
    string path,
    HttpStatusCode expectedStatusCode)
{
    using var request = new HttpRequestMessage(method, path);
    using var response = await http.SendAsync(request);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (response.StatusCode != expectedStatusCode)
    {
        throw new InvalidOperationException(
            $"HTTP {method} {path} should have failed with {(int)expectedStatusCode}, but returned {(int)response.StatusCode}: {responseBody}");
    }
}

static void RequireBearerSession(
    ProviderAccessSessionResponse session,
    string requiredScope)
{
    Require(session.TokenType.Equals("Bearer", StringComparison.OrdinalIgnoreCase), "provider session token type should be Bearer.");
    Require(!string.IsNullOrWhiteSpace(session.AccessToken), "provider session should include access token.");
    Require(session.Scopes.Contains(requiredScope), $"provider session should include {requiredScope} scope.");
    Require(session.ExpiresAtUtc > DateTimeOffset.UtcNow, "provider session should not be expired.");
}

static async Task VerifyPersistedOperatorAsync(
    string connectionString,
    string userId,
    string email,
    string expectedUpdatedBy,
    string plainTotpSecret)
{
    await using var dbContext = CreateDbContext(connectionString);
    var normalizedEmail = NormalizeEmail(email);
    var entity = await dbContext.ProviderAccessOperators
        .AsNoTracking()
        .SingleOrDefaultAsync(providerOperator =>
            providerOperator.UserId == userId
            && providerOperator.NormalizedEmail == normalizedEmail);

    Require(entity is not null, "PostgreSQL should contain the provider operator created through Control Desk.");
    Require(entity!.Status.Equals(ProviderAccessStatuses.Active, StringComparison.OrdinalIgnoreCase), "PostgreSQL operator status should be Active.");
    Require(entity.UpdatedBy == expectedUpdatedBy, "PostgreSQL operator should keep the latest password actor.");
    Require(entity.LastLoginAtUtc is not null, "PostgreSQL operator should record the proof login after password reset.");
    Require(
        !string.IsNullOrWhiteSpace(entity.TotpSecret),
        "PostgreSQL operator should persist TOTP secret material.");
    var protectedTotpSecret = entity.TotpSecret!;
    Require(
        protectedTotpSecret.StartsWith("pa-totp-v1.", StringComparison.Ordinal),
        "PostgreSQL operator should store protected TOTP secret material.");
    Require(
        !protectedTotpSecret.Contains(plainTotpSecret, StringComparison.Ordinal),
        "PostgreSQL operator should not store the raw TOTP enrollment secret.");

    var scopes = JsonSerializer.Deserialize<string[]>(entity.ScopesJson, ProofJson.Options) ?? [];
    Require(scopes.Contains(ProviderAccessScopes.AppActivationRead), "PostgreSQL operator should keep app-activation:read scope.");
    Require(scopes.Contains(ProviderAccessScopes.AppActivationWrite), "PostgreSQL operator should keep app-activation:write scope.");
    Require(scopes.Contains(ProviderAccessScopes.ProviderOperatorsManage), "PostgreSQL operator should keep provider-operators:manage scope.");
}

static string NormalizeEmail(string email)
{
    return email.Trim().ToLowerInvariant();
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

internal sealed class ApiProcess : IAsyncDisposable
{
    private readonly string _name;
    private readonly Process _process;
    private readonly ConcurrentQueue<string> _output;

    private ApiProcess(
        string name,
        Process process,
        ConcurrentQueue<string> output)
    {
        _name = name;
        _process = process;
        _output = output;
    }

    public static async Task<ApiProcess> StartAsync(
        string name,
        string repositoryRoot,
        string projectRelativePath,
        string baseUrl,
        string outputDirectory,
        Action<IDictionary<string, string?>> configureEnvironment,
        TimeSpan timeout)
    {
        var apiProject = Path.Combine(repositoryRoot, projectRelativePath);
        var apiProjectDirectory = Path.GetDirectoryName(apiProject)
            ?? throw new InvalidOperationException($"{name} project directory could not be resolved.");
        var apiProjectName = Path.GetFileNameWithoutExtension(apiProject);
        var apiAssembly = Path.Combine(
            apiProjectDirectory,
            "bin",
            "Debug",
            "net10.0",
            $"{apiProjectName}.dll");

        if (!File.Exists(apiProject))
        {
            throw new InvalidOperationException($"{name} project was not found at '{apiProject}'.");
        }

        if (!File.Exists(apiAssembly))
        {
            throw new InvalidOperationException(
                $"{name} assembly was not found at '{apiAssembly}'. Run `dotnet build SafarSuite.ControlDesk.sln` before this proof.");
        }

        var processStart = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = apiProjectDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        processStart.ArgumentList.Add(apiAssembly);
        processStart.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        processStart.Environment["ASPNETCORE_URLS"] = baseUrl;
        processStart.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        processStart.Environment["DOTNET_NOLOGO"] = "1";
        configureEnvironment(processStart.Environment);

        var output = new ConcurrentQueue<string>();
        var process = Process.Start(processStart)
            ?? throw new InvalidOperationException($"Could not start {name} process.");

        process.OutputDataReceived += (_, eventArgs) => TrackOutput(output, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => TrackOutput(output, eventArgs.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var api = new ApiProcess(name, process, output);

        try
        {
            await WaitForHealthAsync(name, baseUrl, process, output, timeout);
        }
        catch
        {
            await api.DisposeAsync();
            throw;
        }

        return api;
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

    private static async Task WaitForHealthAsync(
        string name,
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
                    $"{name} exited before becoming healthy with code {process.ExitCode}.{Environment.NewLine}{FormatRecentOutput(output)}");
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
            $"{name} did not become healthy within {timeout.TotalSeconds:N0} seconds.{Environment.NewLine}{FormatRecentOutput(output)}");
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
            ? "No API output was captured."
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
    string ControlDeskBaseUrl,
    bool UseExistingCloud,
    bool UseExistingControlDesk,
    bool SkipMigrations,
    bool KeepApisRunning,
    int ApiStartTimeoutSeconds,
    string ProviderAccessSecret,
    string ProviderSessionSigningSecret,
    string ProviderTotpProtectionSecret,
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
          dotnet run --project tools/SafarSuite.ControlDesk.ProviderAccessProxyProof -- [options]

        Options:
          --connection-string <value>          PostgreSQL connection string. Default: local dev compose database.
          --cloud-url <url>                    Control Cloud URL. Default: http://127.0.0.1:5161.
          --control-desk-url <url>             Control Desk URL. Default: http://127.0.0.1:5190.
          --use-existing-cloud                 Do not start a Control Cloud API process.
          --use-existing-control-desk          Do not start a Control Desk API process.
          --skip-migrations                    Do not apply Control Cloud EF migrations before proof.
          --keep-apis-running                  Leave any started API processes running.
          --api-start-timeout-seconds <n>      Wait time for started APIs. Default: 90.
          --output <path>                      Artifact/build directory. Default: artifacts/codex/controldesk-provider-access-proxy-proof.
          --repo-root <path>                   Repository root. Default: current directory.
          --help                               Show help.
        """;

    public static ProofOptions Parse(string[] args)
    {
        var repositoryRoot = Path.GetFullPath(Environment.CurrentDirectory);
        var outputDirectory = Path.Combine(
            repositoryRoot,
            "artifacts",
            "codex",
            "controldesk-provider-access-proxy-proof");
        var connectionString = DefaultConnectionString;
        var controlCloudBaseUrl = "http://127.0.0.1:5161";
        var controlDeskBaseUrl = "http://127.0.0.1:5190";
        var useExistingCloud = false;
        var useExistingControlDesk = false;
        var skipMigrations = false;
        var keepApisRunning = false;
        var apiStartTimeoutSeconds = 90;
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

                case "--control-desk-url":
                    controlDeskBaseUrl = ReadRequired(args, ref index, arg).TrimEnd('/');
                    break;

                case "--use-existing-cloud":
                    useExistingCloud = true;
                    break;

                case "--use-existing-control-desk":
                    useExistingControlDesk = true;
                    break;

                case "--skip-migrations":
                    skipMigrations = true;
                    break;

                case "--keep-apis-running":
                    keepApisRunning = true;
                    break;

                case "--api-start-timeout-seconds":
                    apiStartTimeoutSeconds = int.Parse(ReadRequired(args, ref index, arg));
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
            controlDeskBaseUrl,
            useExistingCloud,
            useExistingControlDesk,
            skipMigrations,
            keepApisRunning,
            apiStartTimeoutSeconds,
            ProviderAccessSecret: "local-development-provider-access-secret-change-before-cloud",
            ProviderSessionSigningSecret: "local-development-provider-session-signing-secret-change-before-cloud",
            ProviderTotpProtectionSecret: "local-development-provider-totp-protection-secret-change-before-cloud",
            ControlDeskSigningKeyId: "local-dev",
            ControlDeskSigningSecret: "local-development-signing-secret-change-before-cloud",
            ControlDeskSourceSystem: "SafarSuite.ControlDesk",
            ControlDeskSourceEnvironment: "ProviderAccessProxyProof",
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
    public const string ProviderOperatorsManage = "provider-operators:manage";
}

internal static class ProviderAccessStatuses
{
    public const string Active = "Active";
    public const string Suspended = "Suspended";
}

internal static class ProofJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}

internal static class ProofTotp
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int Digits = 6;
    private const int PeriodSeconds = 30;

    public static string CreateCode(
        string secret,
        DateTimeOffset now)
    {
        var secretBytes = DecodeBase32(secret);
        var counter = BitConverter.GetBytes(now.ToUnixTimeSeconds() / PeriodSeconds);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var binaryCode =
            ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);

        return (binaryCode % (int)Math.Pow(10, Digits))
            .ToString($"D{Digits}", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string secret)
    {
        var normalizedSecret = new string(secret
            .Where(character => !char.IsWhiteSpace(character) && character is not '-' and not '=')
            .Select(character => char.ToUpperInvariant(character))
            .ToArray());
        var bytes = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in normalizedSecret)
        {
            var value = Base32Alphabet.IndexOf(character);

            if (value < 0)
            {
                throw new FormatException("TOTP secret is not base32 encoded.");
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft < 8)
            {
                continue;
            }

            bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
            bitsLeft -= 8;
        }

        return bytes.ToArray();
    }
}

internal sealed record CreateProviderOperatorSessionRequest(
    string Email,
    string Password,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null,
    string? RecoveryCode = null,
    string? TotpCode = null);

internal sealed record ChangeProviderOperatorPasswordRequest(
    string Email,
    string CurrentPassword,
    string NewPassword);

internal sealed record ProviderAccessSessionResponse(
    string AccessToken,
    string TokenType,
    string Actor,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc);

internal sealed record EntitlementSnapshotIssuedProofPayload(
    string EventVersion,
    Guid EntitlementSnapshotId,
    Guid ClientAccessRevisionId,
    Guid ClientId,
    Guid ContractId,
    long EntitlementVersion,
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
