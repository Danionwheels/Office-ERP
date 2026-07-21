using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Payments.Common;
using SafarSuite.ControlDesk.Application.Modules.Payments.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Payments.ListPortalPaymentClaims;

public sealed class ListPortalPaymentClaimsHandler
{
    private readonly IPortalPaymentClaimRepository _claims;

    public ListPortalPaymentClaimsHandler(IPortalPaymentClaimRepository claims)
    {
        _claims = claims;
    }

    public async Task<Result<IReadOnlyCollection<PortalPaymentClaimResult>>> HandleAsync(
        ListPortalPaymentClaimsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return Result<IReadOnlyCollection<PortalPaymentClaimResult>>.Failure(
                ApplicationError.Validation(nameof(query.ClientId), "Client id cannot be empty."));
        }

        var clientId = query.ClientId.HasValue
            ? ClientId.Create(query.ClientId.Value)
            : (ClientId?)null;
        var claims = await _claims.ListAsync(clientId, cancellationToken);

        return Result<IReadOnlyCollection<PortalPaymentClaimResult>>.Success(
            claims.Select(PortalPaymentClaimResultFactory.From).ToArray());
    }
}
