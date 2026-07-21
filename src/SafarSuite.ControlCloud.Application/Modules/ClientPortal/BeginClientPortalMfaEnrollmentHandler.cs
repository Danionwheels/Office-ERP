using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class BeginClientPortalMfaEnrollmentHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalTotpService _totp;
    private readonly IClientPortalMfaSecretProtector _secrets;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public BeginClientPortalMfaEnrollmentHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalTotpService totp,
        IClientPortalMfaSecretProtector secrets,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _credentials = credentials;
        _totp = totp;
        _secrets = secrets;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<BeginClientPortalMfaEnrollmentResult> HandleAsync(
        BeginClientPortalMfaEnrollmentCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var user = await _identities.GetUserByIdAsync(command.UserId, token);

                if (user is null)
                {
                    return BeginClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalUserNotFound",
                        "Client Portal user was not found.");
                }

                if (string.IsNullOrWhiteSpace(command.Password)
                    || command.Password.Length > 256
                    || !_credentials.VerifyPassword(command.Password, user.PasswordHash))
                {
                    return BeginClientPortalMfaEnrollmentResult.Failure(
                        "ClientPortalReauthenticationRequired",
                        "Confirm the current account password before changing MFA.");
                }

                var secret = _totp.CreateSecret();
                var recoveryCodes = _credentials.CreateRecoveryCodes(10).ToArray();
                var recoveryHashes = recoveryCodes
                    .Select(code => _credentials.HashSecret(
                        $"client-portal-recovery:{_credentials.NormalizeRecoveryCode(code)}"))
                    .ToArray();
                user.BeginTotpEnrollment(_secrets.Protect(secret), recoveryHashes, _clock.UtcNow);
                await _identities.SaveUserAsync(user, token);

                var otpAuthUri = _totp.CreateOtpAuthUri(
                    "SafarSuite Client Portal",
                    user.Email,
                    secret);
                return BeginClientPortalMfaEnrollmentResult.Success(
                    secret,
                    otpAuthUri,
                    _totp.CreateQrCodeDataUri(otpAuthUri),
                    recoveryCodes);
            },
            cancellationToken);
    }
}
