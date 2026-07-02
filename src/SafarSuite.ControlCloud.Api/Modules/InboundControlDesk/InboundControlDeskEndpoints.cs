using SafarSuite.ControlCloud.Application.Modules.InboundControlDesk;
using SafarSuite.ControlCloud.Domain.Modules.InboundControlDesk;
using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlCloud.Api.Modules.InboundControlDesk;

public static class InboundControlDeskEndpoints
{
    private const string CloudMessageIdHeader = "X-SafarSuite-Cloud-Message-Id";

    public static IEndpointRouteBuilder MapInboundControlDeskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/control-desk")
            .WithTags("Control Desk Receiver");

        group.MapPost("/messages", ReceiveEnvelopeAsync)
            .WithName("ReceiveControlDeskEnvelope");

        return endpoints;
    }

    private static async Task<IResult> ReceiveEnvelopeAsync(
        ControlCloudEnvelope envelope,
        ReceiveControlDeskEnvelopeHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReceiveControlDeskEnvelopeCommand(envelope),
            cancellationToken);
        var response = ToResponse(result);

        httpContext.Response.Headers[CloudMessageIdHeader] = result.CloudReference;

        if (result.IsSuccess)
        {
            return Results.Ok(response);
        }

        return IsAuthenticationFailure(result.RejectionCode)
            ? Results.Json(response, statusCode: StatusCodes.Status401Unauthorized)
            : Results.BadRequest(response);
    }

    private static ControlCloudReceiveEnvelopeResponse ToResponse(ReceiveControlDeskEnvelopeResult result)
    {
        return new ControlCloudReceiveEnvelopeResponse(
            result.ReceiptId,
            result.MessageId,
            result.MessageType,
            result.SubjectType,
            result.SubjectId,
            result.IdempotencyKey,
            result.CloudReference,
            result.Status.ToString(),
            result.Detail);
    }

    private static bool IsAuthenticationFailure(string? rejectionCode)
    {
        return rejectionCode is "SigningKeyUnknown" or "SignatureInvalid";
    }
}
