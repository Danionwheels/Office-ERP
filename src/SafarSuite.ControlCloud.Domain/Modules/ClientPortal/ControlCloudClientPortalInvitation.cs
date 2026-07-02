namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public sealed class ControlCloudClientPortalInvitation
{
    public Guid InvitationId { get; set; }

    public Guid ClientId { get; set; }

    public string Email { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Role { get; set; } = "";

    public string TokenHash { get; set; } = "";

    public string Status { get; set; } = ControlCloudClientPortalInvitationStatuses.Pending;

    public string CreatedBy { get; set; } = "";

    public DateTimeOffset InvitedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }

    public Guid? AcceptedUserId { get; set; }

    public bool IsPendingAt(DateTimeOffset now)
    {
        return string.Equals(Status, ControlCloudClientPortalInvitationStatuses.Pending, StringComparison.Ordinal)
            && ExpiresAtUtc > now;
    }

    public bool IsAccepted()
    {
        return string.Equals(Status, ControlCloudClientPortalInvitationStatuses.Accepted, StringComparison.Ordinal);
    }

    public void Accept(Guid userId, DateTimeOffset acceptedAtUtc)
    {
        Status = ControlCloudClientPortalInvitationStatuses.Accepted;
        AcceptedUserId = userId;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public void Resend(
        string tokenHash,
        string createdBy,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (IsAccepted())
        {
            throw new InvalidOperationException("Accepted portal invitations cannot be resent.");
        }

        TokenHash = tokenHash;
        Status = ControlCloudClientPortalInvitationStatuses.Pending;
        CreatedBy = NormalizeText(createdBy);
        InvitedAtUtc = invitedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = null;
        AcceptedUserId = null;
    }

    public void Revoke()
    {
        if (IsAccepted())
        {
            throw new InvalidOperationException("Accepted portal invitations cannot be revoked.");
        }

        Status = ControlCloudClientPortalInvitationStatuses.Revoked;
    }

    public static ControlCloudClientPortalInvitation Create(
        Guid invitationId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string tokenHash,
        string createdBy,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new ControlCloudClientPortalInvitation
        {
            InvitationId = invitationId,
            ClientId = clientId,
            Email = NormalizeEmail(email),
            FullName = NormalizeText(fullName),
            Role = NormalizeRole(role),
            TokenHash = tokenHash,
            Status = ControlCloudClientPortalInvitationStatuses.Pending,
            CreatedBy = NormalizeText(createdBy),
            InvitedAtUtc = invitedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static string NormalizeRole(string role)
    {
        var normalized = NormalizeText(role);

        return string.IsNullOrWhiteSpace(normalized) ? "ClientViewer" : normalized;
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }
}

public static class ControlCloudClientPortalInvitationStatuses
{
    public const string Pending = "Pending";
    public const string Accepted = "Accepted";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
}
