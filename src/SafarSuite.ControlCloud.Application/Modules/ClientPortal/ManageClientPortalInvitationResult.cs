namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ManageClientPortalInvitationResult
{
    private ManageClientPortalInvitationResult(
        ClientPortalInvitationItemResult? invitation,
        string? failureCode,
        string? detail)
    {
        Invitation = invitation;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => Invitation is not null;

    public ClientPortalInvitationItemResult? Invitation { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static ManageClientPortalInvitationResult Success(
        ClientPortalInvitationItemResult invitation)
    {
        return new ManageClientPortalInvitationResult(invitation, null, null);
    }

    public static ManageClientPortalInvitationResult Failure(
        string failureCode,
        string detail)
    {
        return new ManageClientPortalInvitationResult(null, failureCode, detail);
    }
}
