namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ListClientPortalInvitationsResult
{
    private ListClientPortalInvitationsResult(
        Guid clientId,
        IReadOnlyCollection<ClientPortalInvitationItemResult>? invitations,
        string? failureCode,
        string? detail)
    {
        ClientId = clientId;
        Invitations = invitations;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Invitations is not null;

    public Guid ClientId { get; }

    public IReadOnlyCollection<ClientPortalInvitationItemResult>? Invitations { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ListClientPortalInvitationsResult Success(
        Guid clientId,
        IReadOnlyCollection<ClientPortalInvitationItemResult> invitations)
    {
        return new ListClientPortalInvitationsResult(clientId, invitations, null, null);
    }

    public static ListClientPortalInvitationsResult Failure(
        string failureCode,
        string detail)
    {
        return new ListClientPortalInvitationsResult(Guid.Empty, null, failureCode, detail);
    }
}
