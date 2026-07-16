using SafarSuite.ControlDesk.Api.Common;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.GetLatestEntitlementSnapshot;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoice;
using SafarSuite.ControlDesk.Application.Modules.Entitlements.IssueEntitlementSnapshotFromPaidInvoiceDefaults;
using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Entitlements;

namespace SafarSuite.ControlDesk.Api.Modules.Entitlements;

public static class EntitlementEndpoints
{
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/entitlements")
            .WithTags("Entitlements");

        group.MapPost("/snapshots/from-paid-invoice", IssueFromPaidInvoiceAsync);
        group.MapPost("/snapshots/from-paid-invoice/defaults", IssueFromPaidInvoiceDefaultsAsync);
        group.MapGet("/clients/{clientId:guid}/latest-snapshot", GetLatestForClientAsync);

        return endpoints;
    }

    private static async Task<IResult> IssueFromPaidInvoiceAsync(
        IssueEntitlementSnapshotFromPaidInvoiceRequest request,
        IssueEntitlementSnapshotFromPaidInvoiceHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new IssueEntitlementSnapshotFromPaidInvoiceCommand(
            request.InvoiceId,
            request.PaidUntil,
            request.GraceUntil,
            request.OfflineValidUntil,
            request.AllowedDevices,
            request.AllowedBranches,
            ResolveActor(httpContext),
            request.ApprovalReason,
            request.Modules.Select(module => new IssueEntitlementSnapshotModuleCommand(
                module.ModuleCode,
                module.IsEnabled)).ToArray(),
            request.AllowedNamedUsers,
            request.AllowedConcurrentUsers,
            (request.FeatureLimits ?? []).Select(limit => new IssueEntitlementSnapshotFeatureLimitCommand(
                limit.ModuleCode,
                limit.FeatureCode,
                limit.LimitValue,
                limit.Unit)).ToArray(),
            request.EffectiveFromUtc);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueEntitlementSnapshotFromPaidInvoiceResponse(
            result.Value.EntitlementSnapshotId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.ContractRevisionNumber,
            result.Value.ProductCatalogRevisionId,
            result.Value.ProductCatalogRevisionNumber,
            result.Value.ClientAccessRevisionId,
            result.Value.EntitlementVersion,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.EffectiveFromUtc,
            result.Value.SupersedesClientAccessRevisionId,
            result.Value.ApprovedBy,
            result.Value.ApprovalReason,
            result.Value.ApprovedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray(),
            result.Value.AllowedNamedUsers,
            result.Value.AllowedConcurrentUsers,
            (result.Value.FeatureLimits ?? []).Select(limit => new EntitlementFeatureLimitResponse(
                limit.ModuleCode,
                limit.FeatureCode,
                limit.LimitValue,
                limit.Unit)).ToArray());

        return Results.Created($"/api/v1/entitlements/snapshots/{response.EntitlementSnapshotId}", response);
    }

    private static async Task<IResult> IssueFromPaidInvoiceDefaultsAsync(
        IssueEntitlementSnapshotFromPaidInvoiceDefaultsRequest request,
        IssueEntitlementSnapshotFromPaidInvoiceDefaultsHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new IssueEntitlementSnapshotFromPaidInvoiceDefaultsCommand(
                request.InvoiceId,
                ResolveActor(httpContext),
                request.ApprovalReason,
                request.EffectiveFromUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueEntitlementSnapshotFromPaidInvoiceResponse(
            result.Value.EntitlementSnapshotId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.ContractRevisionNumber,
            result.Value.ProductCatalogRevisionId,
            result.Value.ProductCatalogRevisionNumber,
            result.Value.ClientAccessRevisionId,
            result.Value.EntitlementVersion,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.EffectiveFromUtc,
            result.Value.SupersedesClientAccessRevisionId,
            result.Value.ApprovedBy,
            result.Value.ApprovalReason,
            result.Value.ApprovedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray(),
            result.Value.AllowedNamedUsers,
            result.Value.AllowedConcurrentUsers,
            (result.Value.FeatureLimits ?? []).Select(limit => new EntitlementFeatureLimitResponse(
                limit.ModuleCode,
                limit.FeatureCode,
                limit.LimitValue,
                limit.Unit)).ToArray());

        return Results.Created($"/api/v1/entitlements/snapshots/{response.EntitlementSnapshotId}", response);
    }

    private static async Task<IResult> GetLatestForClientAsync(
        Guid clientId,
        GetLatestEntitlementSnapshotHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLatestEntitlementSnapshotQuery(clientId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new EntitlementSnapshotResponse(
            result.Value.EntitlementSnapshotId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.ContractRevisionNumber,
            result.Value.ProductCatalogRevisionId,
            result.Value.ProductCatalogRevisionNumber,
            result.Value.ClientAccessRevisionId,
            result.Value.EntitlementVersion,
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.EffectiveFromUtc,
            result.Value.SupersedesClientAccessRevisionId,
            result.Value.ApprovedBy,
            result.Value.ApprovalReason,
            result.Value.ApprovedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray(),
            result.Value.AllowedNamedUsers,
            result.Value.AllowedConcurrentUsers,
            (result.Value.FeatureLimits ?? []).Select(limit => new EntitlementFeatureLimitResponse(
                limit.ModuleCode,
                limit.FeatureCode,
                limit.LimitValue,
                limit.Unit)).ToArray());

        return Results.Ok(response);
    }

    private static string ResolveActor(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true
            && !string.IsNullOrWhiteSpace(httpContext.User.Identity.Name))
        {
            return httpContext.User.Identity.Name.Trim();
        }

        if (httpContext.Request.Headers.TryGetValue("X-Safar-Actor", out var actor))
        {
            var value = actor.FirstOrDefault()?.Trim();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "Control Desk operator";
    }
}
