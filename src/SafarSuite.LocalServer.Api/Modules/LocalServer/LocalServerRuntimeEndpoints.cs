using System.Security.Cryptography;
using System.Text;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.LocalServer.Application.Commands.GetAppActivationRevocationStatus;
using SafarSuite.LocalServer.Application.Commands.ProcessInstallationCommandsFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Entitlements.Ports;
using SafarSuite.LocalServer.Application.Entitlements.PullEntitlementFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.Heartbeats.ReportHeartbeatFromBootstrapConfiguration;
using SafarSuite.LocalServer.Application.ModuleGateway.EvaluateModuleAccess;
using SafarSuite.LocalServer.Application.Pairing.Ports;
using SafarSuite.LocalServer.Application.Registration.Ports;
using SafarSuite.LocalServer.Application.Registration.RegisterInstallationFromBootstrapBundle;
using SafarSuite.LocalServer.Domain.Entitlements;
using SafarSuite.LocalServer.Domain.Pairing;
using SafarSuite.LocalServer.Domain.Registration;

namespace SafarSuite.LocalServer.Api.Modules.LocalServer;

public static class LocalServerRuntimeEndpoints
{
    public static IEndpointRouteBuilder MapLocalServerRuntimeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/.well-known/safarsuite-local-server", GetPairingDiscoveryAsync)
            .WithName("GetLocalServerPairingDiscovery");

        var group = endpoints
            .MapGroup("/api/v1/local-server")
            .WithTags("Local Server Runtime");

        group.MapPost("/pairing/hello", GetPairingHelloAsync)
            .WithName("GetLocalServerPairingHello");

        group.MapPost("/pairing/requests", CreateDevicePairingRequestAsync)
            .WithName("CreateLocalServerDevicePairingRequest");

        group.MapGet("/pairing/requests/{pairingRequestId:guid}", GetDevicePairingRequestAsync)
            .WithName("GetLocalServerDevicePairingRequest");

        group.MapPost("/pairing/first-manager-token/import", ImportFirstManagerSetupTokenAsync)
            .WithName("ImportLocalServerFirstManagerSetupToken");

        group.MapGet("/devices", ListDevicesAsync)
            .WithName("ListLocalServerDevices");

        group.MapGet("/devices/pending", ListPendingDevicesAsync)
            .WithName("ListPendingLocalServerDevices");

        group.MapPost("/devices/{deviceId:guid}/approve", ApproveDeviceAsync)
            .WithName("ApproveLocalServerDevice");

        group.MapPost("/devices/{deviceId:guid}/suspend", SuspendDeviceAsync)
            .WithName("SuspendLocalServerDevice");

        group.MapPost("/devices/{deviceId:guid}/revoke", RevokeDeviceAsync)
            .WithName("RevokeLocalServerDevice");

        group.MapGet("/bootstrap", GetBootstrapStatusAsync)
            .WithName("GetLocalServerBootstrapStatus");

        group.MapPost("/bootstrap-package/import", ImportBootstrapPackageAsync)
            .WithName("ImportLocalServerBootstrapPackage");

        group.MapPost("/entitlement/pull", PullEntitlementAsync)
            .WithName("PullLocalServerEntitlement");

        group.MapPost("/heartbeat", ReportHeartbeatAsync)
            .WithName("ReportLocalServerHeartbeatFromBootstrap");

        group.MapPost("/commands/process", ProcessCommandsAsync)
            .WithName("ProcessLocalServerCommands");

        group.MapPost("/app-activations/revocation-status", GetAppActivationRevocationStatusAsync)
            .WithName("GetLocalServerAppActivationRevocationStatus");

        group.MapPost("/modules/access", EvaluateModuleAccessAsync)
            .WithName("EvaluateLocalServerModuleAccess");

        group.MapGet("/modules/{moduleCode}/access", EvaluateModuleAccessFromBootstrapAsync)
            .WithName("EvaluateLocalServerModuleAccessFromBootstrap");

        return endpoints;
    }

    private static async Task<IResult> GetPairingDiscoveryAsync(
        HttpRequest httpRequest,
        LocalServerPairingOptions pairingOptions,
        ILocalServerBootstrapConfigurationStore configurationStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);
        var generatedAtUtc = DateTimeOffset.UtcNow;

        return Results.Ok(new LocalServerPairingDiscoveryResponse(
            FormatVersion: LocalServerPairingFormats.DiscoveryVersion,
            HasBootstrapConfiguration: configuration is not null,
            ClientId: configuration?.ClientId,
            InstallationIdHint: configuration?.InstallationId,
            DisplayName: ResolvePairingDisplayName(pairingOptions, configuration),
            PairingMode: ResolvePairingMode(pairingOptions),
            DeploymentProfile: configuration is null
                ? null
                : ToDeploymentProfileResponse(configuration.DeploymentProfile),
            UrlCandidates: BuildUrlCandidates(httpRequest, pairingOptions),
            TlsCertificateSha256: ToOptional(pairingOptions.TlsCertificateSha256),
            TlsCaSha256: ToOptional(pairingOptions.TlsCaSha256),
            ServerPairingKeySha256: ToOptional(pairingOptions.ServerPairingKeySha256),
            BootstrapPayloadSha256: configuration?.PayloadSha256,
            BootstrapSignatureKeyId: configuration?.SignatureKeyId,
            GeneratedAtUtc: generatedAtUtc));
    }

    private static async Task<IResult> GetPairingHelloAsync(
        HttpRequest httpRequest,
        LocalServerPairingHelloRequest request,
        LocalServerPairingOptions pairingOptions,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerEntitlementCache entitlementCache,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before LocalServer pairing hello can be used.");
        }

        if (!string.Equals(
                request.FormatVersion,
                LocalServerPairingFormats.HelloRequestVersion,
                StringComparison.Ordinal))
        {
            return ToFailureResult(
                "PairingHelloFormatUnsupported",
                $"Pairing hello format '{request.FormatVersion}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientNonce))
        {
            return ToFailureResult(
                "ClientNonceRequired",
                "A client nonce is required before LocalServer pairing hello can be used.");
        }

        var entitlement = await entitlementCache.GetCurrentAsync(cancellationToken);
        var serverNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

        return Results.Ok(new LocalServerPairingHelloResponse(
            FormatVersion: LocalServerPairingFormats.HelloResponseVersion,
            ClientId: configuration.ClientId,
            InstallationId: configuration.InstallationId,
            BootstrapPackageId: configuration.BootstrapPackageId,
            DeploymentProfile: ToDeploymentProfileResponse(configuration.DeploymentProfile),
            DisplayName: ResolvePairingDisplayName(pairingOptions, configuration),
            LocalServerVersion: configuration.LocalServerVersion,
            PairingMode: ResolvePairingMode(pairingOptions),
            UrlCandidates: BuildUrlCandidates(httpRequest, pairingOptions),
            TlsCertificateSha256: ToOptional(pairingOptions.TlsCertificateSha256),
            TlsCaSha256: ToOptional(pairingOptions.TlsCaSha256),
            ServerPairingPublicKey: ToOptional(pairingOptions.ServerPairingPublicKey),
            ServerPairingKeySha256: ToOptional(pairingOptions.ServerPairingKeySha256),
            BootstrapPayloadSha256: configuration.PayloadSha256,
            BootstrapSignatureAlgorithm: configuration.SignatureAlgorithm,
            BootstrapSignatureKeyId: configuration.SignatureKeyId,
            EntitlementVersion: entitlement?.EntitlementVersion,
            PaidUntil: entitlement?.PaidUntil,
            OfflineValidUntil: entitlement?.OfflineValidUntil,
            ClientNonce: request.ClientNonce.Trim(),
            ServerNonce: serverNonce,
            AppVersion: ToOptional(request.AppVersion),
            RequestedBy: ToOptional(request.RequestedBy),
            GeneratedAtUtc: DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> CreateDevicePairingRequestAsync(
        LocalServerDevicePairingRequest request,
        LocalServerPairingOptions pairingOptions,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before creating a device pairing request.");
        }

        if (string.Equals(
                ResolvePairingMode(pairingOptions),
                LocalServerPairingModes.PairingDisabled,
                StringComparison.Ordinal))
        {
            return ToFailureResult(
                "DevicePairingDisabled",
                "Device pairing is disabled for this LocalServer.");
        }

        if (!string.Equals(
                request.FormatVersion,
                LocalServerPairingFormats.DevicePairingRequestVersion,
                StringComparison.Ordinal))
        {
            return ToFailureResult(
                "DevicePairingFormatUnsupported",
                $"Device pairing request format '{request.FormatVersion}' is not supported.");
        }

        if (!string.Equals(request.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "InstallationMismatch",
                "Device pairing request installation id does not match this LocalServer.");
        }

        var displayName = NormalizeRequired(request.DeviceDisplayName, 120);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return ToFailureResult(
                "DeviceDisplayNameRequired",
                "A device display name is required before creating a pairing request.");
        }

        var devicePublicKey = NormalizeRequired(request.DevicePublicKey, 8192);

        if (string.IsNullOrWhiteSpace(devicePublicKey))
        {
            return ToFailureResult(
                "DevicePublicKeyRequired",
                "A device public key is required before creating a pairing request.");
        }

        var devicePublicKeySha256 = Sha256Hex(devicePublicKey);
        var existing = (await pairingStore.ListAsync(cancellationToken))
            .FirstOrDefault(record => record.IsActiveRequestForPublicKey(
                configuration.InstallationId,
                devicePublicKeySha256));

        if (existing is not null)
        {
            return Results.Ok(ToPairingRequestResponse(existing));
        }

        var requestedAtUtc = request.RequestedAtUtc ?? DateTimeOffset.UtcNow;
        var record = new LocalServerDevicePairingRecord(
            PairingRequestId: Guid.NewGuid(),
            DeviceId: Guid.NewGuid(),
            ClientId: configuration.ClientId,
            InstallationId: configuration.InstallationId,
            RequestFormatVersion: request.FormatVersion,
            DeviceDisplayName: displayName!,
            DevicePublicKey: devicePublicKey!,
            DevicePublicKeySha256: devicePublicKeySha256,
            DeviceFingerprintHash: NormalizeOptional(request.DeviceFingerprintHash, 160),
            WindowsUserHint: NormalizeOptional(request.WindowsUserHint, 160),
            AppVersion: NormalizeOptional(request.AppVersion, 80),
            HelloServerNonce: NormalizeOptional(request.HelloServerNonce, 500),
            HelloClientNonce: NormalizeOptional(request.HelloClientNonce, 500),
            PairingRequestStatus: LocalServerDevicePairingRecordStatuses.Pending,
            DeviceStatus: LocalServerDevicePairingRecordStatuses.Pending,
            RequestedAtUtc: requestedAtUtc,
            ExpiresAtUtc: requestedAtUtc.AddHours(pairingOptions.RequestExpiresInHours),
            UpdatedAtUtc: requestedAtUtc);

        await pairingStore.SaveAsync(record, cancellationToken);

        return Results.Created(
            $"/api/v1/local-server/pairing/requests/{record.PairingRequestId:D}",
            ToPairingRequestResponse(record));
    }

    private static async Task<IResult> GetDevicePairingRequestAsync(
        Guid pairingRequestId,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var record = await pairingStore.GetByPairingRequestIdAsync(
            pairingRequestId,
            cancellationToken);

        return record is null
            ? NotFound("PairingRequestNotFound", "Device pairing request was not found.")
            : Results.Ok(ToPairingRequestResponse(record));
    }

    private static async Task<IResult> ImportFirstManagerSetupTokenAsync(
        LocalServerSignedFirstManagerSetupTokenResponse? request,
        ILocalServerFirstManagerSetupTokenVerifier verifier,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var importedAtUtc = DateTimeOffset.UtcNow;
        var verification = verifier.Verify(request, importedAtUtc);

        if (!verification.IsSuccess)
        {
            return ToFailureResult(verification.FailureCode, verification.Detail);
        }

        var payload = verification.Payload!;
        var signature = verification.Signature!;
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before importing a first-manager setup token.");
        }

        if (payload.ClientId != configuration.ClientId)
        {
            return ToFailureResult(
                "ClientMismatch",
                "First-manager setup token belongs to another client.");
        }

        if (!string.Equals(payload.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "InstallationMismatch",
                "First-manager setup token belongs to another LocalServer installation.");
        }

        var existingConsumption = await pairingStore.GetFirstManagerSetupTokenConsumptionAsync(
            payload.TokenId,
            cancellationToken);

        if (existingConsumption is not null)
        {
            return ToFailureResult(
                "FirstManagerSetupTokenAlreadyConsumed",
                "First-manager setup token has already been consumed by this LocalServer.");
        }

        var record = await pairingStore.GetByPairingRequestIdAsync(
            payload.PendingDeviceRequestId,
            cancellationToken);

        if (record is null)
        {
            return NotFound(
                "PairingRequestNotFound",
                "First-manager setup token references a pairing request that was not found.");
        }

        if (record.ClientId != configuration.ClientId
            || !string.Equals(record.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "PairingRequestMismatch",
                "First-manager setup token references a pairing request for another client or installation.");
        }

        if (!string.Equals(record.PairingRequestStatus, LocalServerDevicePairingRecordStatuses.Pending, StringComparison.Ordinal)
            || !string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Pending, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "DeviceStatusInvalid",
                "First-manager setup token can only approve a pending device pairing request.");
        }

        var deviceCredential = GenerateDeviceCredential();
        var deviceCredentialId = Guid.NewGuid().ToString("N");
        var approved = record.Approve(
            BuildFirstManagerActor(payload),
            "FirstManagerDevice",
            deviceCredentialId,
            Sha256Hex(deviceCredential),
            importedAtUtc);
        var consumption = new LocalServerFirstManagerSetupTokenConsumptionRecord(
            payload.TokenId,
            payload.ClientId,
            payload.InstallationId,
            payload.PendingDeviceRequestId,
            record.DeviceId,
            NormalizeRequired(payload.ManagerDisplayName, 160) ?? "First Manager",
            NormalizeOptional(payload.ManagerEmail, 160),
            NormalizeRequired(payload.CreatedBy, 160) ?? "SafarSuite Control Cloud",
            signature.KeyId.Trim(),
            signature.PayloadSha256,
            payload.IssuedAtUtc,
            payload.ExpiresAtUtc,
            importedAtUtc);

        var writeResult = await pairingStore.SaveDeviceAndFirstManagerSetupTokenConsumptionAsync(
            approved,
            consumption,
            cancellationToken);

        if (!writeResult.Succeeded)
        {
            return string.Equals(writeResult.FailureCode, "PairingRequestNotFound", StringComparison.Ordinal)
                ? NotFound(writeResult.FailureCode!, writeResult.Detail!)
                : ToFailureResult(writeResult.FailureCode, writeResult.Detail);
        }

        return Results.Ok(new ImportLocalServerFirstManagerSetupTokenResponse(
            payload.TokenId,
            payload.ClientId,
            payload.InstallationId,
            payload.PendingDeviceRequestId,
            approved.DeviceId,
            consumption.ManagerDisplayName,
            consumption.ManagerEmail,
            consumption.CreatedBy,
            ToDeviceResponse(approved),
            deviceCredential,
            signature.KeyId.Trim(),
            signature.PayloadSha256,
            importedAtUtc));
    }

    private static async Task<IResult> ListDevicesAsync(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before listing paired devices.");
        }

        var devices = (await pairingStore.ListAsync(cancellationToken))
            .Where(record => string.Equals(record.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
            .Select(ToDeviceResponse)
            .ToArray();

        return Results.Ok(new LocalServerDeviceRegisterResponse(devices));
    }

    private static async Task<IResult> ListPendingDevicesAsync(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before listing pending devices.");
        }

        var devices = (await pairingStore.ListAsync(cancellationToken))
            .Where(record => string.Equals(record.InstallationId, configuration.InstallationId, StringComparison.Ordinal)
                && string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Pending, StringComparison.Ordinal))
            .Select(ToDeviceResponse)
            .ToArray();

        return Results.Ok(new LocalServerDevicePairingRequestsResponse(devices));
    }

    private static async Task<IResult> ApproveDeviceAsync(
        Guid deviceId,
        ApproveLocalServerDeviceRequest request,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var deviceResult = await GetDeviceForManagerActionAsync(
            deviceId,
            configurationStore,
            pairingStore,
            cancellationToken);

        if (deviceResult.Failure is not null)
        {
            return deviceResult.Failure;
        }

        var record = deviceResult.Record!;

        if (string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Revoked, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "DeviceAlreadyRevoked",
                "A revoked device cannot be approved again. Create a new pairing request.");
        }

        if (string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Approved, StringComparison.Ordinal))
        {
            return Results.Ok(new ApproveLocalServerDeviceResponse(
                ToDeviceResponse(record),
                DeviceCredential: null));
        }

        var approvedBy = NormalizeRequired(request.ApprovedBy, 160);

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            return ToFailureResult(
                "ApprovedByRequired",
                "An approving manager or actor is required before approving a device.");
        }

        var deviceCredential = GenerateDeviceCredential();
        var deviceCredentialId = Guid.NewGuid().ToString("N");
        var approved = record.Approve(
            approvedBy,
            NormalizeRequired(request.AssignedRole, 80) ?? "ManagerApprovedDevice",
            deviceCredentialId,
            Sha256Hex(deviceCredential),
            DateTimeOffset.UtcNow);

        await pairingStore.SaveAsync(approved, cancellationToken);

        return Results.Ok(new ApproveLocalServerDeviceResponse(
            ToDeviceResponse(approved),
            deviceCredential));
    }

    private static async Task<IResult> SuspendDeviceAsync(
        Guid deviceId,
        ChangeLocalServerDeviceStatusRequest request,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var deviceResult = await GetDeviceForManagerActionAsync(
            deviceId,
            configurationStore,
            pairingStore,
            cancellationToken);

        if (deviceResult.Failure is not null)
        {
            return deviceResult.Failure;
        }

        var record = deviceResult.Record!;

        if (!string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Approved, StringComparison.Ordinal))
        {
            return ToFailureResult(
                "DeviceStatusInvalid",
                "Only approved devices can be suspended.");
        }

        var actor = NormalizeRequired(request.Actor, 160);

        if (string.IsNullOrWhiteSpace(actor))
        {
            return ToFailureResult(
                "ActorRequired",
                "An actor is required before suspending a device.");
        }

        var suspended = record.Suspend(
            actor,
            NormalizeRequired(request.Reason, 500) ?? "Device suspended by local manager.",
            DateTimeOffset.UtcNow);

        await pairingStore.SaveAsync(suspended, cancellationToken);

        return Results.Ok(ToDeviceResponse(suspended));
    }

    private static async Task<IResult> RevokeDeviceAsync(
        Guid deviceId,
        ChangeLocalServerDeviceStatusRequest request,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var deviceResult = await GetDeviceForManagerActionAsync(
            deviceId,
            configurationStore,
            pairingStore,
            cancellationToken);

        if (deviceResult.Failure is not null)
        {
            return deviceResult.Failure;
        }

        var record = deviceResult.Record!;

        if (string.Equals(record.DeviceStatus, LocalServerDevicePairingRecordStatuses.Revoked, StringComparison.Ordinal))
        {
            return Results.Ok(ToDeviceResponse(record));
        }

        var actor = NormalizeRequired(request.Actor, 160);

        if (string.IsNullOrWhiteSpace(actor))
        {
            return ToFailureResult(
                "ActorRequired",
                "An actor is required before revoking a device.");
        }

        var revoked = record.Revoke(
            actor,
            NormalizeRequired(request.Reason, 500) ?? "Device revoked by local manager.",
            DateTimeOffset.UtcNow);

        await pairingStore.SaveAsync(revoked, cancellationToken);

        return Results.Ok(ToDeviceResponse(revoked));
    }

    private static async Task<IResult> GetBootstrapStatusAsync(
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerEntitlementCache entitlementCache,
        ILocalServerEntitlementTrustStateStore trustStateStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);
        var entitlement = await entitlementCache.GetCurrentAsync(cancellationToken);
        LocalServerEntitlementTrustState? trustState = null;

        if (configuration is not null)
        {
            trustState = await trustStateStore.GetAsync(
                configuration.InstallationId,
                cancellationToken);
        }

        return Results.Ok(ToStatusResponse(configuration, entitlement, trustState));
    }

    private static async Task<IResult> ImportBootstrapPackageAsync(
        LocalServerSignedBootstrapBundleResponse request,
        string? expectedInstallationId,
        RegisterInstallationFromBootstrapBundleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterInstallationFromBootstrapBundleCommand(
                request,
                expectedInstallationId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new LocalServerBootstrapImportResponse(
                result.BootstrapConfiguration!.ClientId,
                result.BootstrapConfiguration.InstallationId,
                result.BootstrapConfiguration.RegistrationStatus,
                result.BootstrapConfiguration.DeploymentProfile,
                result.Registration!.InstallationStatus,
                result.Registration.RegisteredAtUtc,
                result.BootstrapConfiguration.SignatureKeyId,
                result.BootstrapConfiguration.PayloadSha256))
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> PullEntitlementAsync(
        PullEntitlementFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PullEntitlementFromBootstrapConfigurationCommand(),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.PulledAtUtc,
                result.Entitlement!.ClientId,
                result.Entitlement.InstallationId,
                result.Entitlement.EntitlementVersion,
                result.Entitlement.PaidUntil,
                result.Entitlement.OfflineValidUntil
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ReportHeartbeatAsync(
        ReportHeartbeatFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReportHeartbeatFromBootstrapConfigurationCommand(
                Detail: "Manual local-server heartbeat."),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.Heartbeat!.ClientId,
                result.Heartbeat.InstallationId,
                result.Heartbeat.HeartbeatStatus,
                result.Heartbeat.LicenseStatus,
                result.Heartbeat.EntitlementVersion,
                result.Heartbeat.ReceivedAtUtc,
                result.EntitlementState
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ProcessCommandsAsync(
        ProcessInstallationCommandsFromBootstrapConfigurationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ProcessInstallationCommandsFromBootstrapConfigurationCommand(),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new
            {
                result.PendingCommandCount,
                result.AppliedCount,
                result.FailedCount,
                result.RejectedCount,
                result.Commands
            })
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetAppActivationRevocationStatusAsync(
        HttpRequest httpRequest,
        LocalServerAppActivationRevocationStatusRequest request,
        LocalServerRuntimeAccessOptions runtimeAccess,
        GetAppActivationRevocationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var authorizationFailure = AuthorizeLocalApiAccess(httpRequest, runtimeAccess);
        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        var result = await handler.HandleAsync(
            new GetAppActivationRevocationStatusQuery(
                request.ClientId,
                request.InstallationId,
                request.AppServerInstallationId,
                request.ActivationIssueId,
                request.FingerprintHash,
                request.ServerPublicKeySha256,
                request.RequestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Status!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static IResult? AuthorizeLocalApiAccess(
        HttpRequest request,
        LocalServerRuntimeAccessOptions runtimeAccess)
    {
        var expectedSecret = runtimeAccess.SharedSecret.Trim();

        if (string.IsNullOrWhiteSpace(expectedSecret))
        {
            return Results.Json(
                new
                {
                    code = "LocalApiAccessNotConfigured",
                    detail = "Local API access is not configured for app runtime revocation checks."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var providedSecret = request.Headers[LocalServerRuntimeAccessOptions.AccessKeyHeaderName].ToString().Trim();

        if (string.IsNullOrWhiteSpace(providedSecret)
            || !FixedTimeEquals(providedSecret, expectedSecret))
        {
            return Results.Json(
                new
                {
                    code = "LocalApiAccessDenied",
                    detail = "Local API access is required before checking app activation revocation status."
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return null;
    }

    private static async Task<IResult> EvaluateModuleAccessAsync(
        LocalServerModuleAccessRequest request,
        EvaluateModuleAccessGatewayHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new EvaluateModuleAccessGatewayCommand(
                request.InstallationId,
                request.ModuleCode,
                request.AsOfDate,
                request.RequestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Access!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> EvaluateModuleAccessFromBootstrapAsync(
        string moduleCode,
        string? asOfDate,
        string? requestedBy,
        ILocalServerBootstrapConfigurationStore configurationStore,
        EvaluateModuleAccessGatewayHandler handler,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return ToFailureResult(
                "BootstrapConfigurationMissing",
                "A verified bootstrap configuration is required before evaluating module access.");
        }

        if (!TryParseDateOnly(asOfDate, out var parsedDate, out var failure))
        {
            return ToFailureResult(
                "AsOfDateInvalid",
                failure ?? "asOfDate must use yyyy-MM-dd format.");
        }

        var result = await handler.HandleAsync(
            new EvaluateModuleAccessGatewayCommand(
                configuration.InstallationId,
                moduleCode,
                parsedDate,
                requestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Access!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static LocalServerRuntimeStatusResponse ToStatusResponse(
        LocalServerBootstrapConfiguration? configuration,
        LocalServerCachedEntitlement? entitlement,
        LocalServerEntitlementTrustState? trustState)
    {
        return new LocalServerRuntimeStatusResponse(
            HasBootstrapConfiguration: configuration is not null,
            ClientId: configuration?.ClientId,
            InstallationId: configuration?.InstallationId,
            RegistrationStatus: configuration?.RegistrationStatus,
            DeploymentProfile: configuration?.DeploymentProfile,
            CloudBaseUrl: configuration?.CloudBaseUrl,
            LocalServerVersion: configuration?.LocalServerVersion,
            SetupTokenExpiresAtUtc: configuration?.SetupTokenExpiresAtUtc,
            LastRegistrationAttemptUtc: configuration?.LastRegistrationAttemptUtc,
            LastRegistrationSucceededAtUtc: configuration?.LastRegistrationSucceededAtUtc,
            LastRegistrationFailureCode: configuration?.LastRegistrationFailureCode,
            HasCachedEntitlement: entitlement is not null,
            EntitlementVersion: entitlement?.EntitlementVersion,
            PaidUntil: entitlement?.PaidUntil,
            OfflineValidUntil: entitlement?.OfflineValidUntil,
            LastSuccessfulCloudTimeUtc: trustState?.LastSuccessfulCloudTimeUtc,
            LastLocalCheckAtUtc: trustState?.LastLocalCheckAtUtc,
            ClockMovedBackwards: trustState?.ClockMovedBackwards ?? false);
    }

    private static async Task<DeviceManagerLookup> GetDeviceForManagerActionAsync(
        Guid deviceId,
        ILocalServerBootstrapConfigurationStore configurationStore,
        ILocalServerDevicePairingStore pairingStore,
        CancellationToken cancellationToken)
    {
        var configuration = await configurationStore.GetCurrentAsync(cancellationToken);

        if (configuration is null)
        {
            return new DeviceManagerLookup(
                null,
                ToFailureResult(
                    "BootstrapConfigurationMissing",
                    "A verified bootstrap configuration is required before managing paired devices."));
        }

        var record = await pairingStore.GetByDeviceIdAsync(deviceId, cancellationToken);

        if (record is null)
        {
            return new DeviceManagerLookup(
                null,
                NotFound("DeviceNotFound", "Device was not found."));
        }

        if (!string.Equals(record.InstallationId, configuration.InstallationId, StringComparison.Ordinal))
        {
            return new DeviceManagerLookup(
                null,
                ToFailureResult(
                    "InstallationMismatch",
                    "Device belongs to another LocalServer installation."));
        }

        return new DeviceManagerLookup(record, null);
    }

    private static LocalServerDevicePairingRequestResponse ToPairingRequestResponse(
        LocalServerDevicePairingRecord record)
    {
        return new LocalServerDevicePairingRequestResponse(
            record.PairingRequestId,
            record.DeviceId,
            record.ClientId,
            record.InstallationId,
            record.PairingRequestStatus,
            record.DeviceStatus,
            record.DeviceDisplayName,
            record.RequestedAtUtc,
            record.ExpiresAtUtc);
    }

    private static LocalServerDeviceResponse ToDeviceResponse(
        LocalServerDevicePairingRecord record)
    {
        return new LocalServerDeviceResponse(
            record.PairingRequestId,
            record.DeviceId,
            record.ClientId,
            record.InstallationId,
            record.PairingRequestStatus,
            record.DeviceStatus,
            record.DeviceDisplayName,
            record.DeviceFingerprintHash,
            record.WindowsUserHint,
            record.AppVersion,
            record.DevicePublicKeySha256,
            record.AssignedRole,
            record.RequestedAtUtc,
            record.ExpiresAtUtc,
            record.UpdatedAtUtc,
            record.ApprovedAtUtc,
            record.ApprovedBy,
            record.DeviceCredentialId,
            record.DeviceCredentialIssuedAtUtc,
            record.SuspendedAtUtc,
            record.SuspendedBy,
            record.SuspensionReason,
            record.RevokedAtUtc,
            record.RevokedBy,
            record.RevocationReason);
    }

    private static LocalServerDeploymentProfileResponse ToDeploymentProfileResponse(
        LocalServerBootstrapDeploymentProfile profile)
    {
        return new LocalServerDeploymentProfileResponse(
            profile.BootstrapMode,
            profile.ClientDeploymentMode,
            profile.SiteId,
            profile.SiteRole,
            profile.ParentSiteId,
            profile.BranchCode,
            profile.SyncTopologyId);
    }

    private static string ResolvePairingDisplayName(
        LocalServerPairingOptions pairingOptions,
        LocalServerBootstrapConfiguration? configuration)
    {
        if (!string.IsNullOrWhiteSpace(pairingOptions.DisplayName))
        {
            return pairingOptions.DisplayName.Trim();
        }

        if (configuration is null)
        {
            return "SafarSuite LocalServer";
        }

        var branchOrSite = !string.IsNullOrWhiteSpace(configuration.DeploymentProfile.BranchCode)
            ? configuration.DeploymentProfile.BranchCode
            : configuration.DeploymentProfile.SiteId;

        return string.IsNullOrWhiteSpace(branchOrSite)
            ? $"SafarSuite - {configuration.InstallationId}"
            : $"SafarSuite - {branchOrSite}";
    }

    private static string ResolvePairingMode(
        LocalServerPairingOptions pairingOptions)
    {
        return string.IsNullOrWhiteSpace(pairingOptions.PairingMode)
            ? LocalServerPairingModes.ManagerApproval
            : pairingOptions.PairingMode.Trim();
    }

    private static IReadOnlyCollection<string> BuildUrlCandidates(
        HttpRequest request,
        LocalServerPairingOptions pairingOptions)
    {
        var candidates = new List<string>();

        AddCandidate(candidates, pairingOptions.HttpsUrl);

        if (request.Host.HasValue)
        {
            AddCandidate(
                candidates,
                $"{request.Scheme}://{request.Host.Value}");
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddCandidate(
        ICollection<string> candidates,
        string? value)
    {
        var normalized = value?.Trim().TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            candidates.Add(normalized);
        }
    }

    private static string? ToOptional(string? value)
    {
        var normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static string? NormalizeRequired(
        string? value,
        int maxLength)
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

    private static string? NormalizeOptional(
        string? value,
        int maxLength)
    {
        return NormalizeRequired(value, maxLength);
    }

    private static string GenerateDeviceCredential()
    {
        return $"safarsuite-device.{Base64UrlEncode(RandomNumberGenerator.GetBytes(32))}";
    }

    private static string BuildFirstManagerActor(
        LocalServerFirstManagerSetupTokenPayloadResponse payload)
    {
        var manager = NormalizeRequired(payload.ManagerEmail, 160)
            ?? NormalizeRequired(payload.ManagerDisplayName, 160)
            ?? "first-manager";

        return $"first-manager:{manager}";
    }

    private static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static IResult NotFound(
        string failureCode,
        string detail)
    {
        return Results.NotFound(new LocalServerFailureResponse(failureCode, detail));
    }

    private static IResult ToFailureResult(
        string? failureCode,
        string? detail)
    {
        var code = string.IsNullOrWhiteSpace(failureCode)
            ? "LocalServerRequestFailed"
            : failureCode;
        var response = new LocalServerFailureResponse(
            code,
            string.IsNullOrWhiteSpace(detail)
                ? "Local server request failed."
                : detail);

        return code switch
        {
            "BootstrapConfigurationMissing" => Results.Conflict(response),
            "ControlCloudUnavailable" => Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable),
            "ControlCloudTimeout" => Results.Json(response, statusCode: StatusCodes.Status504GatewayTimeout),
            "ControlCloudRegistrationFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudPullFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudHeartbeatFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandPullFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandAcknowledgementFailed" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "ControlCloudCommandResponseInvalid" => Results.Json(response, statusCode: StatusCodes.Status502BadGateway),
            "SetupTokenNotFound" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "CommandNotFound" => Results.NotFound(response),
            "SignatureInvalid" => Results.BadRequest(response),
            "SigningKeyUnknown" => Results.BadRequest(response),
            "PayloadHashMismatch" => Results.BadRequest(response),
            "InstallationMismatch" => Results.Conflict(response),
            "CommandClientMismatch" => Results.Conflict(response),
            "CommandInstallationMismatch" => Results.Conflict(response),
            "CommandStatusInvalid" => Results.Conflict(response),
            "FirstManagerSetupTokenAlreadyConsumed" => Results.Conflict(response),
            "AppActivationRevocationClientMismatch" => Results.Conflict(response),
            "AppActivationRevocationInstallationMismatch" => Results.Conflict(response),
            _ when code.EndsWith("Required", StringComparison.Ordinal) => Results.BadRequest(response),
            _ when code.EndsWith("Invalid", StringComparison.Ordinal) => Results.BadRequest(response),
            _ when code.EndsWith("Unsupported", StringComparison.Ordinal) => Results.BadRequest(response),
            _ => Results.BadRequest(response)
        };
    }

    private static bool TryParseDateOnly(
        string? value,
        out DateOnly? date,
        out string? failure)
    {
        date = null;
        failure = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (DateOnly.TryParse(value.Trim(), out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        failure = "asOfDate must use yyyy-MM-dd format.";
        return false;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record DeviceManagerLookup(
        LocalServerDevicePairingRecord? Record,
        IResult? Failure);
}

public sealed record LocalServerRuntimeStatusResponse(
    bool HasBootstrapConfiguration,
    Guid? ClientId,
    string? InstallationId,
    string? RegistrationStatus,
    LocalServerBootstrapDeploymentProfile? DeploymentProfile,
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

public sealed record LocalServerBootstrapImportResponse(
    Guid ClientId,
    string InstallationId,
    string BootstrapRegistrationStatus,
    LocalServerBootstrapDeploymentProfile DeploymentProfile,
    string CloudRegistrationStatus,
    DateTimeOffset RegisteredAtUtc,
    string SignatureKeyId,
    string PayloadSha256);

public sealed record LocalServerFailureResponse(
    string FailureCode,
    string Detail);
