using SafarSuite.ControlCloud.Api.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaimProof;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalPaymentClaims;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.ProviderAccess;

public static class ProviderPaymentClaimEndpoints
{
    public static IEndpointRouteBuilder MapProviderPaymentClaimEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/provider-access/payment-claims")
            .WithTags("Provider Payment Claims");

        group.MapGet("", ListAsync).WithName("ListProviderPaymentClaims");
        group.MapGet("/{claimId:guid}", GetAsync).WithName("GetProviderPaymentClaim");
        group.MapGet("/{claimId:guid}/proof", GetProofAsync).WithName("GetProviderPaymentClaimProof");

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid? clientId,
        HttpRequest request,
        ProviderAccessSessionService sessions,
        ListClientPortalPaymentClaimsHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(request, ProviderAccessScopes.ClientPortalManage);
        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var result = await handler.HandleAsync(clientId, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new ClientPortalPaymentClaimListResponse(
                result.Value!.Select(ClientPortalPaymentEndpoints.ToClaimResponse).ToArray()))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetAsync(
        Guid claimId,
        HttpRequest request,
        ProviderAccessSessionService sessions,
        GetClientPortalPaymentClaimHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(request, ProviderAccessScopes.ClientPortalManage);
        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var result = await handler.HandleAsync(claimId, requiredClientId: null, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(ClientPortalPaymentEndpoints.ToClaimResponse(result.Value!))
            : ToFailure(result.FailureCode, result.Detail);
    }

    private static async Task<IResult> GetProofAsync(
        Guid claimId,
        HttpRequest request,
        HttpResponse response,
        ProviderAccessSessionService sessions,
        GetClientPortalPaymentClaimProofHandler handler,
        CancellationToken cancellationToken)
    {
        var authorization = sessions.Authorize(request, ProviderAccessScopes.ClientPortalManage);
        if (!authorization.IsSuccess)
        {
            return ToAuthorizationFailure(authorization);
        }

        var result = await handler.HandleAsync(claimId, requiredClientId: null, cancellationToken);
        if (!result.IsSuccess)
        {
            return ToFailure(result.FailureCode, result.Detail);
        }

        response.Headers["X-Content-Type-Options"] = "nosniff";
        var attachment = result.Value!;
        return Results.File(
            attachment.Content,
            attachment.ContentType,
            attachment.FileName,
            enableRangeProcessing: false);
    }

    private static IResult ToAuthorizationFailure(ProviderAccessAuthorizationResult authorization) =>
        Results.Json(
            new { code = authorization.FailureCode, detail = authorization.Detail },
            statusCode: authorization.StatusCode);

    private static IResult ToFailure(string? code, string? detail)
    {
        var response = new { code, detail };
        return code switch
        {
            "PaymentClaimNotFound" or "PaymentProofNotFound" => Results.NotFound(response),
            "PaymentClaimConflict" => Results.Conflict(response),
            _ => Results.BadRequest(response)
        };
    }
}
