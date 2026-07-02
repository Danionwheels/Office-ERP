using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ListClientPortalInvitations;

public sealed class ListClientPortalInvitationsHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientPortalInvitationClient _portalInvitations;

    public ListClientPortalInvitationsHandler(
        IClientRepository clients,
        IClientPortalInvitationClient portalInvitations)
    {
        _clients = clients;
        _portalInvitations = portalInvitations;
    }

    public async Task<Result<ListClientPortalInvitationsResult>> HandleAsync(
        ListClientPortalInvitationsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(query.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ListClientPortalInvitationsResult>.Failure(
                    ApplicationError.NotFound(nameof(query.ClientId), "Client was not found."));
            }

            var invitations = await _portalInvitations.ListInvitationsAsync(
                client.Id.Value,
                cancellationToken);

            if (!invitations.IsSuccess)
            {
                return Result<ListClientPortalInvitationsResult>.Failure(
                    ClientPortalInvitationResultMapper.ToApplicationError(invitations));
            }

            return Result<ListClientPortalInvitationsResult>.Success(
                new ListClientPortalInvitationsResult(
                    client.Id.Value,
                    invitations.Invitations!
                        .Select(ClientPortalInvitationResultMapper.ToResult)
                        .ToArray()));
        }
        catch (ArgumentException exception)
        {
            return Result<ListClientPortalInvitationsResult>.Failure(
                ApplicationError.Validation(exception.ParamName ?? nameof(query), exception.Message));
        }
    }
}
