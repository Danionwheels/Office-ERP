using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ListClientPortalInvitationsHandler
{
    private readonly IControlCloudClientCommercialProjectionRepository _projections;
    private readonly IClientPortalIdentityRepository _identities;

    public ListClientPortalInvitationsHandler(
        IControlCloudClientCommercialProjectionRepository projections,
        IClientPortalIdentityRepository identities)
    {
        _projections = projections;
        _identities = identities;
    }

    public async Task<ListClientPortalInvitationsResult> HandleAsync(
        ListClientPortalInvitationsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ClientId == Guid.Empty)
        {
            return ListClientPortalInvitationsResult.Failure(
                "ClientIdRequired",
                "Client id is required before listing portal invitations.");
        }

        var projection = await _projections.GetByClientIdAsync(query.ClientId, cancellationToken);

        if (projection is null)
        {
            return ListClientPortalInvitationsResult.Failure(
                "ClientNotFound",
                "Client is not available in Control Cloud yet.");
        }

        var invitations = await _identities.ListInvitationsByClientIdAsync(
            query.ClientId,
            cancellationToken);

        return ListClientPortalInvitationsResult.Success(
            query.ClientId,
            invitations.Select(invitation => ClientPortalInvitationResultMapper.ToItem(invitation)).ToArray());
    }
}
