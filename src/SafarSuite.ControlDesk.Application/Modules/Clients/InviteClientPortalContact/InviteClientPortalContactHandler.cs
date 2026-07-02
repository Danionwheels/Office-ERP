using SafarSuite.ControlDesk.Application.Common.Results;
using SafarSuite.ControlDesk.Application.Modules.Clients.Ports;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.InviteClientPortalContact;

public sealed class InviteClientPortalContactHandler
{
    private static readonly HashSet<string> SupportedPortalRoles = new(StringComparer.Ordinal)
    {
        "ClientOwner",
        "ClientBilling",
        "ClientTechnical",
        "ClientViewer"
    };

    private readonly IClientRepository _clients;
    private readonly IClientPortalInvitationClient _portalInvitations;

    public InviteClientPortalContactHandler(
        IClientRepository clients,
        IClientPortalInvitationClient portalInvitations)
    {
        _clients = clients;
        _portalInvitations = portalInvitations;
    }

    public async Task<Result<InviteClientPortalContactResult>> HandleAsync(
        InviteClientPortalContactCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clientId = ClientId.Create(command.ClientId);
            var contactId = ClientContactId.Create(command.ClientContactId);
            var createdBy = NormalizeCreatedBy(command.CreatedBy);
            var expiresInDays = command.ExpiresInDays <= 0 ? 7 : Math.Clamp(command.ExpiresInDays, 1, 30);
            var client = await _clients.GetByIdAsync(clientId, cancellationToken);

            if (client is null)
            {
                return Result<InviteClientPortalContactResult>.Failure(
                    ApplicationError.NotFound(nameof(command.ClientId), "Client was not found."));
            }

            var contact = client.Contacts.FirstOrDefault(item => item.Id == contactId);

            if (contact is null)
            {
                return Result<InviteClientPortalContactResult>.Failure(
                    ApplicationError.NotFound(nameof(command.ClientContactId), "Client contact was not found."));
            }

            if (string.IsNullOrWhiteSpace(contact.Email))
            {
                return Result<InviteClientPortalContactResult>.Failure(
                    ApplicationError.Validation(
                        nameof(command.ClientContactId),
                        "Client contact needs an email address before portal access can be invited."));
            }

            var portalRole = ResolvePortalRole(command.PortalRole, contact.Role);

            if (!SupportedPortalRoles.Contains(portalRole))
            {
                return Result<InviteClientPortalContactResult>.Failure(
                    ApplicationError.Validation(
                        nameof(command.PortalRole),
                        "Portal role must be ClientOwner, ClientBilling, ClientTechnical, or ClientViewer."));
            }

            var invitation = await _portalInvitations.CreateInvitationAsync(
                client.Id.Value,
                contact.Email,
                contact.FullName,
                portalRole,
                expiresInDays,
                createdBy,
                cancellationToken);

            if (!invitation.IsSuccess)
            {
                return Result<InviteClientPortalContactResult>.Failure(ToApplicationError(invitation));
            }

            var response = invitation.Invitation!;

            return Result<InviteClientPortalContactResult>.Success(new InviteClientPortalContactResult(
                response.InvitationId,
                response.ClientId,
                contact.Id.Value,
                response.Email,
                response.FullName,
                response.Role,
                response.Status,
                response.InvitedAtUtc,
                response.ExpiresAtUtc,
                response.InvitationToken,
                response.InvitationUrl));
        }
        catch (ArgumentException exception)
        {
            return Result<InviteClientPortalContactResult>.Failure(ApplicationError.Validation(
                exception.ParamName ?? nameof(command),
                exception.Message));
        }
    }

    private static string NormalizeCreatedBy(string createdBy)
    {
        return string.IsNullOrWhiteSpace(createdBy)
            ? "SafarSuite Control Desk"
            : createdBy.Trim();
    }

    private static string ResolvePortalRole(string? requestedPortalRole, ClientContactRole contactRole)
    {
        if (!string.IsNullOrWhiteSpace(requestedPortalRole))
        {
            return requestedPortalRole.Trim();
        }

        return contactRole switch
        {
            ClientContactRole.Owner => "ClientOwner",
            ClientContactRole.Billing => "ClientBilling",
            ClientContactRole.Accounts => "ClientBilling",
            ClientContactRole.Technical => "ClientTechnical",
            ClientContactRole.Support => "ClientTechnical",
            _ => "ClientViewer"
        };
    }

    private static ApplicationError ToApplicationError(
        ClientPortalInvitationClientResult result)
    {
        return result.FailureCode switch
        {
            "ClientNotFound" => ApplicationError.NotFound(
                "clientId",
                result.Detail ?? "Client is not available in Control Cloud yet."),
            "PortalUserAlreadyExists" => ApplicationError.Conflict(
                "email",
                result.Detail ?? "A portal user already exists for this client contact."),
            "ControlCloudInvitationUnauthorized" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud rejected the provider invitation key."),
            "ControlCloudInvitationNotConfigured" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud invitation endpoint is not configured."),
            "ControlCloudInvitationUnavailable" => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud invitation endpoint is unavailable."),
            _ => ApplicationError.Unexpected(
                result.Detail ?? "Control Cloud could not create the portal invitation.")
        };
    }
}
