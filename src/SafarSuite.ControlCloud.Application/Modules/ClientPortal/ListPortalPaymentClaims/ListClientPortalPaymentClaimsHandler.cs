using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.ListPortalPaymentClaims;

public sealed class ListClientPortalPaymentClaimsHandler
{
    private readonly IClientPortalPaymentClaimRepository _claims;
    private readonly IClientPortalAttachmentRepository _attachments;

    public ListClientPortalPaymentClaimsHandler(
        IClientPortalPaymentClaimRepository claims,
        IClientPortalAttachmentRepository attachments)
    {
        _claims = claims;
        _attachments = attachments;
    }

    public async Task<ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalPaymentClaimView>>> HandleAsync(
        Guid? clientId,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
        {
            return ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalPaymentClaimView>>.Failure(
                "ClientIdRequired",
                "Client id is required.");
        }

        var claims = await _claims.ListAsync(clientId, cancellationToken);
        var views = new List<ClientPortalPaymentClaimView>(claims.Count);

        foreach (var claim in claims
                     .OrderByDescending(item => item.SubmittedAtUtc)
                     .ThenByDescending(item => item.ClaimId))
        {
            var attachment = claim.ProofAttachmentId is null
                ? null
                : await _attachments.GetByIdAsync(claim.ProofAttachmentId.Value, cancellationToken);
            views.Add(new ClientPortalPaymentClaimView(claim, attachment));
        }

        return ClientPortalPaymentOperationResult<IReadOnlyCollection<ClientPortalPaymentClaimView>>.Success(views);
    }
}
