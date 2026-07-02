using SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

namespace SafarSuite.ControlDesk.Application.Modules.Clients.Ports;

public interface IClientPortalInvitationClient
{
    Task<ClientPortalInvitationClientResult> CreateInvitationAsync(
        Guid clientId,
        string email,
        string fullName,
        string role,
        int expiresInDays,
        string createdBy,
        CancellationToken cancellationToken = default);
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
