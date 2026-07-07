using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ChangeProviderAccessOperatorPassword;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationBootstrapPackage;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateCloudInstallationSetupToken;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperator;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.CreateProviderAccessOperatorSession;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationDiagnostics;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.GetCloudInstallationStatus;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.IssueCloudAppActivationToken;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudAppActivationIssues;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudInstallationAuditEvents;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ListProviderAccessOperators;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.PublishPendingCloudOutboxMessages;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.QueueCloudInstallationSupportCommand;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.ResetProviderAccessOperatorPassword;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.RevokeCloudAppActivationIssue;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorScopes;
using SafarSuite.ControlDesk.Application.Modules.ControlCloud.UpdateProviderAccessOperatorStatus;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.ControlCloud;

namespace SafarSuite.ControlDesk.Api.Modules.ControlCloud;

public static class ControlCloudEndpoints
{
    public static IEndpointRouteBuilder MapControlCloudEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/control-cloud")
            .WithTags("Control Cloud");

        group.MapGet("/outbox-messages", ListOutboxMessagesAsync);
        group.MapGet(
            "/provider-access/operators",
            ListProviderAccessOperatorsAsync);
        group.MapPost(
            "/provider-access/operator-sessions",
            CreateProviderAccessOperatorSessionAsync);
        group.MapPost(
            "/provider-access/operator-password",
            ChangeProviderAccessOperatorPasswordAsync);
        group.MapPost(
            "/provider-access/operators",
            CreateProviderAccessOperatorAsync);
        group.MapPost(
            "/provider-access/operators/{userId}/password",
            ResetProviderAccessOperatorPasswordAsync);
        group.MapPost(
            "/provider-access/operators/{userId}/scopes",
            UpdateProviderAccessOperatorScopesAsync);
        group.MapPost(
            "/provider-access/operators/{userId}/status",
            UpdateProviderAccessOperatorStatusAsync);
        group.MapGet(
            "/clients/{clientId:guid}/installations/{installationId}/status",
            GetInstallationStatusAsync);
        group.MapPost(
            "/clients/{clientId:guid}/installations/{installationId}/setup-token",
            CreateSetupTokenAsync);
        group.MapPost(
            "/clients/{clientId:guid}/installations/{installationId}/bootstrap-package",
            CreateBootstrapPackageAsync);
        group.MapGet(
            "/clients/{clientId:guid}/installations/{installationId}/audit-events",
            ListInstallationAuditEventsAsync);
        group.MapGet(
            "/clients/{clientId:guid}/installations/{installationId}/diagnostics/latest",
            GetLatestDiagnosticsAsync);
        group.MapGet(
            "/clients/{clientId:guid}/app-activation-issues",
            ListAppActivationIssuesAsync);
        group.MapPost(
            "/clients/{clientId:guid}/app-activation-issues/{activationIssueId:guid}/revoke",
            RevokeAppActivationIssueAsync);
        group.MapPost(
            "/clients/{clientId:guid}/installations/{installationId}/support-command",
            QueueSupportCommandAsync);
        group.MapPost(
            "/clients/{clientId:guid}/installations/{installationId}/app-activation-token",
            IssueAppActivationTokenAsync);
        group.MapPost("/outbox-messages/publish", PublishOutboxMessagesAsync);
        group.MapPost("/outbox-messages/publish-local", PublishOutboxMessagesAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateProviderAccessOperatorSessionAsync(
        CreateProviderOperatorSessionRequest request,
        CreateProviderAccessOperatorSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateProviderAccessOperatorSessionCommand(
                request.Email,
                request.Password,
                request.Scopes,
                request.ExpiresInMinutes),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListProviderAccessOperatorsAsync(
        ListProviderAccessOperatorsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ChangeProviderAccessOperatorPasswordAsync(
        ChangeProviderOperatorPasswordRequest request,
        ChangeProviderAccessOperatorPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeProviderAccessOperatorPasswordCommand(
                request.Email,
                request.CurrentPassword,
                request.NewPassword),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateProviderAccessOperatorAsync(
        CreateProviderOperatorRequest request,
        CreateProviderAccessOperatorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateProviderAccessOperatorCommand(
                request.Email,
                request.FullName,
                request.Password,
                request.Scopes,
                request.CreatedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Created(
                $"/api/v1/control-cloud/provider-access/operators/{Uri.EscapeDataString(result.Value.UserId)}",
                result.Value);
    }

    private static async Task<IResult> ResetProviderAccessOperatorPasswordAsync(
        string userId,
        ResetProviderOperatorPasswordRequest request,
        ResetProviderAccessOperatorPasswordHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ResetProviderAccessOperatorPasswordCommand(
                userId,
                request.Password,
                request.UpdatedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateProviderAccessOperatorScopesAsync(
        string userId,
        UpdateProviderOperatorScopesRequest request,
        UpdateProviderAccessOperatorScopesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateProviderAccessOperatorScopesCommand(
                userId,
                request.Scopes,
                request.UpdatedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateProviderAccessOperatorStatusAsync(
        string userId,
        UpdateProviderOperatorStatusRequest request,
        UpdateProviderAccessOperatorStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateProviderAccessOperatorStatusCommand(
                userId,
                request.Status,
                request.UpdatedBy),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetInstallationStatusAsync(
        Guid clientId,
        string installationId,
        GetCloudInstallationStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCloudInstallationStatusQuery(clientId, installationId),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateSetupTokenAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerSetupTokenRequest request,
        CreateCloudInstallationSetupTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateCloudInstallationSetupTokenCommand(
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

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> CreateBootstrapPackageAsync(
        Guid clientId,
        string installationId,
        CreateLocalServerBootstrapPackageRequest request,
        CreateCloudInstallationBootstrapPackageHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateCloudInstallationBootstrapPackageCommand(
                clientId,
                installationId,
                request.ExpiresInHours,
                request.CreatedBy,
                request.DeploymentMode,
                request.LocalServerVersion,
                request.SafarSuiteAppVersion,
                request.ClientDeploymentMode,
                request.SiteId,
                request.SiteRole,
                request.ParentSiteId,
                request.BranchCode,
                request.SyncTopologyId),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListInstallationAuditEventsAsync(
        Guid clientId,
        string installationId,
        int? take,
        ListCloudInstallationAuditEventsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListCloudInstallationAuditEventsQuery(
                clientId,
                installationId,
                take ?? 50),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetLatestDiagnosticsAsync(
        Guid clientId,
        string installationId,
        GetCloudInstallationDiagnosticsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCloudInstallationDiagnosticsQuery(
                clientId,
                installationId),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListAppActivationIssuesAsync(
        Guid clientId,
        string? installationId,
        Guid? appServerInstallationId,
        string? query,
        int? take,
        ListCloudAppActivationIssuesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListCloudAppActivationIssuesQuery(
                clientId,
                installationId,
                appServerInstallationId,
                query,
                take ?? 50),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(new SafarSuiteAppActivationIssuesResponse(result.Value.Issues));
    }

    private static async Task<IResult> RevokeAppActivationIssueAsync(
        Guid clientId,
        Guid activationIssueId,
        RevokeSafarSuiteAppActivationIssueRequest request,
        RevokeCloudAppActivationIssueHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RevokeCloudAppActivationIssueCommand(
                clientId,
                activationIssueId,
                request.RevokedBy,
                request.Reason),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> QueueSupportCommandAsync(
        Guid clientId,
        string installationId,
        QueueCloudInstallationSupportCommandRequest request,
        QueueCloudInstallationSupportCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new QueueCloudInstallationSupportCommandCommand(
                clientId,
                installationId,
                request.CommandType,
                request.Reason,
                request.RequestedBy,
                request.ExpiresInHours),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> IssueAppActivationTokenAsync(
        Guid clientId,
        string installationId,
        IssueSafarSuiteAppActivationTokenRequest request,
        IssueCloudAppActivationTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new IssueCloudAppActivationTokenCommand(
                clientId,
                installationId,
                request.ActivationRequestId,
                request.ServerInstallationId,
                request.FingerprintHash,
                request.ServerPublicKey,
                request.RequestedBy ?? "SafarSuite Control Desk",
                request.ReplacesActivationIssueId),
            cancellationToken);

        return result.IsFailure
            ? ApiResultMapper.ToErrorResult(result.Errors)
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListOutboxMessagesAsync(
        string? status,
        string? messageType,
        ListCloudOutboxMessagesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListCloudOutboxMessagesQuery(status, messageType),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new ListCloudOutboxMessagesResponse(
            result.Value.Messages.Select(message => new CloudOutboxMessageResponse(
                message.CloudOutboxMessageId,
                message.MessageType,
                message.SubjectType,
                message.SubjectId,
                message.PayloadJson,
                message.Status,
                message.AttemptCount,
                message.OccurredAtUtc,
                message.LastAttemptedAtUtc,
                message.NextAttemptAtUtc,
                message.SentAtUtc,
                message.FailedAtUtc,
                message.FailureReason)).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> PublishOutboxMessagesAsync(
        int? batchSize,
        PublishPendingCloudOutboxMessagesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PublishPendingCloudOutboxMessagesCommand(batchSize ?? 20),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new PublishCloudOutboxMessagesResponse(
            result.Value.RequestedBatchSize,
            result.Value.PublishedCount,
            result.Value.FailedCount,
            result.Value.Messages.Select(message => new PublishedCloudOutboxMessageResponse(
                message.CloudOutboxMessageId,
                message.MessageType,
                message.SubjectType,
                message.SubjectId,
                message.Status,
                message.AttemptCount,
                message.LastAttemptedAtUtc,
                message.NextAttemptAtUtc,
                message.SentAtUtc,
                message.FailedAtUtc,
                message.FailureReason,
                message.CloudReference,
                message.EnvelopeSignature)).ToArray());

        return Results.Ok(response);
    }
}
