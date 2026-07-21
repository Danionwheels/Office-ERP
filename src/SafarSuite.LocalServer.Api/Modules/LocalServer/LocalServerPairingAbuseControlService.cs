using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Domain.Pairing;
using SafarSuite.LocalServer.Infrastructure.Pairing;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public sealed class LocalServerPairingAbuseControlService
{
    private const int SourceHashPrefixLength = 24;
    private const string AutomaticActor = "LocalServerPairingAbuseControl";

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _rateBuckets = new(StringComparer.Ordinal);
    private readonly ILocalServerPairingSecurityEventStore _eventStore;
    private readonly LocalServerPairingAbuseControlOptions _options;

    public LocalServerPairingAbuseControlService(
        ILocalServerPairingSecurityEventStore eventStore,
        LocalServerPairingAbuseControlOptions options)
    {
        _eventStore = eventStore;
        _options = options;
    }

    public async Task<LocalServerPairingAbuseGuardResult> GuardAsync(
        HttpRequest httpRequest,
        string endpointGroup,
        CancellationToken cancellationToken)
    {
        var source = ResolveSource(httpRequest);

        if (!_options.Enabled)
        {
            return LocalServerPairingAbuseGuardResult.Allowed(source.SourceKey, source.RemoteAddress);
        }

        var bodyFailure = await TryRejectOversizedAnonymousRequestAsync(
            httpRequest,
            endpointGroup,
            cancellationToken);

        if (bodyFailure is not null)
        {
            return LocalServerPairingAbuseGuardResult.Denied(
                source.SourceKey,
                source.RemoteAddress,
                bodyFailure);
        }

        var now = DateTimeOffset.UtcNow;
        var activeDecision = await _eventStore.GetActiveSourceDecisionAsync(
            source.SourceKey,
            now,
            cancellationToken);

        if (activeDecision is not null)
        {
            int? retryAfterSeconds = activeDecision.ExpiresAtUtc is null
                ? null
                : Math.Max(1, (int)Math.Ceiling((activeDecision.ExpiresAtUtc.Value - now).TotalSeconds));

            var statusCode = string.Equals(
                activeDecision.Action,
                LocalServerPairingAbuseActions.Deny,
                StringComparison.Ordinal)
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status429TooManyRequests;
            var code = statusCode == StatusCodes.Status403Forbidden
                ? "PairingSourceDenied"
                : "PairingSourceQuarantined";
            var eventType = statusCode == StatusCodes.Status403Forbidden
                ? LocalServerPairingSecurityEventTypes.SourceDenied
                : LocalServerPairingSecurityEventTypes.SourceQuarantined;
            var action = statusCode == StatusCodes.Status403Forbidden
                ? LocalServerPairingAbuseActions.Deny
                : LocalServerPairingAbuseActions.Quarantine;
            var failure = await CreateProblemResultAsync(
                httpRequest,
                source,
                code,
                activeDecision.Reason,
                eventType,
                LocalServerPairingSecuritySeverities.Warning,
                endpointGroup,
                "RemoteAddress",
                statusCode,
                action,
                retryAfterSeconds,
                activeDecision.CreatedAtUtc,
                activeDecision.ExpiresAtUtc,
                Count: 1,
                PairingRequestId: null,
                DeviceId: null,
                DeviceInstallIdHash: null,
                DeviceFingerprintHash: null,
                cancellationToken);

            return LocalServerPairingAbuseGuardResult.Denied(
                source.SourceKey,
                source.RemoteAddress,
                failure);
        }

        var limit = ResolveRateLimit(endpointGroup);
        var rateResult = TrackRate(
            $"request:{endpointGroup}:{source.SourceKey}",
            limit.MaxCount,
            limit.Window,
            now);

        if (!rateResult.IsLimited)
        {
            return LocalServerPairingAbuseGuardResult.Allowed(source.SourceKey, source.RemoteAddress);
        }

        var rateFailure = await CreateProblemResultAsync(
            httpRequest,
            source,
            "PairingRateLimited",
            limit.Detail,
            LocalServerPairingSecurityEventTypes.RateLimited,
            LocalServerPairingSecuritySeverities.Warning,
            endpointGroup,
            limit.LimitScope,
            StatusCodes.Status429TooManyRequests,
            LocalServerPairingAbuseActions.Throttle,
            rateResult.RetryAfterSeconds,
            rateResult.WindowStartedAtUtc,
            rateResult.WindowExpiresAtUtc,
            rateResult.Count,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);

        return LocalServerPairingAbuseGuardResult.Denied(
            source.SourceKey,
            source.RemoteAddress,
            rateFailure);
    }

    public async Task<IResult?> TryRejectOversizedAnonymousRequestAsync(
        HttpRequest httpRequest,
        string endpointGroup,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var maxBodyBytes = Math.Clamp(_options.AnonymousRequestBodyLimitBytes, 1024, 1024 * 1024);

        if (httpRequest.ContentLength is null || httpRequest.ContentLength <= maxBodyBytes)
        {
            return null;
        }

        return await CreateProblemResultAsync(
            httpRequest,
            ResolveSource(httpRequest),
            "PairingRequestTooLarge",
            "Anonymous pairing request body exceeded the configured limit.",
            LocalServerPairingSecurityEventTypes.RequestTooLarge,
            LocalServerPairingSecuritySeverities.Warning,
            endpointGroup,
            "RequestBody",
            StatusCodes.Status413PayloadTooLarge,
            LocalServerPairingAbuseActions.Reject,
            RetryAfterSeconds: null,
            WindowStartedAtUtc: null,
            WindowExpiresAtUtc: null,
            Count: 1,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);
    }

    public async Task RecordDuplicateCoalescedAsync(
        LocalServerPairingAbuseGuardResult guard,
        Guid pairingRequestId,
        Guid deviceId,
        string? deviceFingerprintHash,
        CancellationToken cancellationToken)
    {
        await RecordSecurityEventAsync(
            new LocalServerPairingAbuseSource(guard.SourceKey, guard.RemoteAddress),
            LocalServerPairingSecurityEventTypes.DuplicateCoalesced,
            LocalServerPairingSecuritySeverities.Information,
            LocalServerPairingAbuseEndpointGroups.PairingRequest,
            LocalServerPairingAbuseActions.Coalesce,
            "Duplicate active pairing request was coalesced to the existing device request.",
            Count: 1,
            WindowStartedAtUtc: null,
            WindowExpiresAtUtc: null,
            PairingRequestId: pairingRequestId,
            DeviceId: deviceId,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: NormalizeOptional(deviceFingerprintHash, 160),
            cancellationToken);
    }

    public async Task<IResult> RejectPendingQueueFullAsync(
        HttpRequest httpRequest,
        LocalServerPairingAbuseGuardResult guard,
        string? deviceFingerprintHash,
        CancellationToken cancellationToken)
    {
        return await CreateProblemResultAsync(
            httpRequest,
            new LocalServerPairingAbuseSource(guard.SourceKey, guard.RemoteAddress),
            "PairingPendingQueueFull",
            "The LocalServer pending pairing queue is full. Ask a manager to review pending devices before adding another one.",
            LocalServerPairingSecurityEventTypes.PendingQueueFull,
            LocalServerPairingSecuritySeverities.Warning,
            LocalServerPairingAbuseEndpointGroups.PairingRequest,
            "PendingQueue",
            StatusCodes.Status409Conflict,
            LocalServerPairingAbuseActions.Reject,
            RetryAfterSeconds: null,
            WindowStartedAtUtc: null,
            WindowExpiresAtUtc: null,
            Count: 1,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: NormalizeOptional(deviceFingerprintHash, 160),
            cancellationToken);
    }

    public async Task RecordCredentialRejectedAsync(
        HttpRequest httpRequest,
        string endpointGroup,
        string? failureCode,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var source = ResolveSource(httpRequest);
        var now = DateTimeOffset.UtcNow;
        var detail = string.IsNullOrWhiteSpace(failureCode)
            ? "Device credential was rejected."
            : $"Device credential was rejected with code '{NormalizeOptional(failureCode, 120)}'.";

        await RecordSecurityEventAsync(
            source,
            LocalServerPairingSecurityEventTypes.CredentialRejected,
            LocalServerPairingSecuritySeverities.Warning,
            endpointGroup,
            LocalServerPairingAbuseActions.Reject,
            detail,
            Count: 1,
            WindowStartedAtUtc: null,
            WindowExpiresAtUtc: null,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);

        await ApplyAutomaticQuarantineIfLimitExceededAsync(
            source,
            $"credential-failure:{endpointGroup}:{source.SourceKey}",
            endpointGroup,
            Math.Clamp(_options.CredentialFailuresPerDevicePer15Minutes, 1, 1000),
            TimeSpan.FromMinutes(15),
            "Repeated device credential failures exceeded the configured backoff limit.",
            now,
            cancellationToken);
    }

    public async Task RecordFirstManagerTokenRejectedAsync(
        HttpRequest httpRequest,
        string? failureCode,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var source = ResolveSource(httpRequest);
        var now = DateTimeOffset.UtcNow;
        var detail = string.IsNullOrWhiteSpace(failureCode)
            ? "First-manager setup token was rejected."
            : $"First-manager setup token was rejected with code '{NormalizeOptional(failureCode, 120)}'.";

        await RecordSecurityEventAsync(
            source,
            LocalServerPairingSecurityEventTypes.FirstManagerTokenRejected,
            LocalServerPairingSecuritySeverities.Warning,
            LocalServerPairingAbuseEndpointGroups.FirstManagerTokenImport,
            LocalServerPairingAbuseActions.Reject,
            detail,
            Count: 1,
            WindowStartedAtUtc: null,
            WindowExpiresAtUtc: null,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);

        await ApplyAutomaticQuarantineIfLimitExceededAsync(
            source,
            $"first-manager-token-failure:{source.SourceKey}",
            LocalServerPairingAbuseEndpointGroups.FirstManagerTokenImport,
            Math.Clamp(_options.LoginFailuresPerActorPer15Minutes, 1, 1000),
            TimeSpan.FromMinutes(15),
            "Repeated first-manager setup-token failures exceeded the configured backoff limit.",
            now,
            cancellationToken);
    }

    public async Task<LocalServerPairingAbuseSourceDecision> SaveSourceDecisionAsync(
        string sourceKey,
        string action,
        ChangeLocalServerPairingAbuseSourceRequest? request,
        string managerActor,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedAction = NormalizeSourceAction(action);
        var actor = NormalizeOptional(managerActor, 160)
            ?? NormalizeOptional(request?.Actor, 160)
            ?? "LocalServerManager";
        var reason = NormalizeOptional(request?.Reason, 500)
            ?? ResolveDefaultSourceDecisionReason(normalizedAction);
        var expiresAtUtc = ResolveSourceDecisionExpiry(normalizedAction, request, now);
        var decision = new LocalServerPairingAbuseSourceDecision(
            NormalizeSourceKey(sourceKey),
            normalizedAction,
            reason,
            actor,
            now,
            expiresAtUtc);

        await _eventStore.SaveSourceDecisionAsync(decision, cancellationToken);

        var eventType = normalizedAction switch
        {
            LocalServerPairingAbuseActions.Deny => LocalServerPairingSecurityEventTypes.SourceDenied,
            LocalServerPairingAbuseActions.Quarantine => LocalServerPairingSecurityEventTypes.SourceQuarantined,
            _ => LocalServerPairingSecurityEventTypes.SourceAllowed
        };

        await RecordSecurityEventAsync(
            new LocalServerPairingAbuseSource(decision.SourceKey, null),
            eventType,
            LocalServerPairingSecuritySeverities.Information,
            LocalServerPairingAbuseEndpointGroups.PairingRequest,
            normalizedAction,
            reason,
            Count: 1,
            WindowStartedAtUtc: now,
            WindowExpiresAtUtc: expiresAtUtc,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);

        return decision;
    }

    public static string? ResolveEndpointGroupForPath(PathString path)
    {
        var value = path.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(value, "/.well-known/safarsuite-local-server", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.Discovery;
        }

        if (string.Equals(value, "/api/v1/local-server/pairing/hello", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.PairingHello;
        }

        if (string.Equals(value, "/api/v1/local-server/pairing/requests", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.PairingRequest;
        }

        if (value.StartsWith("/api/v1/local-server/pairing/requests/", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.PairingStatus;
        }

        if (string.Equals(
                value,
                "/api/v1/local-server/pairing/first-manager-token/import",
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.FirstManagerTokenImport;
        }

        if (string.Equals(value, "/api/v1/local-server/device-credentials/verify", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "/api/v1/local-server/device-credentials/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.DeviceCredential;
        }

        if (string.Equals(value, "/api/v1/local-server/pairing/manager-sessions", StringComparison.OrdinalIgnoreCase))
        {
            return LocalServerPairingAbuseEndpointGroups.ManagerSession;
        }

        return null;
    }

    public static bool IsSourceDecisionActive(
        LocalServerPairingAbuseSourceDecision decision,
        DateTimeOffset asOfUtc)
    {
        return (string.Equals(decision.Action, LocalServerPairingAbuseActions.Deny, StringComparison.Ordinal)
                || string.Equals(decision.Action, LocalServerPairingAbuseActions.Quarantine, StringComparison.Ordinal))
            && (decision.ExpiresAtUtc is null || decision.ExpiresAtUtc > asOfUtc);
    }

    private async Task<IResult> CreateProblemResultAsync(
        HttpRequest httpRequest,
        LocalServerPairingAbuseSource source,
        string code,
        string detail,
        string eventType,
        string severity,
        string endpointGroup,
        string limitScope,
        int statusCode,
        string action,
        int? RetryAfterSeconds,
        DateTimeOffset? WindowStartedAtUtc,
        DateTimeOffset? WindowExpiresAtUtc,
        int Count,
        Guid? PairingRequestId,
        Guid? DeviceId,
        string? DeviceInstallIdHash,
        string? DeviceFingerprintHash,
        CancellationToken cancellationToken)
    {
        var securityEvent = await RecordSecurityEventAsync(
            source,
            eventType,
            severity,
            endpointGroup,
            action,
            detail,
            Count,
            WindowStartedAtUtc,
            WindowExpiresAtUtc,
            PairingRequestId,
            DeviceId,
            DeviceInstallIdHash,
            DeviceFingerprintHash,
            cancellationToken);

        if (RetryAfterSeconds is not null)
        {
            httpRequest.HttpContext.Response.Headers.RetryAfter =
                RetryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
        }

        return Results.Json(
            new LocalServerPairingAbuseProblemResponse(
                code,
                detail,
                securityEvent.EventId,
                limitScope,
                endpointGroup,
                RetryAfterSeconds,
                WindowStartedAtUtc,
                WindowExpiresAtUtc),
            statusCode: statusCode);
    }

    private async Task<LocalServerPairingSecurityEvent> RecordSecurityEventAsync(
        LocalServerPairingAbuseSource source,
        string eventType,
        string severity,
        string endpointGroup,
        string action,
        string detail,
        int Count,
        DateTimeOffset? WindowStartedAtUtc,
        DateTimeOffset? WindowExpiresAtUtc,
        Guid? PairingRequestId,
        Guid? DeviceId,
        string? DeviceInstallIdHash,
        string? DeviceFingerprintHash,
        CancellationToken cancellationToken)
    {
        var securityEvent = new LocalServerPairingSecurityEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            eventType,
            severity,
            endpointGroup,
            source.SourceKey,
            source.RemoteAddress,
            NormalizeOptional(DeviceInstallIdHash, 160),
            NormalizeOptional(DeviceFingerprintHash, 160),
            PairingRequestId,
            DeviceId,
            Count,
            WindowStartedAtUtc,
            WindowExpiresAtUtc,
            action,
            detail);

        await _eventStore.RecordEventAsync(securityEvent, cancellationToken);

        return securityEvent;
    }

    private async Task ApplyAutomaticQuarantineIfLimitExceededAsync(
        LocalServerPairingAbuseSource source,
        string bucketKey,
        string endpointGroup,
        int maxCount,
        TimeSpan window,
        string detail,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var failureRate = TrackRate(bucketKey, maxCount, window, now);

        if (!failureRate.IsLimited)
        {
            return;
        }

        var expiresAtUtc = now.AddMinutes(Math.Clamp(_options.TemporaryQuarantineMinutes, 1, 24 * 60));
        var decision = new LocalServerPairingAbuseSourceDecision(
            source.SourceKey,
            LocalServerPairingAbuseActions.Quarantine,
            detail,
            AutomaticActor,
            now,
            expiresAtUtc);

        await _eventStore.SaveSourceDecisionAsync(decision, cancellationToken);
        await RecordSecurityEventAsync(
            source,
            LocalServerPairingSecurityEventTypes.SourceQuarantined,
            LocalServerPairingSecuritySeverities.Warning,
            endpointGroup,
            LocalServerPairingAbuseActions.Quarantine,
            detail,
            failureRate.Count,
            failureRate.WindowStartedAtUtc,
            expiresAtUtc,
            PairingRequestId: null,
            DeviceId: null,
            DeviceInstallIdHash: null,
            DeviceFingerprintHash: null,
            cancellationToken);
    }

    private LocalServerPairingAbuseRateLimit ResolveRateLimit(string endpointGroup)
    {
        return endpointGroup switch
        {
            LocalServerPairingAbuseEndpointGroups.Discovery => new(
                Math.Clamp(_options.DiscoveryRequestsPerRemotePerMinute, 1, 100000),
                TimeSpan.FromMinutes(1),
                "RemoteAddress",
                "Too many discovery requests from this source."),
            LocalServerPairingAbuseEndpointGroups.PairingHello => new(
                Math.Clamp(_options.HelloRequestsPerRemotePerMinute, 1, 100000),
                TimeSpan.FromMinutes(1),
                "RemoteAddress",
                "Too many pairing hello requests from this source."),
            LocalServerPairingAbuseEndpointGroups.PairingRequest => new(
                Math.Clamp(_options.PairingRequestsPerRemotePerHour, 1, 100000),
                TimeSpan.FromHours(1),
                "RemoteAddress",
                "Too many pairing requests from this source."),
            LocalServerPairingAbuseEndpointGroups.PairingStatus => new(
                Math.Clamp(_options.PairingStatusPollsPerRequestPerMinute, 1, 100000),
                TimeSpan.FromMinutes(1),
                "PairingRequest",
                "Too many pairing status polls from this source."),
            LocalServerPairingAbuseEndpointGroups.FirstManagerTokenImport => new(
                Math.Clamp(_options.LoginFailuresPerActorPer15Minutes, 1, 100000),
                TimeSpan.FromMinutes(15),
                "RemoteAddress",
                "Too many first-manager setup-token import attempts from this source."),
            LocalServerPairingAbuseEndpointGroups.ManagerSession => new(
                120,
                TimeSpan.FromMinutes(1),
                "RemoteAddress",
                "Too many manager session requests from this source."),
            LocalServerPairingAbuseEndpointGroups.DeviceCredential => new(
                240,
                TimeSpan.FromMinutes(1),
                "RemoteAddress",
                "Too many device credential requests from this source."),
            _ => new(
                120,
                TimeSpan.FromMinutes(1),
                "RemoteAddress",
                "Too many local pairing requests from this source.")
        };
    }

    private RateWindowResult TrackRate(
        string bucketKey,
        int maxCount,
        TimeSpan window,
        DateTimeOffset now)
    {
        var bucket = _rateBuckets.GetOrAdd(bucketKey, _ => new Queue<DateTimeOffset>());

        lock (bucket)
        {
            var cutoff = now.Subtract(window);

            while (bucket.Count > 0 && bucket.Peek() <= cutoff)
            {
                bucket.Dequeue();
            }

            var windowStartedAtUtc = bucket.Count == 0 ? now : bucket.Peek();
            var windowExpiresAtUtc = windowStartedAtUtc.Add(window);

            if (bucket.Count >= maxCount)
            {
                return new RateWindowResult(
                    true,
                    bucket.Count + 1,
                    windowStartedAtUtc,
                    windowExpiresAtUtc,
                    Math.Max(1, (int)Math.Ceiling((windowExpiresAtUtc - now).TotalSeconds)));
            }

            bucket.Enqueue(now);

            return new RateWindowResult(
                false,
                bucket.Count,
                windowStartedAtUtc,
                windowExpiresAtUtc,
                null);
        }
    }

    private DateTimeOffset? ResolveSourceDecisionExpiry(
        string action,
        ChangeLocalServerPairingAbuseSourceRequest? request,
        DateTimeOffset createdAtUtc)
    {
        if (string.Equals(action, LocalServerPairingAbuseActions.Release, StringComparison.Ordinal))
        {
            return null;
        }

        var requestedMinutes = request?.ExpiresInMinutes;

        if (requestedMinutes is not null && requestedMinutes > 0)
        {
            return createdAtUtc.AddMinutes(Math.Clamp(requestedMinutes.Value, 1, 60 * 24 * 30));
        }

        return string.Equals(action, LocalServerPairingAbuseActions.Deny, StringComparison.Ordinal)
            ? createdAtUtc.AddHours(Math.Clamp(_options.ManualDenyTtlHours, 1, 24 * 30))
            : createdAtUtc.AddMinutes(Math.Clamp(_options.TemporaryQuarantineMinutes, 1, 24 * 60));
    }

    private static LocalServerPairingAbuseSource ResolveSource(HttpRequest request)
    {
        var remoteAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString();
        var sourceMaterial = string.IsNullOrWhiteSpace(remoteAddress)
            ? "unknown"
            : remoteAddress.Trim();

        return new LocalServerPairingAbuseSource(
            $"remote:{Sha256Prefix(sourceMaterial)}",
            string.IsNullOrWhiteSpace(remoteAddress) ? null : remoteAddress);
    }

    private static string ResolveDefaultSourceDecisionReason(string action)
    {
        return action switch
        {
            LocalServerPairingAbuseActions.Deny => "Manager temporarily denied this pairing source.",
            LocalServerPairingAbuseActions.Quarantine => "Manager temporarily quarantined this pairing source.",
            _ => "Manager released this pairing source."
        };
    }

    private static string NormalizeSourceAction(string action)
    {
        if (string.Equals(action, LocalServerPairingAbuseActions.Deny, StringComparison.Ordinal))
        {
            return LocalServerPairingAbuseActions.Deny;
        }

        if (string.Equals(action, LocalServerPairingAbuseActions.Quarantine, StringComparison.Ordinal))
        {
            return LocalServerPairingAbuseActions.Quarantine;
        }

        return LocalServerPairingAbuseActions.Release;
    }

    private static string NormalizeSourceKey(string sourceKey)
    {
        var normalized = NormalizeOptional(sourceKey, 160);

        return normalized is null
            ? "remote:unknown"
            : normalized.Replace('/', '-').Replace('\\', '-');
    }

    private static string? NormalizeOptional(string? value, int maxLength)
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

    private static string Sha256Prefix(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));

        return Convert.ToHexString(hash)
            .ToLowerInvariant()[..SourceHashPrefixLength];
    }

    private sealed record LocalServerPairingAbuseSource(
        string SourceKey,
        string? RemoteAddress);

    private sealed record LocalServerPairingAbuseRateLimit(
        int MaxCount,
        TimeSpan Window,
        string LimitScope,
        string Detail);

    private sealed record RateWindowResult(
        bool IsLimited,
        int Count,
        DateTimeOffset WindowStartedAtUtc,
        DateTimeOffset WindowExpiresAtUtc,
        int? RetryAfterSeconds);
}

public sealed record LocalServerPairingAbuseGuardResult(
    string SourceKey,
    string? RemoteAddress,
    IResult? Failure)
{
    public bool IsAllowed => Failure is null;

    public static LocalServerPairingAbuseGuardResult Allowed(
        string sourceKey,
        string? remoteAddress)
    {
        return new LocalServerPairingAbuseGuardResult(sourceKey, remoteAddress, null);
    }

    public static LocalServerPairingAbuseGuardResult Denied(
        string sourceKey,
        string? remoteAddress,
        IResult failure)
    {
        return new LocalServerPairingAbuseGuardResult(sourceKey, remoteAddress, failure);
    }
}

public static class LocalServerPairingAbuseControlApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLocalServerPairingAbuseControls(
        this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var endpointGroup = LocalServerPairingAbuseControlService.ResolveEndpointGroupForPath(
                context.Request.Path);

            if (endpointGroup is null)
            {
                await next();
                return;
            }

            var abuseControls = context.RequestServices.GetRequiredService<LocalServerPairingAbuseControlService>();
            var failure = await abuseControls.TryRejectOversizedAnonymousRequestAsync(
                context.Request,
                endpointGroup,
                context.RequestAborted);

            if (failure is not null)
            {
                await failure.ExecuteAsync(context);
                return;
            }

            await next();
        });
    }
}
