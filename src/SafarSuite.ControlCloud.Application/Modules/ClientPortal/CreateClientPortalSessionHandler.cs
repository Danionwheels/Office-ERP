using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalSessionHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalSessionService _sessions;
    private readonly IClientPortalTotpService _totp;
    private readonly IClientPortalMfaSecretProtector _mfaSecrets;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CreateClientPortalSessionHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalSessionService sessions,
        IClientPortalTotpService totp,
        IClientPortalMfaSecretProtector mfaSecrets,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _credentials = credentials;
        _sessions = sessions;
        _totp = totp;
        _mfaSecrets = mfaSecrets;
        _audit = audit;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<CreateClientPortalSessionResult> HandleAsync(
        CreateClientPortalSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ClientId == Guid.Empty)
        {
            return CreateClientPortalSessionResult.Failure(
                "ClientIdRequired",
                "Client id is required before creating a portal session.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return CreateClientPortalSessionResult.Failure(
                "EmailRequired",
                "Email is required before creating a portal session.");
        }

        if (command.Email.Length > 320)
        {
            return CreateClientPortalSessionResult.Failure("EmailTooLong", "Email cannot exceed 320 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return CreateClientPortalSessionResult.Failure(
                "PasswordRequired",
                "Password is required before creating a portal session.");
        }

        if (command.Password.Length > 256
            || command.TotpCode?.Length > 32
            || command.RecoveryCode?.Length > 128)
        {
            return CreateClientPortalSessionResult.Failure(
                "CredentialsInvalid",
                "One or more credential fields exceed the allowed length.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var user = await _identities.GetUserByClientAndEmailAsync(
                    command.ClientId,
                    command.Email,
                    token);

                if (user is null
                    || !string.Equals(user.Status, ControlCloudClientPortalUserStatuses.Active, StringComparison.Ordinal)
                    || !_credentials.VerifyPassword(command.Password, user.PasswordHash))
                {
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            command.ClientId,
                            null,
                            user?.UserId,
                            ControlCloudAuditWriter.NormalizeEmail(command.Email),
                            ClientPortalAuditEventTypes.SessionRejected,
                            ClientPortalAuditActors.ClientPortal,
                            "Client portal session credentials were rejected.",
                            _clock.UtcNow),
                        token);

                    return CreateClientPortalSessionResult.Failure(
                        "InvalidCredentials",
                        "Email or password is not valid for this client.");
                }

                if (user.IsTotpEnabled)
                {
                    var mfaFailure = ValidateMfa(user, command);

                    if (mfaFailure is not null)
                    {
                        await ControlCloudAuditWriter.TryRecordAsync(
                            _audit,
                            new ClientPortalAuditRecord(
                                Guid.NewGuid(),
                                user.ClientId,
                                null,
                                user.UserId,
                                user.Email,
                                ClientPortalAuditEventTypes.SessionRejected,
                                ClientPortalAuditActors.ClientPortal,
                                "Client portal MFA challenge was rejected.",
                                _clock.UtcNow),
                            token);
                        return mfaFailure;
                    }
                }

                user.RecordLogin(_clock.UtcNow);
                await _identities.SaveUserAsync(user, token);

                var session = await _sessions.CreateSessionAsync(
                    user.UserId,
                    user.ClientId,
                    user.Role,
                    user.SecurityVersion,
                    token);

                if (session.IsSuccess)
                {
                    await ControlCloudAuditWriter.TryRecordAsync(
                        _audit,
                        new ClientPortalAuditRecord(
                            Guid.NewGuid(),
                            user.ClientId,
                            null,
                            user.UserId,
                            user.Email,
                            ClientPortalAuditEventTypes.SessionCreated,
                            ClientPortalAuditActors.ClientPortal,
                            "Client portal session was created.",
                            _clock.UtcNow),
                        token);
                }

                return session;
            },
            cancellationToken);
    }

    private CreateClientPortalSessionResult? ValidateMfa(
        ControlCloudClientPortalUser user,
        CreateClientPortalSessionCommand command)
    {
        var now = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(command.TotpCode))
        {
            if (!_mfaSecrets.TryUnprotect(user.ProtectedTotpSecret!, out var secret))
            {
                return CreateClientPortalSessionResult.Failure(
                    "ClientPortalMfaUnavailable",
                    "Client Portal MFA is temporarily unavailable.");
            }

            if (!_totp.TryVerifyCode(
                    secret,
                    command.TotpCode,
                    now,
                    user.LastTotpStep,
                    out var acceptedStep))
            {
                return CreateClientPortalSessionResult.Failure(
                    "ClientPortalMfaInvalid",
                    "The authenticator code is invalid or was already used.");
            }

            user.RecordTotpUse(acceptedStep);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(command.RecoveryCode))
        {
            var normalized = _credentials.NormalizeRecoveryCode(command.RecoveryCode);
            var suppliedHash = _credentials.HashSecret($"client-portal-recovery:{normalized}");
            var matchingHash = user.RecoveryCodeHashes.FirstOrDefault(hash =>
                FixedTimeEquals(hash, suppliedHash));

            if (matchingHash is null || !user.ConsumeRecoveryCode(matchingHash, now))
            {
                return CreateClientPortalSessionResult.Failure(
                    "ClientPortalMfaInvalid",
                    "The recovery code is invalid or was already used.");
            }

            return null;
        }

        return CreateClientPortalSessionResult.Failure(
            "ClientPortalMfaRequired",
            "An authenticator or recovery code is required.");
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
        var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
