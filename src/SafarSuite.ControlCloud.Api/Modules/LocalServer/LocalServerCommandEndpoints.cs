using System.Text;
using System.Text.Json;
using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Api.Modules.ProviderAccess;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.AcknowledgeInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateInstallationSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.CreateLocalServerBootstrapPackage;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ExportOfflineRenewalFile;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetInstallationStatus;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetLatestInstallationDiagnostics;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.GetPendingInstallationCommands;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerPairingDescriptor;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueLocalServerFirstManagerSetupToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.IssueSafarSuiteAppActivationToken;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListLocalServerBootstrapPackages;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ListSafarSuiteAppActivationIssues;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.MarkLocalServerBootstrapPackageHandoff;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.Ports;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.QueueInstallationCommand;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReceiveInstallationDiagnostics;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.RegisterLocalServerInstallation;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.ReportInstallationHeartbeat;
using SafarSuite.ControlCloud.Application.Modules.LocalServer.RevokeSafarSuiteAppActivationIssue;
using SafarSuite.ControlCloud.Domain.Modules.LocalServer;
using SafarSuite.ControlCloud.Infrastructure.LocalServer;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.LocalServer;

public static class LocalServerCommandEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private static readonly JsonSerializerOptions BootstrapBundleJsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapLocalServerCommandEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var controlCloudGroup = endpoints
            .MapGroup("/api/v1/control-cloud")
            .WithTags("Control Cloud Commands");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/commands",
                QueueCommandAsync)
            .WithName("QueueInstallationCommand");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/status",
                GetInstallationStatusAsync)
            .WithName("GetControlCloudInstallationStatus");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/diagnostics/latest",
                GetLatestDiagnosticsAsync)
            .WithName("GetLatestInstallationDiagnostics");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/setup-token",
                CreateSetupTokenAsync)
            .WithName("CreateLocalServerSetupToken");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/bootstrap-package",
                CreateBootstrapPackageAsync)
            .WithName("CreateLocalServerBootstrapPackage");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/bootstrap-packages",
                ListBootstrapPackagesAsync)
            .WithName("ListLocalServerBootstrapPackages");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/bootstrap-packages/{bootstrapPackageId:guid}/handoff",
                MarkBootstrapPackageHandoffAsync)
            .WithName("MarkLocalServerBootstrapPackageHandoff");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/bootstrap-package/download",
                DownloadBootstrapPackageAsync)
            .WithName("DownloadLocalServerBootstrapPackage");

        controlCloudGroup.MapGet(
                "/app-activation/signing-key",
                GetAppActivationSigningKeyAsync)
            .WithName("GetSafarSuiteAppActivationSigningKey");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/app-activation-issues",
                ListAppActivationIssuesAsync)
            .WithName("ListSafarSuiteAppActivationIssues");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/app-activation-issues/{activationIssueId:guid}/revoke",
                RevokeAppActivationIssueAsync)
            .WithName("RevokeSafarSuiteAppActivationIssue");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/app-activation-token",
                IssueAppActivationTokenAsync)
            .WithName("IssueSafarSuiteAppActivationToken");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/first-manager-setup-token",
                IssueFirstManagerSetupTokenAsync)
            .WithName("IssueLocalServerFirstManagerSetupToken");

        controlCloudGroup.MapPost(
                "/clients/{clientId:guid}/installations/{installationId}/pairing-descriptor",
                IssuePairingDescriptorAsync)
            .WithName("IssueLocalServerPairingDescriptor");

        controlCloudGroup.MapGet(
                "/clients/{clientId:guid}/installations/{installationId}/offline-renewal-file",
                ExportOfflineRenewalFileAsync)
            .WithName("ExportOfflineRenewalFile");

        var localServerGroup = endpoints
            .MapGroup("/api/v1/local-server")
            .WithTags("Local Server Commands");

        localServerGroup.MapGet(
                "/installations/{installationId}/commands/pending",
                GetPendingCommandsAsync)
            .WithName("GetPendingInstallationCommands");

        localServerGroup.MapPost(
                "/installations/{installationId}/registration",
                RegisterInstallationAsync)
            .WithName("RegisterLocalServerInstallation");

        localServerGroup.MapPost(
                "/installations/{installationId}/commands/{commandId:guid}/acknowledgement",
                AcknowledgeCommandAsync)
            .WithName("AcknowledgeInstallationCommand");

        localServerGroup.MapPost(
                "/installations/{installationId}/heartbeat",
                ReportHeartbeatAsync)
            .WithName("ReportInstallationHeartbeat");

        localServerGroup.MapPost(
                "/installations/{installationId}/diagnostics",
                ReceiveDiagnosticsAsync)
            .WithName("ReceiveInstallationDiagnostics");

        localServerGroup.MapGet(
                "/installations/{installationId}/entitlement-bundle",
                GetEntitlementBundleAsync)
            .WithName("GetLocalServerEntitlementBundle");

        return endpoints;
    }

    private static async Task<IResult> GetInstallationStatusAsync(
        Guid clientId,
        string installationId,
        GetInstallationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetInstallationStatusQuery(
                clientId,
                installationId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Status!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetLatestDiagnosticsAsync(
        Guid clientId,
        string installationId,
        GetLatestInstallationDiagnosticsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLatestInstallationDiagnosticsQuery(
                clientId,
                installationId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        return Results.Ok(ToResponse(result.Report!));
    }

    private static async Task<IResult> CreateSetupTokenAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerSetupTokenRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        CreateInstallationSetupTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new CreateInstallationSetupTokenCommand(
                clientId,
                installationId,
                request.ExpiresInHours,
                request.CreatedBy,
                request.DeploymentMode,
                request.ClientDeploymentMode,
                request.SiteId,
                request.SiteRole,
                request.ParentSiteId,
                request.BranchCode,
                request.SyncTopologyId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        var setupToken = result.SetupToken!;

        return Results.Ok(new LocalServerSetupTokenResponse(
            setupToken.SetupTokenId,
            setupToken.ClientId,
            setupToken.InstallationId,
            result.PlainSetupToken!,
            setupToken.Status,
            setupToken.CreatedBy,
            setupToken.DeploymentMode,
            ToResponse(setupToken.DeploymentProfile),
            setupToken.CreatedAtUtc,
            setupToken.ExpiresAtUtc));
    }

    private static async Task<IResult> CreateBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        CreateLocalServerBootstrapPackageHandler handler,
        ControlCloudBootstrapPackageOptions options,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new CreateLocalServerBootstrapPackageCommand(
                clientId,
                installationId,
                request.ExpiresInHours,
                request.CreatedBy,
                request.DeploymentMode,
                request.LocalServerVersion,
                options.CloudBaseUrl,
                options.InstallScriptUrl,
                request.SafarSuiteAppVersion,
                request.ClientDeploymentMode,
                request.SiteId,
                request.SiteRole,
                request.ParentSiteId,
                request.BranchCode,
                request.SyncTopologyId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.BootstrapPackage!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ListBootstrapPackagesAsync(
        Guid clientId,
        string installationId,
        int? take,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        ListLocalServerBootstrapPackagesHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesRead);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new ListLocalServerBootstrapPackagesQuery(
                clientId,
                installationId,
                take ?? 50),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Response!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> MarkBootstrapPackageHandoffAsync(
        Guid clientId,
        string installationId,
        Guid bootstrapPackageId,
        MarkLocalServerBootstrapPackageHandoffRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        MarkLocalServerBootstrapPackageHandoffHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new MarkLocalServerBootstrapPackageHandoffCommand(
                clientId,
                installationId,
                bootstrapPackageId,
                request.Channel,
                request.Recipient,
                request.MarkedBy,
                request.Note),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Response!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static Task<IResult> GetAppActivationSigningKeyAsync(
        IControlCloudAppActivationTokenSigner signer)
    {
        return Task.FromResult<IResult>(Results.Ok(new SafarSuiteAppActivationSigningKeyResponse(
            signer.SigningKeyId,
            signer.PublicKeyPem)));
    }

    private static async Task<IResult> ListAppActivationIssuesAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int? take,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        ListSafarSuiteAppActivationIssuesHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.AppActivationRead);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new ListSafarSuiteAppActivationIssuesQuery(
                clientId,
                installationId,
                appServerInstallationId,
                query,
                take ?? 50),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Response!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> IssueAppActivationTokenAsync(
        Guid clientId,
        string installationId,
        IssueSafarSuiteAppActivationTokenRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        IssueSafarSuiteAppActivationTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.AppActivationWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new IssueSafarSuiteAppActivationTokenCommand(
                clientId,
                installationId,
                request.ActivationRequestId,
                request.ServerInstallationId,
                request.FingerprintHash,
                request.ServerPublicKey,
                request.RequestedBy,
                request.ReplacesActivationIssueId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Response!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> RevokeAppActivationIssueAsync(
        Guid clientId,
        Guid activationIssueId,
        RevokeSafarSuiteAppActivationIssueRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        RevokeSafarSuiteAppActivationIssueHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.AppActivationWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new RevokeSafarSuiteAppActivationIssueCommand(
                clientId,
                activationIssueId,
                request.RevokedBy,
                request.Reason),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Issue!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> IssueFirstManagerSetupTokenAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerFirstManagerSetupTokenRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        IssueLocalServerFirstManagerSetupTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new IssueLocalServerFirstManagerSetupTokenCommand(
                clientId,
                installationId,
                request.PendingDeviceRequestId,
                request.ManagerDisplayName,
                request.ManagerEmail,
                request.CreatedBy,
                request.ExpiresInHours,
                request.Purpose,
                request.RecoveryReason),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Response!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> IssuePairingDescriptorAsync(
        Guid clientId,
        string installationId,
        IssueLocalServerPairingDescriptorRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        IssueLocalServerPairingDescriptorHandler handler,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesRead);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new IssueLocalServerPairingDescriptorCommand(
                clientId,
                installationId,
                request.BootstrapPackageId,
                request.SetupTokenId,
                request.ClientCode,
                request.CustomerName,
                request.AppServerInstallationId,
                request.FingerprintHash,
                request.UrlCandidates,
                request.TlsCaSha256,
                request.TlsCertificateSha256,
                request.ServerPairingKeySha256,
                request.RequestedBy),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Descriptor!)
            : ToFailureResult(result.FailureCode, result.Detail);
    }

    private static IResult? AuthorizeProviderAccess(
        HttpRequest request,
        ProviderAccessSessionService providerAccess,
        string requiredScope)
    {
        var authorization = providerAccess.Authorize(request, requiredScope);

        return authorization.IsSuccess
            ? null
            : Results.Json(
                new { code = authorization.FailureCode, detail = authorization.Detail },
                statusCode: authorization.StatusCode);
    }

    private static async Task<IResult> DownloadBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        HttpRequest httpRequest,
        ProviderAccessSessionService providerAccess,
        CreateLocalServerBootstrapPackageHandler handler,
        ControlCloudBootstrapPackageOptions options,
        CancellationToken cancellationToken)
    {
        var providerAuthorizationFailure = AuthorizeProviderAccess(
            httpRequest,
            providerAccess,
            ProviderAccessScopes.DeploymentPackagesWrite);

        if (providerAuthorizationFailure is not null)
        {
            return providerAuthorizationFailure;
        }

        var result = await handler.HandleAsync(
            new CreateLocalServerBootstrapPackageCommand(
                clientId,
                installationId,
                request.ExpiresInHours,
                request.CreatedBy,
                request.DeploymentMode,
                request.LocalServerVersion,
                options.CloudBaseUrl,
                options.InstallScriptUrl,
                request.SafarSuiteAppVersion,
                request.ClientDeploymentMode,
                request.SiteId,
                request.SiteRole,
                request.ParentSiteId,
                request.BranchCode,
                request.SyncTopologyId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        var bootstrapPackage = result.BootstrapPackage!;
        var json = JsonSerializer.Serialize(bootstrapPackage.SignedBundle, BootstrapBundleJsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        return Results.File(
            bytes,
            bootstrapPackage.BundleContentType,
            bootstrapPackage.BundleFileName);
    }

    private static async Task<IResult> RegisterInstallationAsync(
        string installationId,
        RegisterLocalServerInstallationRequest request,
        RegisterLocalServerInstallationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterLocalServerInstallationCommand(
                request.ClientId,
                installationId,
                request.SetupToken,
                request.LocalServerVersion),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        var installation = result.Installation!;

        return Results.Ok(new LocalServerInstallationRegistrationResponse(
            installation.ClientId,
            installation.InstallationId,
            installation.Status,
            installation.RegisteredAtUtc,
            result.LocalServerVersion,
            ToResponse(installation.DeploymentProfile)));
    }

    private static async Task<IResult> ReceiveDiagnosticsAsync(
        string installationId,
        UploadLocalServerDiagnosticsRequest request,
        ReceiveInstallationDiagnosticsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReceiveInstallationDiagnosticsCommand(
                request.ClientId,
                installationId,
                request.UploadedBy,
                request.Reason,
                request.Bundle),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        var report = result.Report!;

        return Results.Ok(new LocalServerDiagnosticsUploadResponse(
            report.DiagnosticReportId,
            report.ClientId,
            report.InstallationId,
            report.Status,
            report.ReceivedAtUtc));
    }

    private static async Task<IResult> ExportOfflineRenewalFileAsync(
        Guid clientId,
        string installationId,
        string? generatedBy,
        string? reason,
        ExportOfflineRenewalFileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ExportOfflineRenewalFileQuery(
                clientId,
                installationId,
                generatedBy ?? "",
                reason ?? ""),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ToFailureResult(result.FailureCode, result.Detail);
        }

        var renewalFile = result.RenewalFile!;
        var json = JsonSerializer.Serialize(renewalFile, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        return Results.File(
            bytes,
            "application/json",
            BuildOfflineRenewalFileName(
                renewalFile.ClientId,
                renewalFile.InstallationId,
                renewalFile.SignedBundle.Payload.EntitlementVersion));
    }

    private static async Task<IResult> QueueCommandAsync(
        Guid clientId,
        string installationId,
        QueueInstallationCommandRequest request,
        QueueInstallationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new QueueInstallationCommandCommand(
                clientId,
                installationId,
                request.CommandType,
                ToPayloadJson(request.Payload),
                request.NotBeforeUtc,
                request.ExpiresAtUtc,
                request.IdempotencyKey),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(result.Command!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetPendingCommandsAsync(
        string installationId,
        GetPendingInstallationCommandsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetPendingInstallationCommandsQuery(installationId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(new PendingInstallationCommandsResponse(
                installationId.Trim(),
                result.Commands!
                    .OrderBy(command => command.CommandVersion)
                    .Select(ToResponse)
                    .ToArray()));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> AcknowledgeCommandAsync(
        string installationId,
        Guid commandId,
        AcknowledgeInstallationCommandRequest request,
        AcknowledgeInstallationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AcknowledgeInstallationCommandCommand(
                installationId,
                commandId,
                request.ResultStatus,
                request.Detail,
                ToPayloadJson(request.Payload)),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(result.Command!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> ReportHeartbeatAsync(
        string installationId,
        ReportLocalServerHeartbeatRequest request,
        ReportInstallationHeartbeatHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReportInstallationHeartbeatCommand(
                installationId,
                request.ClientId,
                request.LocalServerVersion,
                request.ReportedAtUtc,
                request.LicenseStatus,
                request.EntitlementVersion,
                request.PaidUntil,
                request.WarningStartsAt,
                request.GraceUntil,
                request.OfflineValidUntil,
                request.Detail,
                request.DeploymentProfile,
                request.PairingStatus),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ToResponse(
                result.Heartbeat!,
                result.DeploymentProfile!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetEntitlementBundleAsync(
        string installationId,
        Guid clientId,
        GetClientPortalSignedEntitlementBundleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetClientPortalSignedEntitlementBundleQuery(
                clientId,
                installationId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(ClientPortalEndpoints.ToResponse(result.Bundle!));
        }

        return ToFailureResult(result.FailureCode, result.Detail);
    }

    private static IResult ToFailureResult(string? failureCode, string? detail)
    {
        var response = new { code = failureCode, detail };

        return failureCode switch
        {
            "EntitlementNotFound" => Results.NotFound(response),
            "ActivationIssueNotFound" => Results.NotFound(response),
            "ActivationIssueReplacementNotFound" => Results.NotFound(response),
            "InstallationNotFound" => Results.NotFound(response),
            "InstallationNotRegistered" => Results.NotFound(response),
            "CommandNotFound" => Results.NotFound(response),
            "ClientNotFound" => Results.NotFound(response),
            "DiagnosticsNotFound" => Results.NotFound(response),
            "InstallationClientMismatch" => Results.Conflict(response),
            "ActivationIssueAlreadyRevoked" => Results.Conflict(response),
            "ActivationIssueClientMismatch" => Results.Conflict(response),
            "ActivationIssueReplacementClientMismatch" => Results.Conflict(response),
            "ActivationIssueReplacementInstallationMismatch" => Results.Conflict(response),
            "ActivationIssueReplacementNotRevoked" => Results.Conflict(response),
            "ActivationIssueRevocationCommandInvalid" => Results.Conflict(response),
            "DiagnosticsClientMismatch" => Results.Conflict(response),
            "DiagnosticsInstallationMismatch" => Results.Conflict(response),
            "DiagnosticsFormatUnsupported" => Results.Conflict(response),
            "EntitlementInstallationMismatch" => Results.Conflict(response),
            "EntitlementPayloadInvalid" => Results.Conflict(response),
            "EntitlementVersionRejected" => Results.Conflict(response),
            "InstallationCommandMismatch" => Results.Conflict(response),
            "SetupTokenScopeMismatch" => Results.Conflict(response),
            "SetupTokenNotUsable" => Results.Conflict(response),
            "SetupTokenNotFound" => Results.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            "AppServerInstallationIdRequired" => Results.BadRequest(response),
            "AppFingerprintRequired" => Results.BadRequest(response),
            "AppServerPublicKeyRequired" => Results.BadRequest(response),
            "ActivationIssueQueryInvalid" => Results.BadRequest(response),
            "ActivationIssueTakeInvalid" => Results.BadRequest(response),
            "BootstrapPackageTakeInvalid" => Results.BadRequest(response),
            "ActivationIssueIdRequired" => Results.BadRequest(response),
            "ActivationIssueRevokedByRequired" => Results.BadRequest(response),
            "ActivationIssueRevocationReasonRequired" => Results.BadRequest(response),
            "ActivationIssueReplacementIdInvalid" => Results.BadRequest(response),
            "PendingDeviceRequestIdRequired" => Results.BadRequest(response),
            "ManagerDisplayNameRequired" => Results.BadRequest(response),
            "FirstManagerSetupTokenInvalid" => Results.BadRequest(response),
            "CloudBaseUrlInvalid" => Results.BadRequest(response),
            "InstallScriptUrlInvalid" => Results.BadRequest(response),
            "BootstrapModeUnsupported" => Results.BadRequest(response),
            "ClientDeploymentModeUnsupported" => Results.BadRequest(response),
            "SiteRoleUnsupported" => Results.BadRequest(response),
            "InstallationIdRequired" => Results.BadRequest(response),
            "ClientIdRequired" => Results.BadRequest(response),
            _ => Results.BadRequest(response)
        };
    }

    private static string BuildOfflineRenewalFileName(
        Guid clientId,
        string installationId,
        long entitlementVersion)
    {
        var cleanInstallationId = new string(
            installationId
                .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_'
                    ? character
                    : '-')
                .ToArray());

        return $"safarsuite-renewal-{clientId:N}-{cleanInstallationId}-v{entitlementVersion}.json";
    }

    private static InstallationCommandResponse ToResponse(
        ControlCloudInstallationCommand command)
    {
        return new InstallationCommandResponse(
            command.CommandId,
            command.ClientId,
            command.InstallationId,
            command.CommandVersion,
            command.CommandType,
            command.Status,
            command.IdempotencyKey,
            ToPayload(command.PayloadJson),
            new InstallationCommandSignatureResponse(
                command.SignatureAlgorithm,
                command.SignatureKeyId,
                command.PayloadSha256,
                command.SignatureValue),
            command.QueuedAtUtc,
            command.NotBeforeUtc,
            command.ExpiresAtUtc,
            command.AcknowledgedAtUtc,
            command.AcknowledgementStatus,
            command.AcknowledgementDetail);
    }

    private static LocalServerHeartbeatResponse ToResponse(
        ControlCloudInstallationHeartbeat heartbeat,
        ControlCloudInstallationDeploymentProfile deploymentProfile)
    {
        return new LocalServerHeartbeatResponse(
            heartbeat.HeartbeatId,
            heartbeat.InstallationId,
            heartbeat.ClientId,
            heartbeat.HeartbeatStatus,
            heartbeat.ReceivedAtUtc,
            heartbeat.ReportedAtUtc,
            heartbeat.LicenseStatus,
            heartbeat.EntitlementVersion,
            heartbeat.PaidUntil,
            heartbeat.WarningStartsAt,
            heartbeat.GraceUntil,
            heartbeat.OfflineValidUntil,
            heartbeat.LocalServerVersion,
            heartbeat.Detail,
            ToResponse(deploymentProfile),
            ToResponse(heartbeat.PairingStatus));
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

    private static LocalServerPairingStatusResponse? ToResponse(
        ControlCloudInstallationPairingStatus? pairingStatus)
    {
        return pairingStatus is null
            ? null
            : new LocalServerPairingStatusResponse(
                pairingStatus.PairingMode,
                pairingStatus.TotalDeviceCount,
                pairingStatus.PendingDeviceCount,
                pairingStatus.ApprovedDeviceCount,
                pairingStatus.SuspendedDeviceCount,
                pairingStatus.RevokedDeviceCount,
                pairingStatus.FirstManagerDeviceApproved,
                pairingStatus.FirstManagerDeviceApprovedAtUtc,
                pairingStatus.LastDeviceUpdatedAtUtc);
    }

    private static LocalServerDiagnosticReportResponse ToResponse(
        ControlCloudInstallationDiagnosticReport report)
    {
        return new LocalServerDiagnosticReportResponse(
            report.DiagnosticReportId,
            report.ClientId,
            report.InstallationId,
            report.Status,
            report.ReceivedAtUtc,
            report.GeneratedAtUtc,
            report.UploadedBy,
            report.Reason,
            report.LocalServerVersion,
            report.LicenseStatus,
            JsonSerializer.Deserialize<LocalServerDiagnosticBundleResponse>(
                report.BundleJson,
                JsonOptions)!);
    }

    private static string ToPayloadJson(JsonElement payload)
    {
        return payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? "{}"
            : payload.GetRawText();
    }

    private static JsonElement ToPayload(string payloadJson)
    {
        using var document = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);

        return document.RootElement.Clone();
    }
}
