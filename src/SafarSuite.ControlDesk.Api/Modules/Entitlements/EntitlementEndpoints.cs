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
        CancellationToken cancellationToken)
    {
        var command = new IssueEntitlementSnapshotFromPaidInvoiceCommand(
            request.InvoiceId,
            request.PaidUntil,
            request.GraceUntil,
            request.OfflineValidUntil,
            request.AllowedDevices,
            request.AllowedBranches,
            request.Modules.Select(module => new IssueEntitlementSnapshotModuleCommand(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueEntitlementSnapshotFromPaidInvoiceResponse(
            result.Value.EntitlementSnapshotId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        return Results.Created($"/api/v1/entitlements/snapshots/{response.EntitlementSnapshotId}", response);
    }

    private static async Task<IResult> IssueFromPaidInvoiceDefaultsAsync(
        IssueEntitlementSnapshotFromPaidInvoiceDefaultsRequest request,
        IssueEntitlementSnapshotFromPaidInvoiceDefaultsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new IssueEntitlementSnapshotFromPaidInvoiceDefaultsCommand(request.InvoiceId),
            cancellationToken);

        if (result.IsFailure)
        {
            return ApiResultMapper.ToErrorResult(result.Errors);
        }

        var response = new IssueEntitlementSnapshotFromPaidInvoiceResponse(
            result.Value.EntitlementSnapshotId,
            result.Value.ClientId,
            result.Value.ContractId,
            result.Value.InvoiceId,
            result.Value.InvoiceNumber,
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

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
            result.Value.Status,
            result.Value.PaidUntil,
            result.Value.GraceUntil,
            result.Value.OfflineValidUntil,
            result.Value.AllowedDevices,
            result.Value.AllowedBranches,
            result.Value.IssuedAtUtc,
            result.Value.Modules.Select(module => new EntitlementModuleResponse(
                module.ModuleCode,
                module.IsEnabled)).ToArray());

        return Results.Ok(response);
    }
}
