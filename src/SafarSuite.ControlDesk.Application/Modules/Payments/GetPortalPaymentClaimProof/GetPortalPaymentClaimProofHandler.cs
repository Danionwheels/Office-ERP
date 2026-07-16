using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.GetPortalPaymentClaimProof;

public sealed class GetPortalPaymentClaimProofHandler
{
    private readonly IPortalPaymentClaimRepository _claims;
    private readonly IControlCloudPaymentClaimClient _cloudClient;

    public GetPortalPaymentClaimProofHandler(
        IPortalPaymentClaimRepository claims,
        IControlCloudPaymentClaimClient cloudClient)
    {
        _claims = claims;
        _cloudClient = cloudClient;
    }

    public async Task<Result<GetPortalPaymentClaimProofResult>> HandleAsync(
        GetPortalPaymentClaimProofQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClaimId == Guid.Empty)
        {
            return Result<GetPortalPaymentClaimProofResult>.Failure(ApplicationError.Validation(
                nameof(query.ClaimId),
                "Claim id cannot be empty."));
        }

        var claim = await _claims.GetByIdAsync(
            PortalPaymentClaimId.Create(query.ClaimId),
            cancellationToken);

        if (claim is null)
        {
            return Result<GetPortalPaymentClaimProofResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClaimId),
                "Portal payment claim was not found."));
        }

        if (!claim.ProofAttachmentId.HasValue)
        {
            return Result<GetPortalPaymentClaimProofResult>.Failure(ApplicationError.NotFound(
                nameof(query.ClaimId),
                "This portal payment claim does not have a proof attachment."));
        }

        var cloudResult = await _cloudClient.GetProofAsync(query.ClaimId, cancellationToken);

        if (!cloudResult.IsSuccess)
        {
            return cloudResult.FailureCode?.Contains("NotFound", StringComparison.OrdinalIgnoreCase) == true
                ? Result<GetPortalPaymentClaimProofResult>.Failure(ApplicationError.NotFound(
                    nameof(query.ClaimId),
                    cloudResult.Detail ?? "Payment proof was not found in Control Cloud."))
                : Result<GetPortalPaymentClaimProofResult>.Failure(ApplicationError.ServiceUnavailable(
                    cloudResult.Detail ?? "Control Cloud payment proof is unavailable.",
                    nameof(query.ClaimId)));
        }

        return Result<GetPortalPaymentClaimProofResult>.Success(new GetPortalPaymentClaimProofResult(
            cloudResult.Content,
            cloudResult.ContentType,
            cloudResult.FileName));
    }
}
