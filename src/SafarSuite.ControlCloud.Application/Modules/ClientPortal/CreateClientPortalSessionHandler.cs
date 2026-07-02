using SafarSuite.ControlCloud.Application.Common;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal;

public sealed class CreateClientPortalSessionHandler
{
    private readonly IClientPortalIdentityRepository _identities;
    private readonly IClientPortalCredentialService _credentials;
    private readonly IClientPortalSessionService _sessions;
    private readonly IClientPortalAuditRecorder _audit;
    private readonly IControlCloudUnitOfWork _unitOfWork;
    private readonly IControlCloudClock _clock;

    public CreateClientPortalSessionHandler(
        IClientPortalIdentityRepository identities,
        IClientPortalCredentialService credentials,
        IClientPortalSessionService sessions,
        IClientPortalAuditRecorder audit,
        IControlCloudUnitOfWork unitOfWork,
        IControlCloudClock clock)
    {
        _identities = identities;
        _credentials = credentials;
        _sessions = sessions;
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

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return CreateClientPortalSessionResult.Failure(
                "PasswordRequired",
                "Password is required before creating a portal session.");
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

                user.RecordLogin(_clock.UtcNow);
                await _identities.SaveUserAsync(user, token);

                var session = await _sessions.CreateSessionAsync(
                    user.ClientId,
                    user.Role,
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
}
