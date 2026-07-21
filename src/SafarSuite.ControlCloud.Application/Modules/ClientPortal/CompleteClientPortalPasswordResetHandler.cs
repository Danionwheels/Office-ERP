using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CompleteClientPortalPasswordResetHandler
{
    private readonly IClientPortalPasswordResetRepository _passwordResets;
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalSessionService _sessions;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CompleteClientPortalPasswordResetHandler(
        IClientPortalPasswordResetRepository passwordResets,
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalSessionService sessions,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _passwordResets = passwordResets;
        _identities = identities;
        _credentials = credentials;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CompleteClientPortalPasswordResetResult> HandleAsync(
        CompleteClientPortalPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ResetToken) || command.ResetToken.Length > 256)
        {
            return CompleteClientPortalPasswordResetResult.Failure("PasswordResetTokenRequired", "A password reset token is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 8)
        {
            return CompleteClientPortalPasswordResetResult.Failure("PasswordTooShort", "Password must be at least 8 characters.");
        }

        if (command.NewPassword.Length > 256)
        {
            return CompleteClientPortalPasswordResetResult.Failure("PasswordTooLong", "Password cannot exceed 256 characters.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var reset = await _passwordResets.GetByTokenHashAsync(
                    _credentials.HashSecret($"client-portal-password-reset:{command.ResetToken.Trim()}"),
                    token);
                var now = _clock.UtcNow;

                if (reset is null || !reset.TryConsume(now))
                {
                    return CompleteClientPortalPasswordResetResult.Failure(
                        "PasswordResetInvalid",
                        "The password reset token is invalid, expired, or already used.");
                }

                var user = await _identities.GetUserByIdAsync(reset.UserId, token);

                if (user is null)
                {
                    return CompleteClientPortalPasswordResetResult.Failure(
                        "PasswordResetInvalid",
                        "The password reset token is invalid, expired, or already used.");
                }

                user.ChangePassword(_credentials.HashPassword(command.NewPassword!));
                await _identities.SaveUserAsync(user, token);
                await _passwordResets.SaveAsync(reset, token);
                await _sessions.RevokeAllForUserAsync(user.UserId, "Password reset completed.", token);
                return CompleteClientPortalPasswordResetResult.Success();
            },
            cancellationToken);
    }
}
