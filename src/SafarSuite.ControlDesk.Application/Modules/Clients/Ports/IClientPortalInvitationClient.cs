using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientPortalInvitationClient
{
    Task<ClientPortalInvitationListClientResult> ListInvitationsAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ClientPortalInvitationClientResult> CreateInvitationAsync(
        Guid clientId,
        string email,
        string fullName,
        string role,
        int expiresInDays,
        string createdBy,
        CancellationToken cancellationToken = default);

    Task<ClientPortalInvitationClientResult> ResendInvitationAsync(
        Guid clientId,
        Guid invitationId,
        int expiresInDays,
        string createdBy,
        CancellationToken cancellationToken = default);

    Task<ClientPortalInvitationClientResult> RevokeInvitationAsync(
        Guid clientId,
        Guid invitationId,
        string revokedBy,
        CancellationToken cancellationToken = default);
}

public sealed class ClientPortalInvitationListClientResult
{
    private ClientPortalInvitationListClientResult(
        IReadOnlyCollection<ClientPortalInvitationResponse>? invitations,
        string? failureCode,
        string? detail)
    {
        Invitations = invitations;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Invitations is not null;

    public IReadOnlyCollection<ClientPortalInvitationResponse>? Invitations { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ClientPortalInvitationListClientResult Success(
        IReadOnlyCollection<ClientPortalInvitationResponse> invitations)
    {
        return new ClientPortalInvitationListClientResult(invitations, null, null);
    }

    public static ClientPortalInvitationListClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ClientPortalInvitationListClientResult(null, failureCode, detail);
    }
}

public sealed class ClientPortalInvitationClientResult
{
    private ClientPortalInvitationClientResult(
        ClientPortalInvitationResponse? invitation,
        string? failureCode,
        string? detail)
    {
        Invitation = invitation;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Invitation is not null;

    public ClientPortalInvitationResponse? Invitation { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ClientPortalInvitationClientResult Success(
        ClientPortalInvitationResponse invitation)
    {
        return new ClientPortalInvitationClientResult(
            invitation,
            failureCode: null,
            detail: null);
    }

    public static ClientPortalInvitationClientResult Failure(
        string failureCode,
        string detail)
    {
        return new ClientPortalInvitationClientResult(
            invitation: null,
            failureCode,
            detail);
    }
}
