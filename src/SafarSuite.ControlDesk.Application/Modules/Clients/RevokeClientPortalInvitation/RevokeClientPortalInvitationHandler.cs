using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.RevokeClientPortalInvitation;

public sealed class RevokeClientPortalInvitationHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientPortalInvitationClient _portalInvitations;

    public RevokeClientPortalInvitationHandler(
        IClientRepository clients,
        IClientPortalInvitationClient portalInvitations)
    {
        _clients = clients;
        _portalInvitations = portalInvitations;
    }

    public async Task<Result<ClientPortalInvitationResult>> HandleAsync(
        RevokeClientPortalInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<ClientPortalInvitationResult>.Failure(
                    ApplicationError.NotFound(nameof(command.ClientId), "Client was not found."));
            }

            var invitation = await _portalInvitations.RevokeInvitationAsync(
                client.Id.Value,
                command.InvitationId,
                NormalizeRevokedBy(command.RevokedBy),
                cancellationToken);

            if (!invitation.IsSuccess)
            {
                return Result<ClientPortalInvitationResult>.Failure(
                    ClientPortalInvitationResultMapper.ToApplicationError(invitation));
            }

            return Result<ClientPortalInvitationResult>.Success(
                ClientPortalInvitationResultMapper.ToResult(invitation.Invitation!));
        }
        catch (ArgumentException exception)
        {
            return Result<ClientPortalInvitationResult>.Failure(
                ApplicationError.Validation(exception.ParamName ?? nameof(command), exception.Message));
        }
    }

    private static string NormalizeRevokedBy(string revokedBy)
    {
        return string.IsNullOrWhiteSpace(revokedBy)
            ? "SafarSuite Control Desk"
            : revokedBy.Trim();
    }
}
