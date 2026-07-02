using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

internal static class ClientPortalInvitationResultMapper
{
    public static ClientPortalInvitationItemResult ToItem(
        ControlCloudClientPortalInvitation invitation,
        string? invitationToken = null)
    {
        return new ClientPortalInvitationItemResult(
            invitation.InvitationId,
            invitation.ClientId,
            invitation.Email,
            invitation.FullName,
            invitation.Role,
            invitation.Status,
            invitation.InvitedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitationToken);
    }
}

public sealed record ClientPortalInvitationItemResult(
    Guid InvitationId,
    Guid ClientId,
    string Email,
    string FullName,
    string Role,
    string Status,
    DateTimeOffset InvitedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    string? InvitationToken);
