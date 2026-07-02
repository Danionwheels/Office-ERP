using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.ResendClientPortalInvitation;

public sealed class ResendClientPortalInvitationHandler
{
    private readonly IClientRepository _clients;
    private readonly IClientPortalInvitationClient _portalInvitations;

    public ResendClientPortalInvitationHandler(
        IClientRepository clients,
        IClientPortalInvitationClient portalInvitations)
    {
        _clients = clients;
        _portalInvitations = portalInvitations;
    }

    public async Task<Result<ClientPortalInvitationResult>> HandleAsync(
        ResendClientPortalInvitationCommand command,
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

            var invitation = await _portalInvitations.ResendInvitationAsync(
                client.Id.Value,
                command.InvitationId,
                command.ExpiresInDays <= 0 ? 7 : Math.Clamp(command.ExpiresInDays, 1, 30),
                NormalizeCreatedBy(command.CreatedBy),
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

    private static string NormalizeCreatedBy(string createdBy)
    {
        return string.IsNullOrWhiteSpace(createdBy)
            ? "SafarSuite Control Desk"
            : createdBy.Trim();
    }
}
