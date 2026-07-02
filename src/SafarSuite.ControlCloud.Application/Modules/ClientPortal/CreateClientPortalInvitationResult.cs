namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalInvitationResult
{
    private CreateClientPortalInvitationResult(
        Guid invitationId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string status,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc,
        string? invitationToken,
        string? failureCode,
        string? detail)
    {
        InvitationId = invitationId;
        ClientId = clientId;
        Email = email;
        FullName = fullName;
        Role = role;
        Status = status;
        InvitedAtUtc = invitedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        InvitationToken = invitationToken;
        FailureCode = failureCode;
        Detail = detail;
    }

    public bool IsSuccess => InvitationToken is not null;

    public Guid InvitationId { get; }

    public Guid ClientId { get; }

    public string Email { get; }

    public string FullName { get; }

    public string Role { get; }

    public string Status { get; }

    public DateTimeOffset InvitedAtUtc { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public string? InvitationToken { get; }

    public string? FailureCode { get; }

    public string? Detail { get; }

    public static CreateClientPortalInvitationResult Success(
        Guid invitationId,
        Guid clientId,
        string email,
        string fullName,
        string role,
        string status,
        DateTimeOffset invitedAtUtc,
        DateTimeOffset expiresAtUtc,
        string invitationToken)
    {
        return new CreateClientPortalInvitationResult(
            invitationId,
            clientId,
            email,
            fullName,
            role,
            status,
            invitedAtUtc,
            expiresAtUtc,
            invitationToken,
            failureCode: null,
            detail: null);
    }

    public static CreateClientPortalInvitationResult Failure(
        string failureCode,
        string detail)
    {
        return new CreateClientPortalInvitationResult(
            Guid.Empty,
            Guid.Empty,
            email: "",
            fullName: "",
            role: "",
            status: "",
            invitedAtUtc: default,
            expiresAtUtc: default,
            invitationToken: null,
            failureCode,
            detail);
    }
}
