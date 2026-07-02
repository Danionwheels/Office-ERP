using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class AcceptClientPortalInvitationHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalSessionService _sessions;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public AcceptClientPortalInvitationHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalSessionService sessions,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _credentials = credentials;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AcceptClientPortalInvitationResult> HandleAsync(
        AcceptClientPortalInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.InvitationToken))
        {
            return AcceptClientPortalInvitationResult.Failure(
                "InvitationTokenRequired",
                "Invitation token is required before accepting a portal invite.");
        }

        if (command.Password.Length < 8)
        {
            return AcceptClientPortalInvitationResult.Failure(
                "PasswordTooShort",
                "Password must be at least 8 characters.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var invitation = await _identities.GetInvitationByTokenHashAsync(
                    _credentials.HashSecret(command.InvitationToken),
                    token);

                if (invitation is null)
                {
                    return AcceptClientPortalInvitationResult.Failure(
                        "InvitationNotFound",
                        "Invitation token is not valid.");
                }

                var now = _clock.UtcNow;

                if (!invitation.IsPendingAt(now))
                {
                    return AcceptClientPortalInvitationResult.Failure(
                        "InvitationNotUsable",
                        "Invitation is not pending or has expired.");
                }

                var existingUser = await _identities.GetUserByClientAndEmailAsync(
                    invitation.ClientId,
                    invitation.Email,
                    token);

                if (existingUser is not null)
                {
                    return AcceptClientPortalInvitationResult.Failure(
                        "PortalUserAlreadyExists",
                        "A portal user already exists for this client and email.");
                }

                var user = ControlCloudClientPortalUser.Create(
                    Guid.NewGuid(),
                    invitation.ClientId,
                    invitation.Email,
                    string.IsNullOrWhiteSpace(command.FullName)
                        ? invitation.FullName
                        : command.FullName,
                    invitation.Role,
                    _credentials.HashPassword(command.Password),
                    now);
                invitation.Accept(user.UserId, now);
                user.RecordLogin(now);

                await _identities.AddUserAsync(user, token);
                await _identities.SaveInvitationAsync(invitation, token);

                var session = await _sessions.CreateSessionAsync(user.ClientId, user.Role, token);

                return session.IsSuccess
                    ? AcceptClientPortalInvitationResult.Success(
                        user.UserId,
                        user.ClientId,
                        user.Email,
                        user.FullName,
                        user.Role,
                        session.AccessToken!,
                        session.ExpiresAtUtc!.Value)
                    : AcceptClientPortalInvitationResult.Failure(
                        session.FailureCode ?? "PortalSessionFailed",
                        session.Detail ?? "Portal session could not be created.");
            },
            cancellationToken);
    }
}
