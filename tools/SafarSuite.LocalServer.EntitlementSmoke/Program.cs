using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Common;
using SafarSuite.LocalServer.Application.Entitlements.EvaluateFeatureAccess;
using SafarSuite.LocalServer.Application.Entitlements.ImportSignedEntitlementBundle;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromControlCloud;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatToControlCloud;
using SafarSuite.LocalServer.Domain.Entitlements;
using SafarSuite.LocalServer.Infrastructure.Entitlements;
using SafarSuite.LocalServer.Infrastructure.Heartbeats;

var installationId = "office-main";
var clientId = Guid.NewGuid();
var cachePath = Path.Combine(
    Path.GetTempPath(),
    $"safarsuite-local-entitlement-smoke-{Guid.NewGuid():N}.json");
var trustOptions = new LocalServerEntitlementTrustOptions
{
    CacheStorePath = cachePath,
    SigningKeys =
    [
        new LocalServerEntitlementTrustKeyOptions
        {
            KeyId = "local-entitlement-dev",
            Secret = "local-entitlement-signing-secret-change-before-cloud"
        }
    ]
};
var clock = new FixedLocalServerClock(
    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
var cache = new FileLocalServerEntitlementCache(trustOptions);
var verifier = new HmacLocalServerEntitlementBundleVerifier(trustOptions);
var importHandler = new ImportSignedEntitlementBundleHandler(
    verifier,
    cache,
    clock);
var evaluateHandler = new EvaluateFeatureAccessHandler(
    cache,
    new LocalServerEntitlementPolicy(),
    clock);

var signedBundle = CreateSignedBundle(
    clientId,
    installationId,
    entitlementVersion: 100);
var controlCloudOptions = new ControlCloudEntitlementPullOptions
{
    BaseUrl = new Uri("https://control-cloud.local")
};
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
    new LocalServerEntitlementPolicy(),
    clock);
var heartbeatResult = await heartbeatHandler.HandleAsync(
    new ReportHeartbeatToControlCloudCommand(
        clientId,
        installationId,
        "local-server-smoke",
        Detail: "Local entitlement smoke heartbeat."));

Require(heartbeatResult.IsSuccess, "Heartbeat should report current entitlement state to Control Cloud.");
Require(heartbeatResult.Heartbeat!.HeartbeatStatus == "Received", "Heartbeat status should be Received.");
Require(heartbeatResult.Heartbeat.LicenseStatus == LocalServerEntitlementAccessStates.Active, "Heartbeat license status should be Active.");
Require(heartbeatHttpHandler.LastRequest?.EntitlementVersion == 100, "Heartbeat should report cached entitlement version 100.");

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

var activeDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 8, 15));
Require(activeDecision.IsAllowed, "Accounting should be allowed during active period.");
Require(activeDecision.AccessState == LocalServerEntitlementAccessStates.Active, "Accounting should be active before warning.");

var warningDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 8, 24));
Require(warningDecision.IsAllowed, "Accounting should be allowed during warning.");
Require(warningDecision.AccessState == LocalServerEntitlementAccessStates.Warning, "Accounting should enter warning state.");

var graceDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 5));
Require(graceDecision.IsAllowed, "Accounting should be allowed during grace.");
Require(graceDecision.AccessState == LocalServerEntitlementAccessStates.Grace, "Accounting should enter grace state.");

var restrictedDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 8));
Require(!restrictedDecision.IsAllowed, "Accounting should be restricted after grace.");
Require(restrictedDecision.AccessState == LocalServerEntitlementAccessStates.Restricted, "Accounting should enter restricted state after grace.");

var expiredDecision = await EvaluateAsync("Accounting", new DateOnly(2026, 9, 15));
Require(!expiredDecision.IsAllowed, "Accounting should be denied after offline validity.");
Require(expiredDecision.AccessState == LocalServerEntitlementAccessStates.Expired, "Accounting should be expired after offline validity.");

var disabledDecision = await EvaluateAsync("Reports", new DateOnly(2026, 8, 15));
Require(!disabledDecision.IsAllowed, "Reports should be denied when disabled.");
Require(disabledDecision.AccessState == LocalServerEntitlementAccessStates.ModuleDisabled, "Reports should be module-disabled.");

var cachedEntitlement = await cache.GetCurrentAsync();

Require(cachedEntitlement?.EntitlementVersion == 100, "Cached entitlement should remain at version 100.");
var verifiedCachedEntitlement = cachedEntitlement
    ?? throw new InvalidOperationException("Cached entitlement should exist.");
var importedEntitlement = pullResult.Entitlement
    ?? throw new InvalidOperationException("Pulled entitlement should exist.");

Console.WriteLine(JsonSerializer.Serialize(
    new
    {
        status = "Passed",
        installationId,
        cachedVersion = verifiedCachedEntitlement.EntitlementVersion,
        importedBundleIssueId = importedEntitlement.BundleIssueId,
        pulledAtUtc = pullResult.PulledAtUtc,
        heartbeatStatus = heartbeatResult.Heartbeat.HeartbeatStatus,
        heartbeatLicenseStatus = heartbeatResult.Heartbeat.LicenseStatus,
        badSignatureRejected = badSignatureResult.FailureCode,
        olderVersionRejected = olderImportResult.FailureCode,
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

static ClientPortalSignedEntitlementBundleResponse CreateSignedBundle(
    Guid clientId,
    string installationId,
    long entitlementVersion)
{
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
        new DateOnly(2026, 8, 31),
        new DateOnly(2026, 8, 24),
        new DateOnly(2026, 9, 7),
        new DateOnly(2026, 9, 14),
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

internal sealed class FixedLocalServerClock : ILocalServerClock
{
    public FixedLocalServerClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
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
            heartbeatRequest.Detail);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
}
