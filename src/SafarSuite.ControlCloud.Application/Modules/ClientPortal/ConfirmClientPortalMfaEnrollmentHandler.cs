using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class ConfirmClientPortalMfaEnrollmentHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalTotpService _totp;
    private readonly IClientPortalMfaSecretProtector _secrets;
    private readonly IClientPortalSessionService _sessions;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public ConfirmClientPortalMfaEnrollmentHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalTotpService totp,
        IClientPortalMfaSecretProtector secrets,
        IClientPortalSessionService sessions,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _totp = totp;
        _secrets = secrets;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ConfirmClientPortalMfaEnrollmentResult> HandleAsync(
        ConfirmClientPortalMfaEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var user = await _identities.GetUserByIdAsync(command.UserId, token);

                if (user is null)
                {
                    return ConfirmClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalUserNotFound",
                        "Client Portal user was not found.");
                }

                if (string.IsNullOrWhiteSpace(user.PendingProtectedTotpSecret)
                    || !_secrets.TryUnprotect(user.PendingProtectedTotpSecret, out var secret))
                {
                    return ConfirmClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalMfaEnrollmentNotStarted",
                        "Start TOTP enrollment before confirming it.");
                }

                if (user.TotpEnrollmentStartedAtUtc is null
                    || user.TotpEnrollmentStartedAtUtc.Value.AddMinutes(10) <= _clock.UtcNow)
                {
                    return ConfirmClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalMfaEnrollmentExpired",
                        "TOTP enrollment expired. Start the enrollment again.");
                }

                if (!_totp.TryVerifyCode(secret, command.Code, _clock.UtcNow, null, out var acceptedStep))
                {
                    return ConfirmClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalMfaInvalid",
                        "The authenticator code is invalid.");
                }

                user.ConfirmTotpEnrollment(acceptedStep, _clock.UtcNow);
                await _identities.SaveUserAsync(user, token);
                await _sessions.RevokeAllForUserAsync(
                    user.UserId,
                    "MFA enrollment changed.",
                    token);
                var replacementSession = await _sessions.CreateSessionAsync(
                    user.UserId,
                    user.ClientId,
                    user.Role,
                    user.SecurityVersion,
                    token);

                return replacementSession.IsSuccess
                    ? ConfirmClientPortalMfaEnrollmentResult.Success(replacementSession)
                    : ConfirmClientPortalMfaEnrollmentResult.Failure(
                        replacementSession.FailureCode ?? "PortalSessionFailed",
                        replacementSession.Detail ?? "A replacement session could not be created.");
            },
            cancellationToken);
    }
}
